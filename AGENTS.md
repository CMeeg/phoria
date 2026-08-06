# AGENTS.md

Instructions for AI agents working in this repository.

## Project Overview

Phoria is an Islands architecture framework for .NET powered by Vite. It renders islands of interactivity using React, Svelte, or Vue within .NET web apps (Razor Pages or MVC) via both CSR and SSR.

**Current focus:** driving the project to a stable, production-ready `1.0.0`. See [`docs/PROJECT.md`](docs/PROJECT.md) for the v1 milestone scope, phases, and open questions.

## Repository Structure

Monorepo using **pnpm workspaces** + **Turborepo** (task running/caching) + **Changesets** (publishing).

```
packages/
  phoria-islands/       @phoria/phoria - Core Vite plugin + client/server entry
  phoria-react/         @phoria/phoria-react - React integration
  phoria-svelte/        @phoria/phoria-svelte - Svelte integration
  phoria-vue/           @phoria/phoria-vue - Vue integration
  vite-plugin-dotnet-dev-certs/  @phoria/vite-plugin-dotnet-dev-certs - A Vite plugin to integrate dotnet dev-certs
  Phoria/               Phoria (.NET) - NuGet package with TagHelpers, SSR, server process
  Phoria.Tests/         Phoria (.NET) test project
examples/
  getting-started/      Single-framework example (React)
  framework-multiple/   Multi-framework example (React + Svelte + Vue)
```

## Prerequisites

- **Node.js** v24.18.0 (see `.nvmrc`)
- **pnpm** 11.17.0 (see `packageManager` in `package.json`)
- **.NET SDK** 10.0.302 (see `global.json`, rolls forward to latest feature)

## Commands

### Install

```bash
pnpm install
```

### Build (all packages)

```bash
pnpm build
```

Build order is handled by Turborepo: each package's `build` depends on `^build` (dependencies built first). Framework-specific packages (`phoria-react`, `phoria-svelte`, `phoria-vue`) depend on `@phoria/phoria`.

### Lint

```bash
pnpm lint
```

Runs **Biome** (`biome check`) on each package.

### Type Check

```bash
pnpm check
```

Runs `tsc` (no emit) on each package.

### Test

```bash
pnpm test            # Vitest unit tests across JS packages (via Turborepo)
pnpm test:browser    # Vitest browser-mode component tests (Playwright provider)
dotnet test --solution Phoria.sln --configuration Release  # xUnit v3 tests for the Phoria .NET package
```

`global.json` sets `test.runner: Microsoft.Testing.Platform`, so `dotnet test` runs in MTP mode — pass `--solution <path>` (not a bare path) to run every project's test executable across both target frameworks.

Example e2e test (requires a preview build running):

```bash
cd examples/framework-multiple/WebApp && pnpm test:e2e
```

### CI Order

The CI pipeline runs: `build` → `lint` → `check` → `test`. Always build before linting, type-checking, or testing.

## Code Style

### TypeScript / JavaScript

Enforced by **Biome** (config: `biome.jsonc` at repo root):

- **Indent**: tabs (not spaces)
- **Semicolons**: as-needed (omit when possible)
- **Trailing commas**: none
- **Line width**: 120
- **Line endings**: lf
- **Imports**: auto-organized by Biome
- **Constants**: `lowerCamelCase` (e.g. `jsPackages`, `root`) — no `UPPER_SNAKE_CASE`

Run Biome manually: `pnpm biome check <path>` or `pnpm biome check --write <path>` to auto-fix.

### C# (.NET)

