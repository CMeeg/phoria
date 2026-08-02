# Phase 2 — Server Robustness Close-Out

> **Prerequisite:** The main Phase 2 plan (`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`) is complete. This document closes every remaining item — regressions from that work, previously deferred issues, and observations surfaced during implementation and review.

**Goal:** Resolve every open server-robustness item. Nothing is deferred without a specific, recorded rationale.

**Tech Stack:** C#/.NET 10 (`PhoriaServerProcess`, `PhoriaServerMonitor`, `PhoriaServerHttpClientFactory`, `Phoria.Islands`, `Phoria.IO`), TypeScript (`@phoria/phoria/server` routing/handlers, e2e `server.ts`), Aspire 13.4.6 AppHosts.

## Global Constraints

- Repo style: Biome for TS/JSON/markdown (tabs, line width 120); C# tabs + 4-space indent, nullable enabled, language 13.0.
- Central package management via `Directory.Packages.props`. pnpm catalog in `pnpm-workspace.yaml`.
- Build order: `build` → `lint` → `check` → `test`.
- Do not modify the published `@phoria/phoria` packages beyond the Task 4 logger interface addition (non-breaking, optional parameter). Do not touch browser/client code.
- Tests: xUnit v3 for .NET (both net8.0 and net10.0 TFMs), Vitest for JS packages.
- The `Phoria.sln` does not include e2e apps or AppHosts — build those individually.

---

