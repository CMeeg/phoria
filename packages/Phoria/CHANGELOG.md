# phoria-dotnet

## 0.5.0-beta.0

### Minor Changes

- c716413: Target net8.0 and net10.0. net9.0 support is dropped (EOL 2026-11-10). Tests migrated to xUnit v3 on Microsoft.Testing.Platform.
- 6803d37: Add opt-in OpenTelemetry tracing and metrics alongside structured logging for Phoria hosts and example Phoria Server sidecars, including SSR span correlation, request metrics, configurable signal gates, and health-check log de-duplication.
- 6803d37: Configurable Phoria Server unavailable policy (`Degrade`/`Fail`), a startup timeout, bounded process restarts, an opt-in `AddPhoriaServerHealthCheck`, and correctness fixes that keep entry tags and last-known status sane while the server is down.

### Patch Changes

- 6803d37: Graceful stop of the Phoria server process on host shutdown: `StopServer` sends SIGTERM and waits a grace period before force-killing the process tree, instead of immediately SIGKILLing the process, and the host's shutdown signal now triggers `StopServer` rather than being passed to CliWrap's `ListenAsync` (whose cancellation would SIGKILL the node process before the graceful path could run).
- 6803d37: Suppress the "doesn't have CSS chunks" warning for the `<phoria-island-styles/>` element. Rendering nothing when the entry has no CSS is expected for this optional element, so the warning is now only logged when an explicit `<link rel="stylesheet" phoria-href="...">` references an entry without CSS.
- 6803d37: Fix Phoria health-check client diagnostics and example Aspire endpoint schemes while preserving OpenTelemetry log, metric, and trace export.

## 0.4.2

### Patch Changes

- f3f0e0b: Add `PhoriaIslandComponentFactory`

## 0.4.1

### Patch Changes

- 2881b78: Fix build and runtime errors when running Vite outside of the web app directory

## 0.4.0

### Minor Changes

- 95978f0: Rename some types to fix some inconsistencies and conflicts

## 0.3.0

### Minor Changes

- 1b81b31: Update to target Vite 6

## 0.2.0

### Minor Changes

- 914dd0d: Update to dotnet 9

## 0.1.1

### Minor Changes

- 8f93f57: First release of 🏝️ Phoria Islands for dotnet.
