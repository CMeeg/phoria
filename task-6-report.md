# Task 6 Report

## Changes

- Made the five-parameter `PhoriaServerProcess` constructor public.
- Removed `InternalsVisibleTo` for `Phoria.Tests` from `packages/Phoria/Phoria.csproj`.
- Renamed `e2e/with-sidecar/WebApp/appsettings.Production.json` to `appsettings.Preview.json` without changing its contents.
- Changed the with-sidecar AppHost `DOTNET_ENVIRONMENT` value from `Production` to `Preview`.
- Did not change the with-sidecar WebApp environment or process-ownership behavior.

## Verification

All requested Release builds completed successfully with 0 warnings and 0 errors:

- `dotnet build packages/Phoria/Phoria.csproj --configuration Release`
- `dotnet build e2e/framework-multiple/Phoria.AppHost/Phoria.AppHost.csproj --configuration Release`
- `dotnet build e2e/framework-multiple/WebApp/WebApp.csproj --configuration Release`
- `dotnet build e2e/with-workspace/WebApp/Phoria.AppHost/Phoria.AppHost.csproj --configuration Release`
- `dotnet build e2e/with-workspace/WebApp/WebApp.csproj --configuration Release`
- `dotnet build e2e/with-sidecar/Phoria.AppHost/Phoria.AppHost.csproj --configuration Release`
- `dotnet build e2e/with-sidecar/WebApp/WebApp.csproj --configuration Release`

`git diff --check` completed successfully.

## Concerns

The with-sidecar AppHost was built but not launched as part of this task; no runtime Preview smoke launch was requested by the implementation command.

## Review Fix

The friend-assembly cleanup exposed two test-only implementation details. Production behavior remains unchanged:

- `PhoriaServerProcessTests` now uses the six-second test default directly instead of accessing `PhoriaServerProcess.StopGracePeriod`.
- `PhoriaIslandHtmlContentTests` now uses the registered client name directly instead of accessing the internal `PhoriaServerHttpClientFactory` type.

Exact verification commands and output:

```text
$ dotnet test --solution Phoria.sln --configuration Release
Running tests from /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net10.0/Phoria.Tests.dll (net10.0|x64)
Running tests from /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net8.0/Phoria.Tests.dll (net8.0|x64)
/home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net8.0/Phoria.Tests.dll (net8.0|x64) passed (6s 745ms)
/home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net10.0/Phoria.Tests.dll (net10.0|x64) passed (6s 852ms)

Test run summary: Passed!
  /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net10.0/Phoria.Tests.dll (net10.0|x64) passed (6s 852ms)
  /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net8.0/Phoria.Tests.dll (net8.0|x64) passed (6s 745ms)

  total: 94
  failed: 0
  succeeded: 94
  skipped: 0
  duration: 7s 293ms
```

```text
$ dotnet build packages/Phoria/Phoria.csproj --configuration Release --framework net8.0
  Determining projects to restore...
  All projects are up-to-date for restore.
  Phoria -> /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria/bin/Release/net8.0/Phoria.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed: 00:00:01.03

$ dotnet build packages/Phoria/Phoria.csproj --configuration Release --framework net10.0
  Determining projects to restore...
  All projects are up-to-date for restore.
  Phoria -> /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria/bin/Release/net10.0/Phoria.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed: 00:00:01.01
```

```text
$ dotnet build packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --framework net8.0
  Determining projects to restore...
  All projects are up-to-date for restore.
  Phoria -> /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria/bin/Release/net8.0/Phoria.dll
  Phoria.Tests -> /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net8.0/Phoria.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed: 00:00:01.93

$ dotnet build packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --framework net10.0
  Determining projects to restore...
  All projects are up-to-date for restore.
  Phoria -> /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria/bin/Release/net10.0/Phoria.dll
  Phoria.Tests -> /home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout/packages/Phoria.Tests/bin/Release/net10.0/Phoria.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed: 00:00:02.10
```
