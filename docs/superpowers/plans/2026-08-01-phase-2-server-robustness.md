# Phase 2 — Server Robustness: Observations & Initial Scope

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Seed the Phase 2 ("Server robustness & production-readiness", per `docs/PROJECT.md`) implementation plan with the observations gathered while running the `e2e/framework-multiple` preview (a build-time CA1873 warning and a non-graceful Ctrl+C shutdown), and consolidate them with the already-tracked Phase 2 deferred issues so this document is the single scope reference for Phase 2.

**Architecture:** Two distinct shutdown paths are involved. (1) **e2e preview orchestration** — Aspire AppHost runs the .NET web app and the Phoria Server (h3/node) side-by-side as siblings; neither owns the other, so graceful shutdown is an orchestration concern. (2) **Sidecar process ownership** — in Production (`appsettings.Production.json`) the .NET app *spawns* the Phoria Server via `PhoriaServerProcess`, so graceful shutdown of the node process is a .NET hosting concern. Both paths need hardening. The existing e2e apps exercise sibling orchestration; a new minimal `with-sidecar` app exercises .NET-owned process orchestration.

**Tech Stack:** C#/.NET 10 (`PhoriaServerProcess`, `PhoriaServerProcessService`, CliWrap, Aspire AppHost), TypeScript (`e2e` server entries, h3/listhen), Node process orchestration (Aspire for preview; a maintained signal-forwarding runner for parallel builds), OpenTelemetry logging (`OpenTelemetry.Extensions.Logging`/OTLP on .NET and `@opentelemetry/sdk-logs`/OTLP HTTP on Node — see Task 7).

## Global Constraints

