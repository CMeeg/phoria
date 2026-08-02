# Task 7 Report

## Changes

- Updated both sibling AppHosts to select `tsx` with the source `server.ts`, `NODE_ENV=development`, and `DOTNET_ENVIRONMENT=Preview` in Development.
- Preserved the Preview configuration's `node` command and compiled arguments, with `NODE_ENV=production` and `DOTNET_ENVIRONMENT=Preview`.
- Added the exact `dev:aspire` script to both sibling packages without changing `dev` or `preview`.
- Documented Aspire as the recommended development workflow, retained the two-terminal workflow, and documented debugger-stop ownership guidance.
- Updated the Phase 2 deferred-issue entry to record debugger-stop orphaning as expected host/debugger behavior with documented guidance.

## Verification

All commands were run from `/home/meeg/projects/cmeeg/phoria/.worktrees/phase-2-server-robustness-closeout`.

### .NET builds

Commands:

```text
dotnet build e2e/framework-multiple/WebApp/WebApp.csproj --configuration Release --nologo
dotnet build e2e/framework-multiple/Phoria.AppHost/Phoria.AppHost.csproj --configuration Release --nologo
dotnet build e2e/with-workspace/WebApp/WebApp.csproj --configuration Release --nologo
dotnet build e2e/with-workspace/WebApp/Phoria.AppHost/Phoria.AppHost.csproj --configuration Release --nologo
dotnet build e2e/with-sidecar/WebApp/WebApp.csproj --configuration Release --nologo
```

Output for each command:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

The first AppHost build pass reported `CS1061` for `IHostEnvironment.IsDevelopment`; adding `using Microsoft.Extensions.Hosting;` to both AppHosts fixed the missing extension namespace. The final pass above is clean.

### TypeScript

Commands:

```text
pnpm --dir e2e/framework-multiple run check
pnpm --dir e2e/with-workspace/WebApp run check
```

Output for each command:

```text
$ tsc
```

Both exited with status 0.

### Biome and whitespace

Commands:

```text
pnpm exec biome check e2e/framework-multiple/package.json e2e/with-workspace/WebApp/package.json
git diff --check
```

Output:

```text
Checked 2 files in 13ms. No fixes applied.
```

`git diff --check` produced no output and exited with status 0.

### Vite production assets

Commands:

```text
pnpm --dir e2e/framework-multiple run build:islands
pnpm --dir e2e/with-workspace/WebApp run build:islands
```

Both built client, SSR, and server output successfully, including `dist/server/server.js`. Vite emitted the existing Svelte warning about `startAt` being captured as an initial value; this is unrelated to Task 7 and did not fail the builds.

### Aspire launches

Commands:

```text
timeout --signal=INT --kill-after=10s 25s pnpm --dir e2e/framework-multiple run dev:aspire
timeout --signal=INT --kill-after=10s 25s pnpm --dir e2e/with-workspace/WebApp run dev:aspire
timeout --signal=INT --kill-after=10s 25s pnpm --dir e2e/framework-multiple run preview
timeout --signal=INT --kill-after=10s 25s pnpm --dir e2e/with-workspace/WebApp run preview
```

Observed output for all four launches included:

```text
Starting dashboard...
Dashboard: https://localhost:<random>/login?...
Press CTRL+C to stop the AppHost and exit.
```

The Aspire CLI logs showed both `webapp` and `phoria-server` reaching `Running` and `Ready`, plus the dashboard reaching `Running` and `Ready`. Development logs showed the executable command as `tsx` with the source server path. Preview logs showed:

```text
Cmd: .../node
Args: ["dist/server/server.js"]
```

The bounded commands were interrupted by the test harness after the resources reached Ready, so their wrapper output ended with `[ELIFECYCLE] Command failed` and Aspire's `Stopping Aspire` message. This is not a resource-start failure; an interactive Ctrl+C lifecycle was not independently verified in this non-interactive shell.

## Concerns

- Aspire reported `Developer certificates may not be fully trusted (trust exit code was: PartiallyFailedToTrustTheCertificate)` in this Linux environment. The dashboard and resources still started.
- Aspire 13.4.6 non-interactive SIGINT behavior remains environment-sensitive. The guide documents Ctrl+C for interactive use and the existing plan records `aspire stop` as the reliable scripted teardown fallback.
