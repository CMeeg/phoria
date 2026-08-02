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