### Task 1: `PhoriaServerProcess` lifecycle hardening

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs`
- Modify: `packages/Phoria.Tests/Server/PhoriaServerProcessTests.cs`

**Interfaces:**
- Produces: all four remaining correctness gaps in `PhoriaServerProcess` are closed. The timer NRE is eliminated, spawned children are never orphaned during shutdown, concurrent stop callers share one awaitable task, and the semaphore lifecycle is unambiguous.

**Background:** Task 4 of the main plan delivered graceful SIGTERM → grace → force-kill and end-to-end tests. The implementation review (Task 4 re-review) and the final whole-branch review identified four correctness gaps that remain in the merged code.

- [ ] **Step 1: Fix the timer NRE on shutdown**

`StartServer` at `PhoriaServerProcess.cs:97-100` creates `periodicTimer` inside a lock, releases the lock, then dereferences `periodicTimer!` in the while-loop condition. If `StopServerCore` fires between the unlock and the dereference, it clears `periodicTimer` to null under its own lock at line 312-313, and line 100 throws `NullReferenceException`.

Fix: capture the timer in a local variable inside the lock block.

```csharp
PeriodicTimer timer;
lock (sync)
{
    if (stopping)
    {
        stoppingToken.ThrowIfCancellationRequested();
        return;
    }
    periodicTimer = new PeriodicTimer(...);
    timer = periodicTimer;
}
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await EnsureProcessIsRunning(...);
}
```

The local variable keeps the reference alive regardless of what `StopServerCore` does to the field. If `StopServerCore` disposes the timer, `WaitForNextTickAsync` throws `ObjectDisposedException` — the correct, safe failure mode.

- [ ] **Step 2: Close the spawn-window orphan**

`StartServer` registers `stoppingToken.Register(() => _ = StopServer())` at line 81. `EnsureProcessIsRunning` starts the process via `cmd.ListenAsync(CancellationToken.None)` at line 140, and `processId` is only assigned when the `StartedCommandEvent` fires at line 158. If the host token fires after `ListenAsync` begins but before the event assigns `processId`, `StopServer` sees `processId == null` and returns — the newly spawned child continues running unowned while `ListenAsync(None)` blocks.

`StopServerCore` already awaits `processStartCompletion` (line 298-303), but the gate expects a `stopTask` that may not exist yet in this mid-spawn window. Fix: ensure the spawn path produces a `stopTask` that `StopServer` can return, or synchronize startup state so a shutdown during spawn first waits for the PID to become available (via `processStartCompletion`), then stops the process through the normal sequence. Add a regression test that cancels in the spawn window and verifies no orphaned node process remains.

- [ ] **Step 3: Make in-flight stop awaitable for all callers**

`StopServer()` at line 283 uses a lock + `stopTask` to ensure idempotency. Two call paths arrive at StopServer: the token registration at line 81 (`_ = StopServer()`) and `PhoriaServerProcessService.ExecuteAsync`'s OCE path. The fire-and-forget registration (`_ =`) means the registration's call to `StopServer` is unobserved — if the registration wins the race and creates `stopTask`, the service's call returns the same task (correct). But the registration itself never awaits that task — the host can finish shutdown before the graceful stop completes.

Fix: store and share a single stop task so all callers can await it. The registration in `StartServer` should capture the `StopServer()` task and forward it. The simplest approach: have `StartServer`'s `finally` block (line 105-108) await the shared `StopServer()` return value, which already works — the `finally` awaits `StopServer()` which returns the shared `stopTask`. The registration is the weak link — ensure the host shutdown path (service → OCE → StartServer throws → finally → await StopServer) observes the in-flight graceful stop. The registration remains as a safety net but the primary await path is the finally block.

- [ ] **Step 4: Harden the semaphore lifecycle**

The pre-existing deferred issue: `StopServer` disposes the semaphore (via `semaphoreToDispose` captured in `StopServerCore`), and `Dispose()` at line 324-333 also disposes semaphore and periodicTimer. `StopServerCore` clears `semaphore` to null, then `Dispose()` re-disposes a potentially already-disposed or recreated semaphore. The semaphore should be owned by `StartServer`/`EnsureProcessIsRunning` alone. `Dispose()` should only act on remaining state not already captured by `StopServerCore`. The lifecycles of `semaphore` and `periodicTimer` need a single owner — which should be the `StartServer` try/finally block, not `Dispose()`.

Audit and fix: ensure `semaphore` is only created and disposed within `StartServer`'s scope, never touched by `Dispose()`. `periodicTimer` follows the same pattern. `Dispose()` becomes a minimal safety net for edge cases where `StartServer` never ran.

- [ ] **Step 5: Evidence**

Run focused tests: the existing `PhoriaServerProcessTests` suite plus new regression tests for the spawn-window (cancellation during ListenAsync startup) and concurrent-stop scenarios. Run the full `dotnet test --solution Phoria.sln --configuration Release`. Verify both TFMs pass.

---

### Task 2: StreamPool disposal + certificate gating

**Files:**
- Modify: `packages/Phoria/Islands/PhoriaIslandHtmlContent.cs`
- Modify: `packages/Phoria/Islands/PhoriaIslandComponentFactory.cs`
- Modify: `packages/Phoria/ServiceCollectionExtensions.cs`

**Interfaces:**
- Produces: all `StreamPool` instances are deterministically disposed. Certificate validation uses the system trust store in production; dangerous-accept is restricted to development.

- [ ] **Step 1: Dispose `StreamPool` instances after render**

`PhoriaIslandSsr.RenderIsland()` creates two `StreamPool` instances (`contentStreamPool` at line 43, `propsStreamPool` at line 30). Both are passed into `PhoriaIslandSsrResult` as record properties. `PhoriaIslandHtmlContent.WriteTo()` reads the underlying `RecyclableMemoryStream` via `GetReadOnlySequence()` and `Position = 0` but never disposes the owning `StreamPool`. Neither does `PhoriaIslandComponentFactory.CreateAsync()`, which receives the result and constructs the `PhoriaIslandHtmlContent`.

Fix: `StreamPool` already implements `IDisposable`. The consumer closest to the end of the lifetime is `PhoriaIslandHtmlContent` — it's the last thing to read the streams. Implement `IDisposable` on `PhoriaIslandHtmlContent`, disposing both `ssrResult.Content` and `ssrResult.Props` (when non-null). The factory's `CreateAsync` wraps the content in a `using` scope suitable for the Razor rendering pipeline. If `IHtmlContent` lifetime management makes `IDisposable` awkward, alternatively make `PhoriaIslandComponentFactory` own the disposal after `WriteTo` completes — the existing `IServiceCollection` scoping (transient TagHelper → scoped factory) gives a natural disposal point.

- [ ] **Step 2: Gate `DangerousAcceptAnyServerCertificateValidator` on development environment**

`ServiceCollectionExtensions.cs:37` unconditionally uses `DangerousAcceptAnyServerCertificateValidator`. The `ConfigurePrimaryHttpMessageHandler` delegate receives `IServiceProvider` via the overload. Resolve `IHostEnvironment` from the provider and gate:

```csharp
services.AddHttpClient(PhoriaServerHttpClientFactory.HttpClientName)
    .ConfigurePrimaryHttpMessageHandler(services => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = services.GetRequiredService<IHostEnvironment>().IsDevelopment()
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : null
    })
