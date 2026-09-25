# Configurable Phoria Server unavailable policy + WebApp health check

Date: 2026-08-11 · Status: Approved for implementation

## Problem

When the WebApp is up but the Phoria Server is not, Phoria's behavior is one-size-fits-all and partially un-decided. Today the server monitor blocks in the background until its first healthy `/hc`, the WebApp serves anyway, and per-render degradation applies: isomorphic islands fall back to client-only, `ServerOnly` islands throw `PhoriaIslandComponentException` which `PhoriaIslandTagHelper` **silently suppresses** (open `TODO: Log or throw exception?`), and the process supervisor (when the app owns the process) restarts Node **indefinitely**. There is no way for a consumer to choose to fail fast instead, no signal an orchestrator can act on, and an unhealthy status leaves `ServerStatus.Mode` defaulting to `Development`, which makes the entry tag helpers emit dev URLs in production while the server is down (open TODO at `PhoriaIslandEntryTagHelper.cs:126`).

## Goals

- Make the WebApp's behavior when the Phoria Server is unavailable a **consumer choice**, not a fixed default: either carry on with best-effort degraded service, or report the whole app down.
- Add a **fail-fast startup timeout**: if the server never becomes healthy within the configured window, the WebApp fails to start instead of serving degraded forever.
- **Bound the process supervisor's recovery attempts** so an app-owned sidecar does not restart forever.
- Expose a **WebApp health check** whose status reflects the Phoria Server, so an orchestrator (Kubernetes, Azure Container Apps, Docker) can restart the app when recovery has failed.
- Fix the latent correctness issues while unhealthy: entry tags must not emit dev URLs in production, and the monitor should preserve the last-known healthy mode.
- Update both example apps with the new options and health-check wiring.

## Non-goals

- Serving the built client bundle from .NET as a fallback. In production the Phoria Server serves the client assets, so a degraded page is best-effort (HTML renders, islands stay inert until recovery). Making degraded CSR fully usable is out of scope.
- Changing the default behavior. Every new option defaults to today's behavior, so this is purely opt-in.
- Bounding restart attempts by time window (only a count), or fail-on-`Degrade` semantics.

## Design

### Configuration surface

New enum `PhoriaServerUnavailableBehavior` and three new options, all in `packages/Phoria/PhoriaOptions.cs` (all defaults preserve current behavior):

```csharp
public enum PhoriaServerUnavailableBehavior { Degrade, Fail }

// PhoriaServerOptions
public int StartupTimeout { get; set; }                       // seconds; 0 = wait indefinitely (default)
public PhoriaServerUnavailableBehavior UnavailableBehavior { get; set; } = PhoriaServerUnavailableBehavior.Degrade;

// PhoriaServerOptions.ProcessOptions
public int MaxRestartAttempts { get; set; }                   // 0 = unlimited (default)
```

Bound from `phoria:server:startupTimeout`, `phoria:server:unavailableBehavior`, and `phoria:server:process:maxRestartAttempts`. The `int`-seconds style matches the existing `HealthCheckInterval`/`HealthCheckTimeout` options. These options are .NET-only — the Node-side `appsettings.ts` parser ignores unknown keys, so no JS-side mirror is needed.

The policy enum lives in `PhoriaServerStatus.cs`, next to the existing `PhoriaServerHealth`/`PhoriaServerMode` enums, so all server status types share one home.

### Startup fail-fast

`PhoriaServerMonitor.StartMonitoring` (`packages/Phoria/Server/PhoriaServerMonitor.cs`): when `StartupTimeout > 0`, await the existing `firstHealthy` TCS with a timeout — `firstHealthy.Task.WaitAsync(TimeSpan.FromSeconds(options.Server.StartupTimeout), cancellationToken)`. A `TimeoutException` propagates when the server never becomes healthy, logged first via a new error message `LogServerStartupTimeout(url, seconds)` (new `EventId.Server.ServerStartupTimeout`).

`PhoriaServerMonitorService.ExecuteAsync` (`packages/Phoria/Server/PhoriaServerMonitorService.cs`) is restructured so any non-cancellation failure stops the monitor cleanly then rethrows:

```csharp
try
{
    await serverMonitor.StartMonitoring(stoppingToken);
    await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
}
catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
{
    await serverMonitor.StopMonitoring();
}
catch
{
    await serverMonitor.StopMonitoring();
    throw;
}
```