- This is a **capture/scoping document**, not the full Phase 2 implementation. The fixes below execute as Phase 2 work (after Phase 1 / CI tasks are complete); nothing here should be smuggled into dependency-upgrade or CI tasks.
- The known deferred Phase 2 issues (`Process.Kill()` process-tree bug,
  `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s,
  unconditional `DangerousAcceptAnyServerCertificateValidator`,
  `IMemoryPoolFactory<byte>` adoption) now live in this document's own
  "Known deferred issues" section (Task 5) — they were migrated out of
  `docs/deferred-issues-phase-1.md`, which is trimmed accordingly and retains
  only the entries unrelated to Phase 2 (dependency tracker, phase-1/phase-3
  code-quality follow-ups).
- `run-p` replacement is a **review** task for parallel build scripts only: prefer a maintained library over a hand-rolled script; candidates must be evaluated, not assumed. `concurrently` is one candidate, not a decision. Aspire AppHost replaces `run-p preview:*` for the e2e preview path.
- Repo style: Biome for TS/JSON/markdown (tabs, line width 120); tabs + 4-space indent for C#. `dotnet` build order: `build` → `lint` → `check` → `test`.
- CA1873 is enabled because `packages/Phoria/Phoria.csproj` sets `<AnalysisLevel>latest-recommended</AnalysisLevel>` (recommended rules, .NET 10).

---

### Task 1: Fix the CA1873 warning in `PhoriaServerProcess`

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs` (line 91)

**Interfaces:**
- Produces: Release builds of `packages/Phoria` compile without the CA1873 warning `PhoriaServerProcess.cs(91,62)`.

- [ ] **Step 1: Guard the expensive logging argument with `IsEnabled`**

`string.Join(" ", processOptions.Arguments ?? [])` is evaluated eagerly at the call site of the source-generated `[LoggerMessage]` method even when `Information` logging is disabled, which is what CA1873 flags (see <https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873>). The documented fix is to guard the call:

```csharp
if (logger.IsEnabled(LogLevel.Information))
{
    logger.LogServerProcessIsStarting(processOptions.Command, string.Join(" ", processOptions.Arguments ?? []));
}
```

- [ ] **Step 2: Verify the warning is gone**

Run: `dotnet build packages/Phoria/Phoria.csproj --configuration Release`
Expected: exit 0, and no `CA1873` warning in the output. Also confirm no other CA1873 warnings surface elsewhere in the package once this one is fixed.

---

### Task 2: Harden the Phoria Server (node) shutdown handler

**Files:**
- Modify: `e2e/framework-multiple/WebApp/ui/src/server.ts` (shutdown handler, lines 108-129)
- Modify: `e2e/with-workspace/WebApp/ui/src/server.ts` (same snippet)
- Modify: `docs/guides/getting-started.md` (the server script snippet, lines ~360-381)

**Interfaces:**
- Produces: `listener.close()` resolves promptly on SIGINT/SIGTERM instead of waiting up to the keep-alive timeout for the .NET app's pooled HTTP connection to close; the 5s force-exit fallback remains as a safety net.

- [ ] **Step 1: Close idle keep-alive connections before/while closing the listener**

listhen's `Listener` exposes the underlying Node server (`listener.server`, confirmed in `listhen/dist/shared/*.d.ts`), and Node ≥18.2 provides `closeIdleConnections()`. In `shutdown()`, drop idle connections so `close()` does not stall:

```ts
function shutdown(signal: NodeJS.Signals) {
  console.log(`Received signal ${signal}. Shutting down server.`)

  void listener.close().then(() => {
    console.log("Server listener closed.")
    process.exit(0)
  })

  // Drop idle keep-alive connections so close() doesn't wait for them
  listener.server.closeIdleConnections()

  // Force shutdown after 5 seconds
  setTimeout(() => {
    console.error("Could not shutdown gracefully. Forcefully shutting down server.")
    process.exit(1)
  }, 5000)
}
```

- [ ] **Step 2: Mirror the change in the second e2e app and the guide**

Apply the identical change to `e2e/with-workspace/WebApp/ui/src/server.ts` and to the server script in `docs/guides/getting-started.md` so end users get the hardened snippet.

- [ ] **Step 3: Verify**

Run: `pnpm --filter framework-multiple check` and `pnpm --filter with-workspace check` (or the equivalent `tsc`), plus `pnpm biome check --write` on the changed files. Expected: no type errors, no lint errors. Live signal behavior is verified in Task 6.

---

### Task 3: Review and replace `run-p` in parallel build scripts

**Background:**

`npm-run-all@4.1.5` (pinned via `pnpm-workspace.yaml` catalog) registers **zero signal handlers**. That makes it unsuitable for preview orchestration, but preview is now owned by Aspire AppHost (Task 7). The build scripts still need a cross-platform parallel runner, so this task evaluates and replaces `run-p` for `build:*` only. Do not use this task to select the preview orchestrator.

**Files:**
- Modify: `e2e/framework-multiple/package.json` (`build` script and runner dependency)
- Modify: `e2e/with-workspace/WebApp/package.json` (`build` script and runner dependency)
- Modify: `docs/guides/building-for-production.md` (documented `build` script pattern; preview is updated in Task 7)
- Modify: `pnpm-workspace.yaml` (catalog entry) — remove `npm-run-all` if no remaining consumer, or retain it only if another script still requires it

**Interfaces:**
- Produces: parallel `build:*` scripts use the selected maintained runner, while preview orchestration is explicitly delegated to the Aspire AppHost defined in Task 7.

- [ ] **Step 1: Evaluate candidates against the build requirements**

Candidates (evaluate, don't assume): `concurrently` (`-k`/`--success` flags, well-tested signal forwarding), `npm-run-all` 6.x/forks, `lil-js/run`, and `execa`-based orchestration. Requirements: (a) runs the `build:*` scripts in parallel; (b) preserves useful failure status; (c) is platform-agnostic and Windows-safe; (d) is maintained; (e) does not require keeping the old `preview:*` glob. Record the chosen library and rejected alternatives (with reasons) in this task's notes.

- [ ] **Step 2: Apply the replacement to build scripts**

Update both e2e `package.json` files' `build` scripts, the `pnpm-workspace.yaml` catalog, and the guide's build section to the chosen library. Remove the old `preview` script pattern only when Task 7 adds each AppHost-backed preview command. Run `pnpm install` to update the lockfile.

- [ ] **Step 3: Verify parallel builds**

Run: `pnpm --filter framework-multiple build` and the equivalent `pnpm --filter with-workspace build` from its WebApp directory.
Expected: both build children run in parallel, failures propagate with a non-zero status, and successful builds exit 0. Preview shutdown is verified in Task 7 and Task 6 through Aspire.

---

### Task 4: Library-level graceful process stop in `PhoriaServerProcess`

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs` (`StopServer()`, lines 138-165)
- Reference: `packages/Phoria/Server/PhoriaServerProcessService.cs` (line 12 TODO)
- Reference: `docs/deferred-issues-phase-1.md` → "`Process.Kill()` does not kill the entire process tree"

**Interfaces:**
- Produces: when the .NET host owns the Phoria Server process (Production sidecar model), shutdown sends a graceful signal first, waits a grace period, then force-kills the process tree — so the node server runs its SIGTERM handler instead of being SIGKILLed.

- [ ] **Step 1: Design the termination sequence**

`Process.Kill()` sends SIGKILL on Unix; the node server's graceful handlers never run. Proposed sequence for `StopServer()`:
1. Send SIGTERM to the process (via `kill -TERM <pid>` using CliWrap — already a dependency — or P/Invoke `kill(2)`). On Windows, `Process.Kill()` (TerminateProcess) is the only option and is acceptable.
2. Wait a grace period (align with the node server's 5s force-exit window, e.g. 5-6s) for the process to exit on its own.
3. Fallback: `Process.Kill(entireProcessTree: true)` so any children (e.g. esbuild in dev-mode vite) are also terminated — this resolves the deferred process-tree bug.

Record the design decision (kill command vs P/Invoke; grace-period value) and any .NET 10-specific API that supersedes it before implementing.

- [ ] **Step 2: Implement and unit-test**

Implement the sequence in `StopServer()` and add unit tests covering: process not running, process exits within grace period, process must be force-killed. `packages/Phoria.Tests` currently has no `PhoriaServerProcess` coverage — this is the first.

- [ ] **Step 3: Address the `PhoriaServerProcessService` debugger-stop TODO**

The TODO at `PhoriaServerProcessService.cs:12` ("Process is not being killed when stoppingToken is triggered — seems only a problem when stopping the debugger") should be revisited: stopping the debugger kills the host without running graceful shutdown, orphaning the node process. If still reproducible after Step 2, record a dedicated follow-up issue with repro steps.

---

### Task 5: Consolidate the known Phase 2 deferred issues into this doc

**Files:**
- Modify: this document (new "Known deferred issues" section)
- Reference: `docs/deferred-issues-phase-1.md`

- [x] **Step 1: Import the five deferred entries and trim the source**

Copy the five Phase 2 entries from `docs/deferred-issues-phase-1.md` (process-tree kill, `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, unconditional `DangerousAcceptAnyServerCertificateValidator`, `IMemoryPoolFactory<byte>` adoption) into a "Known deferred issues" section here, with their labels/milestones, so this document is the single Phase 2 scope reference. Remove the "Phase 2 — Server Robustness" section from `docs/deferred-issues-phase-1.md` (superseded by this section, not duplicated) and replace it with a one-line pointer to this document. Leave the rest of that file untouched — the dependency-tracker entry and the phase-1/phase-3 code-quality follow-ups are unrelated to Phase 2 and stay there until filed as their own issues.

Done 2026-08-01 — see "Known deferred issues" section below; `docs/deferred-issues-phase-1.md` trimmed accordingly.

- [x] **Step 2: Cross-link this document**

Add a reference to this plan from `docs/PROJECT.md`'s Phase 2 bullet and from `docs/deferred-issues-phase-1.md` so both point here.

Done 2026-08-01 — both files updated.

## Known deferred issues

Migrated from `docs/deferred-issues-phase-1.md` (2026-08-01), where they were
recorded at Phase 1 close-out as explicitly out-of-scope for a
dependency-upgrade phase. `gh` was not available in that environment (or this
one), so none have been filed as real GitHub issues yet — each entry below is
meant to become one issue when `gh` access (or the GitHub UI) is available.

### `Process.Kill()` does not kill the entire process tree

**Body:** `PhoriaServerProcess` (or wherever the Node server process is
stopped) calls `Process.Kill()` without `entireProcessTree: true`. This is
suspected to be the root cause of the known server-process shutdown bug
described in `docs/PROJECT.md` (reproduces mainly when stopping the
debugger). Fix: pass `entireProcessTree: true` (or the .NET 10 equivalent)
so child processes spawned by the Node server are also terminated. See
Task 4 above, which implements this fix.

**Labels:** `phase-2`, `server-robustness`, `bug`
**Milestone:** v1

---

### `StartServer`/`StopServer` has a semaphore race

**Body:** The server process lifecycle's `StartServer`/`StopServer` methods
have a race condition around the semaphore guarding concurrent
start/stop calls. Needs a lifecycle-hardening pass as part of Phase 2's
"in-process start, monitor/reconnect, graceful degradation" work.

**Labels:** `phase-2`, `server-robustness`, `bug`
**Milestone:** v1

---

### Undisposed `StreamPool`s

**Body:** One or more `StreamPool` instances (wrapping
`RecyclableMemoryStream`) are created but never disposed, per the plan's
Task 6 self-review notes. Needs an audit of `Phoria.IO.StreamPool`
lifetimes and proper `IDisposable` cleanup wired into DI/service lifetimes.

**Labels:** `phase-2`, `server-robustness`, `bug`
**Milestone:** v1

---

### Unconditional `DangerousAcceptAnyServerCertificateValidator`

**Body:** The server process's HTTP client (or equivalent) unconditionally
accepts any server certificate via
`DangerousAcceptAnyServerCertificateValidator`, with no environment gating.
This should be restricted to development/preview scenarios (e.g. paired
with `@phoria/vite-plugin-dotnet-dev-certs`) and never active in
production.

**Labels:** `phase-2`, `server-robustness`, `security`
**Milestone:** v1

---

### Adopt `IMemoryPoolFactory<byte>` for `Phoria.IO.StreamPool`

**Body:** .NET 10 introduces `IMemoryPoolFactory<byte>` as a more modern
alternative to hand-rolled `RecyclableMemoryStream` pooling. `StreamPool`
currently exposes `RecyclableMemoryStream` publicly, and consumers rely on
`GetReadOnlySequence()`, an `IBufferWriter<byte>` cast, and `Stream`
semantics — none of which `MemoryPool<byte>` provides directly. This is a
**public-API refactor** of `Phoria.IO`, not a drop-in dependency bump;
scope it as a deliberate design task in Phase 2, alongside the other
`Phoria.IO`/server-robustness fixes above.

**Labels:** `phase-2`, `server-robustness`, `enhancement`
**Milestone:** v1

---

### Task 6: Live verification of the shutdown paths

**Files:**
- None (verification only)

- [ ] **Step 1: Reproduce the current shutdown behavior (baseline)**

Run the pre-Aspire sibling preview commands before implementing Task 7: `pnpm --filter framework-multiple build`, then start its `preview:webapp` and `preview:server` scripts using the existing `preview` command; exercise the app; press Ctrl+C.
Expected (current, known-ugly): dotnet logs "Application is shutting down...", pnpm prints `[ERR_PNPM_RECURSIVE_RUN_FIRST_FAIL]` and `[ELIFECYCLE] Command failed`, and a second `^C^C` may be required. Capture the node server's output and confirm whether `Received signal SIGINT. Shutting down server.` appears at all (it may be lost on the broken stdout pipe).

- [ ] **Step 2: Confirm the keep-alive `close()` hypothesis**

While the baseline is running, note whether node's shutdown message appears and how long the process lingers. This validates whether Task 2's `closeIdleConnections()` is the fix for the lingering, or whether the linger is purely the dead parent's broken pipe (in which case Task 3 or Task 7 is the fix). If the linger persists after Tasks 2, 3, and 7, investigate the .NET → node HTTP keep-alive connection lifecycle.

- [ ] **Step 3: Re-verify after Tasks 2, 3, 7, and 8**

After Task 2, verify the direct Node shutdown handler. After Task 3, verify parallel builds. After Task 7, run the Aspire sibling apps. After Task 8, run the Aspire sidecar app. Confirm each shutdown is clean (single Ctrl+C, clean exit, no orphaned processes, and no error spam).

---

### Task 7: Add OpenTelemetry logging and Aspire AppHosts

**Background:** Added to Phase 2 scope per `docs/PROJECT.md`'s "health/observability" bullet, now made concrete as OpenTelemetry logging. The .NET side has no logging provider configuration beyond the default `ILogger<T>` setup, so existing source-generated logging call sites remain unchanged. The Node/h3 side has only `console.log`/`console.error` calls in the e2e server entries. Logging only for this task — traces/metrics are explicitly out of scope for now (see `docs/PROJECT.md` Open questions); no cross-runtime log correlation is attempted.

**Files:**
- Create: `e2e/framework-multiple/Phoria.AppHost/Phoria.AppHost.csproj`
- Create: `e2e/framework-multiple/Phoria.AppHost/Program.cs`
- Create: `e2e/framework-multiple/Phoria.AppHost/appsettings.json`
- Create: `e2e/with-workspace/WebApp/Phoria.AppHost/Phoria.AppHost.csproj`
- Create: `e2e/with-workspace/WebApp/Phoria.AppHost/Program.cs`
- Create: `e2e/with-workspace/WebApp/Phoria.AppHost/appsettings.json`
- Modify: `e2e/framework-multiple/WebApp/Program.cs`
- Modify: `e2e/with-workspace/WebApp/Program.cs`
- Modify: `e2e/framework-multiple/WebApp/WebApp.csproj`
- Modify: `e2e/with-workspace/WebApp/WebApp.csproj`
- Modify: `e2e/framework-multiple/WebApp/appsettings.Preview.json`
- Modify: `e2e/with-workspace/WebApp/appsettings.Preview.json`
- Modify: `e2e/framework-multiple/WebApp/ui/src/server.ts`
- Modify: `e2e/with-workspace/WebApp/ui/src/server.ts`
- Modify: `e2e/framework-multiple/package.json`
- Modify: `e2e/with-workspace/WebApp/package.json`
- Modify: `Directory.Packages.props` and `pnpm-workspace.yaml` for dependency versions
- Modify: `docs/guides/building-for-production.md` and `docs/guides/getting-started.md` for Aspire-backed preview instructions

**Interfaces:**
- Produces: server-process lifecycle events and errors in both runtimes are emitted as structured OpenTelemetry logs; `pnpm run preview` starts an Aspire AppHost; the AppHost reads the Node command and argument list from the existing `Phoria.Server` configuration rather than duplicating it.

- [ ] **Step 1: Record and verify the integration decisions**

Use these decisions unless a compatibility check disproves one: .NET uses `OpenTelemetry.Extensions.Logging` plus `OpenTelemetry.Exporter.OpenTelemetryProtocol`; Node uses `@opentelemetry/api-logs`, `@opentelemetry/sdk-logs`, and `@opentelemetry/exporter-logs-otlp-http`; both use OTLP HTTP/protobuf; `OTEL_EXPORTER_OTLP_ENDPOINT` is configurable and the Aspire dashboard is the local target; console output remains the fallback when no endpoint is configured; dependencies stay in the e2e apps, not the published `Phoria` or `@phoria/phoria` packages. Evaluate and record rejected alternatives: raw .NET SDK logging, pino/winston transports, OTLP gRPC, a standalone dashboard container, and a full Aspire AppHost for the published package.

Use Aspire AppHost for local preview, not for the published library. The existing `framework-multiple` and `with-workspace` AppHosts each start the WebApp project and the compiled Node executable as sibling resources. Add the preview `Phoria:Server:Process` command and arguments to each WebApp's `appsettings.Preview.json`; each AppHost explicitly loads that file into its own configuration and binds it before creating the executable resource. This keeps appsettings as the single source of truth without assuming the AppHost automatically inherits WebApp configuration. The new `with-sidecar` AppHost in Task 8 starts only the WebApp project, allowing the WebApp's `PhoriaServerProcess` to own Node.

- [ ] **Step 2: Add the AppHosts for sibling preview orchestration**

Create one net10.0 AppHost project for each existing e2e app using `Aspire.Hosting`, and add a project reference to its WebApp so `Projects.WebApp` is generated. In `Program.cs`, load the WebApp `appsettings.Preview.json` using a path relative to the AppHost project, bind `Phoria:Server:Process`, and call `builder.AddProject<Projects.WebApp>("webapp")`. Add the compiled Node server as an executable resource using the bound command and arguments, set its working directory to the WebApp UI root, expose the configured Phoria server HTTP endpoint, and pass `NODE_ENV=production` and `DOTNET_ENVIRONMENT=Preview`. Configure the package scripts' `preview` command to run `aspire run` from the AppHost directory instead of `run-p preview:*`; keep `build` separate and run it first.

- [ ] **Step 3: Implement .NET-side OpenTelemetry logging**

Add centrally managed OTel package versions and e2e WebApp references. At each e2e `Program.cs` composition root, configure logging with `builder.Logging.AddOpenTelemetry(...)`, include formatted state and scopes, and add the OTLP exporter only when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Preserve the existing appsettings log-level filters and do not modify the published `Phoria.csproj`.

- [ ] **Step 4: Implement Node/h3-side OpenTelemetry logging**

Add the Node log SDK setup before creating the h3 app. Create a small local logger in each e2e server entry (or a shared e2e helper if the package boundaries permit), replacing the four lifecycle/error `console` calls with structured log records containing event name, signal, error message, stack, and cause where available. Configure the OTLP HTTP exporter from standard `OTEL_*` environment variables and retain a console fallback when no collector endpoint is configured. Do not add the logger to the published `@phoria/phoria` package or browser code.

- [ ] **Step 5: Verify the sibling AppHosts and OTel output**

Run `pnpm --filter framework-multiple build`, then `pnpm --filter framework-multiple preview`; repeat for `pnpm --filter with-workspace build` and its WebApp-directory preview. Confirm `aspire run` starts the WebApp, Node server, and dashboard, and that the dashboard is reachable at its configured local URL. Confirm structured logs are visible for startup, normal graceful stop, and forced/crash stop. Press Ctrl+C once and verify both sibling processes exit cleanly without `[ERR_PNPM_RECURSIVE_RUN_FIRST_FAIL]` or `[ELIFECYCLE]`. Run each app's TypeScript check, Biome check, and `dotnet build`.

---

### Task 8: Add the `with-sidecar` Aspire e2e app

**Background:** The existing e2e apps test Aspire-owned sibling processes. They do not exercise the production ownership path where `PhoriaServerProcess` starts and stops the Node server. Add a minimal React-only app so the two process models can be tested under the same Aspire dashboard and manual workflow without adding unrelated framework coverage.

**Files:**
- Create: `e2e/with-sidecar/package.json`
- Create: `e2e/with-sidecar/Phoria.AppHost/Phoria.AppHost.csproj`
- Create: `e2e/with-sidecar/Phoria.AppHost/Program.cs`
- Create: `e2e/with-sidecar/Phoria.AppHost/appsettings.json`
- Create: `e2e/with-sidecar/WebApp/WebApp.csproj`
- Create: `e2e/with-sidecar/WebApp/Program.cs`
- Create: `e2e/with-sidecar/WebApp/appsettings.json`
- Create: `e2e/with-sidecar/WebApp/appsettings.Production.json`
- Create: `e2e/with-sidecar/WebApp/ui/src/server.ts`
- Create: `e2e/with-sidecar/WebApp/ui/src/entry-client.tsx`
- Create: `e2e/with-sidecar/WebApp/ui/src/entry-server.tsx`
- Create: `e2e/with-sidecar/WebApp/Pages/Index.cshtml`
- Create: `e2e/with-sidecar/WebApp/Pages/Index.cshtml.cs`
- Create: `e2e/with-sidecar/WebApp/vite.config.ts` and TypeScript configuration files following the existing e2e app pattern
- Modify: workspace package/catalog configuration only if the new app requires a dependency not already present

**Interfaces:**
- Consumes: the OTel logging and AppHost conventions established in Task 7, and the graceful process lifecycle implemented in Task 4.
- Produces: a minimal e2e app whose AppHost starts only the WebApp; the WebApp reads `Phoria:Server:Process` from appsettings and starts the compiled Node server through `PhoriaServerProcess`.

- [ ] **Step 1: Scaffold the minimal React Phoria app**

Copy the smallest working WebApp and React island structure from the existing e2e apps, retaining `AddPhoria()`, `UsePhoria()`, one SSR entry, one client entry, and one rendered island. Add the app to the workspace scripts so `build`, `check`, `lint`, and `preview` can be run independently.

- [ ] **Step 2: Configure .NET-owned Node process startup**

Set `Phoria:Server:Process:Command` to `node` and `Phoria:Server:Process:Arguments` to the compiled server path in the sidecar app's production configuration. Add the WebApp project reference to the AppHost and configure it to start only the WebApp project with `DOTNET_ENVIRONMENT=Production`; do not register Node as a separate AppHost executable. Pass the OTLP endpoint environment variables through the WebApp so the child Node process inherits them.

- [ ] **Step 3: Add OTel logging and lifecycle verification**

Apply the Task 7 .NET and Node logging setup to the new app. Start it with `dotnet run --project Phoria.AppHost` (or the repository's `aspire run` equivalent), confirm the Node process is a child of the .NET process, and confirm logs from both runtimes appear in the Aspire dashboard. Exercise normal stop, forced stop, and process-tree termination; verify the Node SIGTERM handler runs before the grace-period fallback and that no orphaned Node process remains.

- [ ] **Step 4: Run the app's automated checks**

Run the new app's build, TypeScript check, Biome check, and WebApp `dotnet build --configuration Release`. Run the existing smoke-test path against the app if its route and launch contract can be added without weakening the current framework-multiple smoke test.
