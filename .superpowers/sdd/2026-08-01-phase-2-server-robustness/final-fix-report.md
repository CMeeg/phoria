# Phase 2 Server Robustness Final Fix

## Changes

- `PhoriaServerProcess.StopServer` now creates one shared in-flight stop task. Concurrent callers return and await that task, including the host shutdown registration and hosted-service shutdown path.
- Startup publishes a completion source under the same state lock as the process ID. Stop waits for that startup state, so a process that reports `StartedCommandEvent` during shutdown is still terminated.
- The semaphore is no longer disposed by `StopServer`; disposal remains in `Dispose` to avoid a concurrent `Release`/`Dispose` race.
- Removed the unreachable `OperationCanceledException` handler from `EnsureProcessIsRunning`.
- Added a regression test proving concurrent stop callers remain pending through the force-stop grace period.
- Both Aspire AppHosts now set the WebApp environment to `Preview`. Both WebApps retain Phoria services but clear `Phoria:Server:Process` in Preview, leaving Node ownership with the sibling AppHost while preserving the shared Preview port and process configuration. Node remains `NODE_ENV=production`.

## Verification

Commands were run from `/home/meeg/projects/cmeeg/phoria/.worktrees/phase2-server-robustness` unless noted.

```text
dotnet test packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release -- --filter-method Phoria.Tests.Server.PhoriaServerProcessTests.StopServer_ConcurrentCallersAwaitTheSameStopOperation
Result: Passed, net8.0 and net10.0, total 2, failed 0, succeeded 2, skipped 0.

dotnet test --solution Phoria.sln --configuration Release
Result: Passed, total 66, failed 0, succeeded 66, skipped 0.

dotnet build packages/Phoria/Phoria.csproj --configuration Release
Result: Build succeeded, net8.0 and net10.0, 0 warnings, 0 errors.

dotnet build WebApp/WebApp.csproj --configuration Release
dotnet build Phoria.AppHost/Phoria.AppHost.csproj --configuration Release
(run sequentially from e2e/framework-multiple)
Result: Both builds succeeded, 0 warnings, 0 errors.

dotnet build WebApp/WebApp.csproj --configuration Release
dotnet build WebApp/Phoria.AppHost/Phoria.AppHost.csproj --configuration Release
(run sequentially from e2e/with-workspace)
Result: Both builds succeeded, 0 warnings, 0 errors.

pnpm --dir e2e/framework-multiple build:islands
pnpm --dir e2e/with-workspace/WebApp build:islands
Result: Both Vite builds succeeded. Existing Svelte warning: `startAt` is captured only initially.

pnpm --dir e2e/framework-multiple check
pnpm --dir e2e/framework-multiple lint
pnpm --dir e2e/with-workspace/WebApp check
pnpm --dir e2e/with-workspace/WebApp lint
Result: TypeScript checks passed. Biome checks passed with one existing warning per sibling for the unused Vue shim suppression.

git diff --check
Result: Passed with no whitespace errors.
```

An initial parallel invocation of each sibling WebApp and AppHost build produced generated static-web-assets file contention (`rjsmrazor.dswa.cache.json` / missing `apphost`). Sequential reruns passed; this was a verification-command concurrency issue, not a source/build error.

## Residual Concerns

- No deterministic direct test seam exists for cancelling in the narrow interval between OS process launch and `StartedCommandEvent`; the synchronized startup completion path is covered indirectly by the existing host-shutdown tests and directly by the lifecycle implementation.
- The existing Svelte compiler and Biome suppression warnings remain outside this fix.