`StopMonitoring` cancels the background poll loop and disposes the timer/semaphore, so the timeout path leaks nothing. With the framework default `BackgroundServiceExceptionBehavior.StopHost`, the rethrown `TimeoutException` stops the host — the WebApp fails to start.

The timeout applies to **both** policies: setting it means "if the server isn't up in time, don't start"; leaving it at `0` (default) in `Degrade` mode serves degraded and reports `Degraded` via the health check.

### Runtime behavior: Degrade vs Fail

`PhoriaIslandComponentFactory` (`packages/Phoria/Islands/PhoriaIslandComponentFactory.cs`) already receives `IOptions<PhoriaOptions>`; the behavior comes from `options.Server.UnavailableBehavior`:

- `Fail` + unhealthy → both isomorphic **and** server-only islands throw `PhoriaIslandComponentException` → page 500. `ClientOnly` unaffected.
- `Degrade` + unhealthy → isomorphic degrades to client-only + existing warning (unchanged); server-only logs a **new warning** `LogServerUnhealthySuppressingComponent(component)` (new `EventId.Islands.ServerUnhealthySuppressingComponent`) before throwing, so the suppression that follows is no longer silent.

`PhoriaIslandTagHelper` (`packages/Phoria/Islands/PhoriaIslandTagHelper.cs`) gains `IOptions<PhoriaOptions>` and makes its catch policy-aware: catch `PhoriaIslandComponentException` → suppress in `Degrade` mode (resolving the "Log or throw exception?" TODO — it is now logged by the factory), **rethrow** in `Fail` mode so the page 500s instead of silently dropping the island.

`PhoriaServerMiddleware` (`packages/Phoria/Server/PhoriaServerMiddleware.cs`) gains `IOptions<PhoriaOptions>`. In `Fail` mode:
- unclaimed GETs return **503** when `ServerStatus.Health != Healthy` (instead of `await next` falling through to 404), logged via a new warning `LogServerUnavailable` (new `EventId.Server.MiddlewareServerUnavailable`);
- a mid-proxy `HttpRequestException` (server died between the health gate and the proxy call) also yields 503 in `Fail` mode instead of falling through.

`Degrade` mode keeps today's fall-through behavior. The HMR WebSocket proxy is only reached when healthy, so it is unaffected.

### Bounded recovery

`PhoriaServerProcess` (`packages/Phoria/Server/PhoriaServerProcess.cs`) tracks restart attempts in the supervision loop:

- Reset the counter to `0` whenever the monitor reports `Healthy` (in `EnsureProcessIsRunning`).
- Increment each time the spawned process exits while the server is unhealthy.
- When `MaxRestartAttempts > 0` is exceeded, log a new error `LogServerRestartLimitExceeded` (new `EventId.Server.ServerRestartLimitExceeded`), stop spawning, and end the supervision loop — `StartServer` returns without killing any live process. The `finally` block's `StopServer()` call is a no-op for a dead process; a live process must not be terminated by the give-up path.
- The monitor keeps polling independently — the server may still come back externally, and the health check keeps reflecting reality.

Escalation after exhaustion is delegated to the policy + health check: `Fail` → the app 503s + reports `Unhealthy` + the orchestrator restarts the container (fresh attempts); `Degrade` → stays degraded.

### WebApp health check

New opt-in pieces in `packages/Phoria/Health/`, no new package dependency (`IHealthCheck`/`IHealthChecksBuilder` ship in `Microsoft.AspNetCore.App`):

- `PhoriaServerHealthCheck : IHealthCheck` — reads `IPhoriaServerMonitor` and `IOptions<PhoriaOptions>`; reports `Healthy` when the monitor is healthy, otherwise `Degraded` under the `Degrade` policy and `Unhealthy` under `Fail`. `Unknown` (still starting) counts as `Degraded`/`Unhealthy` per policy. Descriptions state the server URL and which state applies.
- `PhoriaHealthChecksBuilderExtensions.AddPhoriaServerHealthCheck(this IHealthChecksBuilder builder, string name = "phoria-server")` — registers the check. Consumers opt in with `builder.Services.AddPhoria(); ... builder.Services.AddHealthChecks().AddPhoriaServerHealthCheck(); app.MapHealthChecks("/health");`.

### Correctness fixes while unhealthy