- Nullable reference types: enabled
- Implicit usings: enabled
- Language version: 13.0 (pinned — see `Directory.Build.props`; `latest` is not per-TFM and would offer C# 15 to the `net8.0` build on the .NET 10 SDK)
- Central package management via `Directory.Packages.props`
- The `Phoria.csproj` targets `net8.0;net10.0` (net9.0 dropped — see `docs/PROJECT.md`)
- **Comments are opt-in, not expected**: only add them when they explain a non-obvious decision (e.g. the `process.cwd()` constraint comment in the example AppHost `Program.cs`) — never to restate what the code already says

## Gotchas

- **Biome 2 formats `package.json` with `expand: always`**, matching Changesets output — no exclusion needed.
- **Framework packages need `cross-env NODE_ENV=production`** in their build scripts (e.g., `phoria-react`). The core `phoria-islands` package does not.
- **The .NET solution (`Phoria.sln`) only contains the `Phoria` NuGet package**, not the example apps. Build .NET projects via their individual `.csproj` or the example `package.json` scripts.
- **Each JS package has 4 entry points**: `.` (main), `./client`, `./server`, `./vite`. Changes to one entry don't affect others.
- **Workspace dependencies** use `workspace:*` protocol and are resolved by pnpm.
- **Peer dependencies matter**: framework packages peer-depend on `@phoria/phoria` at `>=0.4.0 <1.0.0` (widened from `~0.4.0` to prevent premature `1.0.0` releases via the changesets peer cascade) — version bumps need care. This must be reconciled when all packages reach `1.0.0` (Phase 5).
- **Vite 8 uses Rolldown/Oxc** — `rollupOptions` is deprecated in favour of `rolldownOptions` in build config.
- **`resolve.tsconfigPaths: true`** (built into Vite 8) replaces the separate `vite-tsconfig-paths` plugin — do not reintroduce the plugin.
- **All pnpm settings live in `pnpm-workspace.yaml`**, not `package.json`/`.npmrc` (e.g. `packageExtensions`, `peerDependencyRules`, catalogs).

## Versioning & Publishing

Uses **Changesets** (`pnpm changeset` to create). Release flow:

1. `pnpm changeset` — create a changeset describing the change
2. Merge to `main` — CI runs `changesets/action` which opens a "Release" PR
3. Merge the Release PR — publishes to npm (JS packages) and NuGet (Phoria .NET)

NuGet publishing uses the `scripts/dotnet/publish.js` script via `pnpm --filter phoria-dotnet run publish`.

## Examples

`examples/` contains standalone example apps (user-facing), distinct from the `e2e/` workspace integration tests (removed; e2e coverage now lives in the examples and their test suites). Examples are **deliberately outside the pnpm workspace**: the root `pnpm-workspace.yaml` globs do not match them, and each example ships its own `WebApp/pnpm-workspace.yaml` + committed `WebApp/pnpm-lock.yaml`.

- Committed examples reference the **published** phoria packages (registry ranges), so a fresh clone or giget fetch can `pnpm install && pnpm build` them standalone. CI enforces this via `pnpm examples:check` — it fails if an example commits a `link:`/`file:` phoria ref or a Phoria `ProjectReference`.
- Local development against in-repo packages: run `pnpm examples:link` to switch the example to `link:` refs + a `ProjectReference` (run `pnpm build` at the repo root first so linked `dist` exists), and `pnpm examples:sync` when done to restore the committed registry state. Never commit a linked example — `pnpm examples:check` rejects it.
- The release flow keeps examples current: after `changeset publish`, `pnpm examples:bump` rewrites each example's refs to the just-released versions and regenerates the lockfiles.
- pnpm uses the **nearest** `pnpm-workspace.yaml`, so running commands inside an example's directory shadows the repo-root workspace — no `--ignore-workspace` flag needed. pnpm 11 reads build-script settings from `pnpm-workspace.yaml` (the `pnpm` field in `package.json` is ignored), so each example carries its own `allowBuilds` (esbuild, Biome, etc.).
- Each example WebApp's `package.json` scripts run from `WebApp/` (the Vite dev server only discovers config in the spawned working directory): `build` (Vite islands + .NET), `dev` (`aspire run`), `dev:server` (Phoria Server with Vite HMR via tsx), `preview` (`aspire start --environment Preview`), `stop` (`aspire stop --all`), `test:e2e` (Vitest), plus `lint`/`check`.

**The Vite dev server resolves `root` and `cwd` from `process.cwd()` and only discovers `vite.config.ts` in the current working directory.** Every dev/prod flow therefore spawns the Node process from a specific working directory:

- Each example AppHost ships a `Properties/launchSettings.json` with a `Development` profile (matching the official `aspire-apphost` template) that sets `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT`. Without it, `aspire run` would default the csproj AppHost to Production — the docs' "Development by default" only applies via the launch profile or the single-file AppHost path.
- The example AppHost owns Node via `AddJavaScriptApp`, running `dev:server` (Development) or `preview:server` (Preview) from the `WebApp/` directory. In Production the .NET host owns Node via `Phoria:Server:Process` (`appsettings.Production.json` spawning `node ui/dist/server/server.js` from the content root).

Keep the config in the directory the spawned process starts in; a config one level up is not discovered. Relative `root`/`cwd` resolve against the spawn directory, not the config file.

`aspire run`/`aspire start` resolve the committed `aspire.config.json` in the directory they run from (the example root, found from the `WebApp/` script cwd). `aspire stop --all` stops all discoverable AppHosts without an interactive target selection.

Aspire CLI 13.4.6 may leave DCP-managed resources running after non-interactive SIGINT; use `aspire stop` when scripted teardown is required.