```

Production uses `null` → the system trust store. Development uses dangerous-accept for local dev-cert scenarios (paired with `@phoria/vite-plugin-dotnet-dev-certs`).

- [ ] **Step 3: Evidence**

Run the full .NET test suite. Verify the `PhoriaIslandHtmlContent` disposal path doesn't break the Razor rendering pipeline (TagHelpers hold `IHtmlContent` values within the request scope — disposal should happen when the request ends or the content is written).

---

### Task 3: Production SSR graceful degradation

**Files:**
- Modify: `packages/Phoria/Islands/PhoriaIslandComponentFactory.cs`
- Modify: `packages/Phoria/Server/PhoriaServerMonitor.cs`

**Interfaces:**
- Produces: isomorphic components render client-only when the SSR server is unavailable instead of throwing. Components switch back to SSR if the server recovers mid-session. The `PhoriaServerMonitor` exposes a recovery signal.

- [ ] **Step 1: Isomorphic → client-only fallback**

`PhoriaIslandComponentFactory.CreateAsync()` at line 42-48: when `renderMode != ServerOnly` and the server is unhealthy, it throws `PhoriaIslandComponentException`. For `Isomorphic` mode specifically, this should degrade instead of fail: log a warning, set `renderMode = ClientOnly`, skip the SSR call, and continue to render. `ServerOnly` mode still throws — it has no client-side fallback and the server is genuinely required.

```csharp
if (renderMode != PhoriaIslandRenderMode.ServerOnly
    && serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
{
    if (renderMode == PhoriaIslandRenderMode.Isomorphic)
    {
        logger.LogServerUnhealthyDegradingToClient(component);
        renderMode = PhoriaIslandRenderMode.ClientOnly;
    }
    else
    {
        throw new PhoriaIslandComponentException(...);
    }
}
```

- [ ] **Step 2: Expose health-recovery signal from the monitor**

`IPhoriaServerMonitor` currently exposes `ServerStatus` (a polled snapshot) and `StartMonitoring`/`StopMonitoring`. There is no way for a consumer to react when health transitions from Unhealthy → Healthy without polling. The middleware (`PhoriaServerMiddleware`) and component factory both check `ServerStatus.Health` on every request — that's already reactive on a per-request basis, which is sufficient for the render-mode fallback. The monitor's periodic health check is responsible for the flip; consumers read the current value.

For the `PhoriaServerMonitorService.cs:12` TODO ("block until server is started"), the existing periodic-timer loop in `PhoriaServerMonitor.StartMonitoring` already polls continuously. The TODO is about blocking the service's `ExecuteAsync` until the first successful health check, so the app doesn't start serving requests before the server is known-healthy. Add a first-healthy `TaskCompletionSource` to the monitor, set it on the first Healthy transition, and have the service await it before completing `ExecuteAsync`'s initialization phase. This aligns with the existing `HealthCheckInterval` timing and doesn't require a new interface method.

- [ ] **Step 3: Evidence**

Existing tests continue to pass. Manual verification: start the sidecar app without the node server, request the page — the island renders as a client placeholder with a warning logged, rather than a 500 error.

---

### Task 4: Node handler logger interface

**Files:**
- Modify: `packages/phoria-islands/src/server/routing.ts`
- Modify: `packages/phoria-islands/src/server/main.ts` (re-export)

**Interfaces:**
- Produces: `createPhoriaCsrRequestHandler` and `createPhoriaSsrRequestHandler` accept an optional `logger` in their options. The library exports a minimal `PhoriaLogger` interface. The library never imports OTel. Existing callers are unaffected (optional parameter, console default).

- [ ] **Step 1: Define and export the `PhoriaLogger` interface**

```ts
export interface PhoriaLogger {
    info(message: string, data?: Record<string, unknown>): void
    warn(message: string, data?: Record<string, unknown>): void
    error(message: string, data?: Record<string, unknown>): void
}
```

Provide a default console-backed implementation. The interface is small enough to survive stabilization into 1.0 without boxing the library into a specific logging framework.

- [ ] **Step 2: Accept `logger` in handler options**

Add `logger?: PhoriaLogger` to `PhoriaCsrRequestHandlerOptions` and `PhoriaSsrRequestHandlerOptions` (the existing `options` parameter). Default to the console implementation. The handlers use the logger for internal diagnostics (file-not-found, SSR errors, render failures) — replacing current bare `console` calls or silent failures.

- [ ] **Step 3: Update e2e server.ts files to pass their OTel logger**

The e2e `server.ts` already has a `log()` wrapper at line 27-37 backed by OTel. Wrap it as a `PhoriaLogger`-conforming object and pass it to the handler factories. This proves the interface in practice without modifying the published package's dependencies.

- [ ] **Step 4: Evidence**

TypeScript check on the published package and both e2e apps. Existing tests pass. The e2e apps log handler-level diagnostics through OTel / console fallback.

---

### Task 5: `LoggerMessage` / `EventId` refactor

**Files:**
- Modify: `packages/Phoria/Logging/EventId.cs`
- Modify: All files with `[LoggerMessage]` attributes (7 files): `PhoriaServerProcess.cs`, `PhoriaServerMonitor.cs`, `PhoriaServerMiddleware.cs`, `ViteDevServerHmrProxy.cs`, `ViteManifestReader.cs`, `ViteSsrManifestReader.cs`, `PhoriaIslandEntryTagHelper.cs`

**Interfaces:**
- Produces: `EventId` offsets are named constants per feature, sequential and unambiguous. Collision risk is visible at a glance. The `EventFeature` base offsets remain for feature-area grouping in structured log tooling.

- [ ] **Step 1: Define sequential event constants per feature area**

Move event ids from ad-hoc `+ N` offsets into named constants within each feature's log-messages partial class. Example for Server:

```csharp
private static class ServerEvent
{
    public const int MiddlewareProxyError = 1;
    public const int ServerIsHealthy = 2;
    public const int ServerIsUnhealthy = 3;
    public const int ProcessNotConfigured = 7;   // existing offset preserved
    public const int ProcessIsHealthy = 8;
    // ... etc
}
// Usage:
[LoggerMessage(EventId = EventFeature.Server + ServerEvent.ProcessNotConfigured, ...)]
```

Do not renumber existing event ids — preserve the current `EventFeature.Feature + N` values so log consumers see no break. The constants become documentation of what ranges are allocated. Gaps between numbers (e.g. 8 → 14) are now documented at the declaration site rather than implicit across files.

Do this for all four feature areas (Core, IO, Islands, Server, Vite) in each log-messages partial class.

- [ ] **Step 2: Evidence**

`dotnet build packages/Phoria/Phoria.csproj --configuration Release` — clean, no warnings. Existing tests pass. No runtime behavior change — only `const int` indirection.

---

### Task 6: Quick cleanups

**Files:**
- Modify: `packages/Phoria/Phoria.csproj`
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs`
- Modify: `e2e/with-sidecar/Phoria.AppHost/Program.cs`
- Rename: `e2e/with-sidecar/WebApp/appsettings.Production.json` → `e2e/with-sidecar/WebApp/appsettings.Preview.json`

**Interfaces:**
- Produces: `InternalsVisibleTo` removed from the published package. `with-sidecar` uses `Preview` environment consistently with the sibling e2e apps.

- [ ] **Step 1: Drop `InternalsVisibleTo`**

The test project uses `InternalsVisibleTo` in `Phoria.csproj:26` solely to call a 5-parameter internal constructor on `PhoriaServerProcess` that accepts `int? processId` and `TimeSpan? stopGracePeriod`. Make this constructor `public` instead of `internal`. Remove the `<InternalsVisibleTo Include="Phoria.Tests" />` line from `Phoria.csproj`. The public constructor's XML doc comment already documents its purpose (test seam for direct process-id injection). No production code uses this overload — it exists for integration tests spawning real node children.

- [ ] **Step 2: Fix `with-sidecar` environment**

The sibling apps (framework-multiple, with-workspace) use `DOTNET_ENVIRONMENT=Preview` for the preview path. The sidecar app currently uses `Production` at `Phoria.AppHost/Program.cs:5` and `appsettings.Production.json` contains the `Phoria:Server:Process` config. Change it to `Preview` for consistency:

1. Rename `appsettings.Production.json` → `appsettings.Preview.json` (the file content stays — `Phoria:Server:Process` config triggers `PhoriaServerProcess` to own the Node child).
2. Update `Phoria.AppHost/Program.cs:5`: `"Production"` → `"Preview"`.
3. The sidecar's `WebApp/Program.cs` does NOT need the siblings' "null Process in non-Production" guard — this app's purpose IS to exercise .NET-owned Node in Preview. The Process ownership is driven by the `Phoria:Server:Process` config in the Preview appsettings layer, not by environment-name gating.

- [ ] **Step 3: Evidence**

`dotnet build` on Phoria (clean), both AppHosts, and all three WebApps. The with-sidecar AppHost preview launch exercises the Preview config with process ownership.

---

### Task 7: Aspire dev mode + documentation

**Files:**
- Modify: `e2e/framework-multiple/Phoria.AppHost/Program.cs`
- Modify: `e2e/with-workspace/WebApp/Phoria.AppHost/Program.cs`
- Modify: `e2e/framework-multiple/package.json`
- Modify: `e2e/with-workspace/WebApp/package.json`
- Modify: `docs/guides/getting-started.md`

**Interfaces:**
- Produces: the sibling AppHosts support both dev and preview modes. A single `aspire run` command starts the WebApp + Vite dev server (with HMR) + dashboard. The guide documents Aspire as the recommended dev workflow, with the existing two-terminal `pnpm dev` as an available alternative.

- [ ] **Step 1: Add dev/preview awareness to sibling AppHosts**

The AppHost reads `builder.Environment.IsDevelopment()` to switch the Node resource between dev and preview. In dev: command is `tsx` with the source `server.ts` path, `NODE_ENV=development`. In preview: command is `node` with the compiled `dist/server/server.js`, `NODE_ENV=production`. The WebApp always gets `DOTNET_ENVIRONMENT=Preview` so it loads `appsettings.Preview.json` (which disables .NET-owned Node process in sibling apps — the AppHost owns Node in both modes).

```csharp
var isDev = builder.Environment.IsDevelopment();
var nodeCommand = isDev ? "tsx" : command;   // command = "node" from Preview config
var nodeArgs = isDev
    ? new[] { Path.Combine(webAppDirectory, "ui", "src", "server.ts") }
    : arguments;
var nodeEnv = isDev ? "development" : "production";

builder.AddExecutable("phoria-server", nodeCommand, [...], nodeArgs)
    .WithWorkingDirectory(Path.Combine(webAppDirectory, "ui"))
    .WithHttpEndpoint(port: port, name: "http", isProxied: false)
    .WithEnvironment("NODE_ENV", nodeEnv)
    .WithEnvironment("DOTNET_ENVIRONMENT", "Preview")
    .WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);
```

Apply to both sibling AppHosts symmetrically. The WebApp's `DOTNET_ENVIRONMENT` stays `Preview` in both modes (the AppHost owns Node regardless — dev vs preview only affects the Node command).

- [ ] **Step 2: Add recommended dev scripts**

Add `dev:aspire` scripts to each e2e `package.json`:

```json
"dev:aspire": "aspire run --apphost ./Phoria.AppHost/Phoria.AppHost.csproj -- --environment Development"
```

This starts everything with one command: WebApp, Vite dev server (HMR), and the Aspire dashboard. Keep the existing `dev` script (`tsx ./ui/src/server.ts`) for lightweight iteration.

- [ ] **Step 3: Update getting-started guide**

Document two dev workflows:

1. **Recommended — single-command Aspire:** `pnpm dev:aspire` starts the WebApp, Vite dev server (HMR), and Aspire dashboard. Structured logs and resource health are visible in the dashboard at the URL printed by `aspire run`. Ctrl+C cleanly stops everything.
2. **Alternative — two-terminal manual:** Terminal 1: `pnpm dev` (starts the Vite dev server). Terminal 2: `dotnet run --project WebApp/WebApp.csproj` (starts the .NET app). Lighter-weight, no dashboard.

Also document how to run the debugger safely: in dev mode (`pnpm dev` + separate .NET process), stopping the debugger only kills the .NET process — the Node dev server runs independently and must be stopped manually. In the sidecar production model, never run the debugger with `Phoria:Server:Process` configured — the host owns the Node process and TerminateProcess orphanes it. Use the sibling Aspire preview model for integration-testing graceful shutdown.

Update the debugger-stop entry in the plan's "Known deferred issues" section to reflect that this is now documented as expected behavior with clear development guidance.

- [ ] **Step 4: Evidence**

`dotnet build` on all three WebApps and both AppHosts. TypeScript check and Biome on modified files. Launch each sibling app in dev mode via `aspire run --environment Development`, confirm Vite HMR is active and the dashboard shows both resources. Launch in preview mode, confirm the compiled Node server runs. Clean Ctrl+C exit for both modes.

---

### Task 8: Deferred-issue closeout

**Files:**
- Modify: `docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md` (Known deferred issues section)

**Interfaces:**
- Produces: every item in the plan's "Known deferred issues" section has a final status — resolved in this close-out, or explicitly deferred with a recorded rationale.

- [ ] **Step 1: Resolve completed deferred items**

The following items from the plan's "Known deferred issues" are addressed by tasks above:

| Item | Resolved by |
|------|------------|
| `Process.Kill()` process-tree bug | Addressed in main plan Task 4 (entireProcessTree: true in StopProcess) |
| `StartServer`/`StopServer` semaphore race | Task 1 Step 4 above |
| Undisposed `StreamPool`s | Task 2 Step 1 above |
| Unconditional `DangerousAcceptAnyServerCertificateValidator` | Task 2 Step 2 above |
| Stopping the debugger orphans the Phoria Server process | Task 7 Step 3 above (documented with dev guidance) |

Update each entry's body with a resolution line referencing the close-out task.

- [ ] **Step 2: Record the IMemoryPoolFactory<byte> assessment**

Add a resolution to that entry:

> **Resolution (2026-08-02):** Assessed and deferred post-1.0. `MemoryPool<byte>` provides fixed-size rented blocks via `Rent()/IMemoryOwner<byte>`, not an expanding stream with `IBufferWriter<byte>`, `GetReadOnlySequence()`, and `Stream` semantics. Phoria's consumers (`PhoriaIslandHtmlContent`, `PhoriaIslandPropsSerializer`, `PhoriaIslandSsr`) depend on all three. Adopting `IMemoryPoolFactory<byte>` would require either rewriting consumers against `Memory<byte>`/`ReadOnlySequence<byte>` (breaking `StreamContent`, `CopyToAsync`, the HTML writer path) or building a new stream wrapper around `MemoryPool<byte>` that reimplements the `RecyclableMemoryStream` API — effectively reinventing `Microsoft.IO.RecyclableMemoryStream`. The existing `RecyclableMemoryStreamManager` with configurable block/buffer sizes and aggressive buffer return is appropriate for this use case. Revisit if a future `Microsoft.IO` release accepts `MemoryPool<byte>` as a backing allocator.

- [ ] **Step 3: Record environmental limitations**

Add a note to the plan documenting the two items that are environmental rather than code defects:
- Aspire CLI 13.4.6 non-interactive SIGINT semantics (DCP resource cleanup) — requires an Aspire library upgrade; not a Phoria code fix. `aspire stop` provides reliable teardown in scripts.
- Node shutdown OTel events not observed before parent exit — observability gap. The explicit OTel flush/shutdown path in `server.ts` is present; this needs a collector-backed integration test.

---

## Deferred items — final status after close-out

| Item | Disposition | Rationale |
|------|------------|-----------|
| `IMemoryPoolFactory<byte>` adoption | Deferred post-1.0 | Assessed in Task 8. `MemoryPool<byte>` lacks stream/I/O semantics required by Phoria's consumers. Not a drop-in replacement. |
| Debugger-stop orphaning | Documented | Host/debugger behavior. Task 7 documents dev workflow guidance. |
| Aspire CLI 13.4.6 SIGINT semantics | Environmental | Needs Aspire library upgrade. Not a Phoria code fix. `aspire stop` available. |
| Node shutdown OTel delivery | Observability gap | Not a correctness issue. Flush/shutdown path exists. Needs collector-backed test. |

No other Phase 2 items remain open.