- `PhoriaIslandEntryTagHelper` (`packages/Phoria/Islands/PhoriaIslandEntryTagHelper.cs`): when `ServerStatus.Health != Healthy`, **suppress** script/link output and log a new warning `LogEntryTagsSuppressedWhileUnhealthy` (new `EventId.Islands.EntryTagsSuppressedWhileUnhealthy`). This resolves the TODO at line 126 ("When the server was unhealthy in prod this was spitting out the original attribute value, but we prob want to suppress output in that case") and eliminates the dev-URL-in-production path entirely — today an unhealthy/unknown status defaults `Mode` to `Development`, emitting `{serverUrl}/@vite/client` in production.
- `PhoriaServerMonitor.CreateUnhealthyServerStatus` **preserves the last-known healthy `Mode` and `Frameworks`** so the status record reflects reality through a downtime (matters for health-check descriptions and any consumer reading `ServerStatus.Mode`).

### Example app updates

Both `examples/getting-started` and `examples/framework-multiple`:

- `WebApp/Program.cs`: add `builder.Services.AddHealthChecks().AddPhoriaServerHealthCheck();` and `app.MapHealthChecks("/health");`.
- `WebApp/appsettings.Production.json` (app-owned process): add `server: { startupTimeout: 60, unavailableBehavior: "Fail", process: { maxRestartAttempts: 5 } }` alongside the existing `process.command`/`arguments` — production fails fast on a dead sidecar and lets an orchestrator restart the container.
- `WebApp/appsettings.Preview.json` (AppHost-owned Node): add `server: { startupTimeout: 60, unavailableBehavior: "Fail" }` — no `process.maxRestartAttempts` because the WebApp does not own the process in Preview.
- Development: leave defaults (`Degrade`, no timeout) so the dev workflow is unchanged.

## Testing

- `PhoriaServerMonitorTests`: startup timeout throws `TimeoutException` after the window and stops monitoring cleanly; unhealthy status preserves the last-known `Mode`/`Frameworks`; healthy transition restores state.
- `PhoriaServerProcessTests`: restart counter resets on healthy; increments per exit; stops spawning after the limit; the give-up path does not kill a live process; the supervision loop ends.
- `PhoriaIslandSsrLifecycleTests` (or a new factory test): `Fail` policy + unhealthy → isomorphic and server-only both throw; `Degrade` policy → isomorphic degrades, server-only throws after a warning is logged.
- New `PhoriaIslandTagHelper` test: `Degrade` suppresses, `Fail` rethrows.
- New `PhoriaServerMiddleware` tests: `Fail` + unhealthy → 503 on unclaimed GET; mid-proxy `HttpRequestException` → 503 in `Fail`, fall-through in `Degrade`; `Degrade` + unhealthy → fall-through as today.
- New `PhoriaServerHealthCheck` tests: status mapping for healthy/unhealthy × policy.
- `PhoriaIslandEntryTagHelperTests`: healthy path unchanged; add an unhealthy-stub case asserting suppression.
- Existing constructor usages affected by the new `IOptions<PhoriaOptions>` parameter: `PhoriaIslandComponentFactory` (already has it), `PhoriaIslandTagHelper` (no direct test construction — DI resolves it), `PhoriaServerMiddleware` (internal, no existing tests). `PhoriaServerMonitorTests` constructs the monitor directly and is unaffected (timeout comes from `options`).
- Examples: extend the `framework-multiple` e2e smoke test (`ui/tests/e2e/smoke.test.ts`) with a `/health` assertion returning `Healthy` while the sidecar is up. A deeper "stop the sidecar mid-run and assert 503/degradation" e2e is deliberately out of scope — the sidecar is owned by Aspire and stopping it from the test is operationally fragile; the behavior is covered by unit tests.

## Documentation

- `docs/ARCHITECTURE.md`: options table (three new options + enum), the "Health, startup, and degradation" section, and the middleware/process/monitor bullets describing the new behavior.
- `docs/MEMORY.md`: dated entry capturing the decisions.
- `docs/guides/deployment.md`: short note showing the Production options alongside the existing `server.process` example.
- The placeholder guides (`configuration.md`, `phoria-server.md`) are out of scope — they are declared work-in-progress and documenting the options belongs in `ARCHITECTURE.md`.

## Changeset

Phoria (.NET) **minor** (new options, new health-check extension, opt-in behavior). No JS packages change.

## Open questions

- None blocking. The restart counter is a straight count reset on healthy; a time-window-based bound was considered and rejected as unnecessary complexity for v1.
