# AGENTS.md

Instructions for AI agents working in this repository.

## Project Overview

Phoria is an Islands architecture framework for .NET powered by Vite. It renders islands of interactivity using React, Svelte, or Vue within .NET web apps (Razor Pages or MVC) via both CSR and SSR.

**Current focus:** driving the project to a stable, production-ready `1.0.0`. See [`docs/PROJECT.md`](docs/PROJECT.md) for the v1 milestone scope, phases, and open questions.

## Repository Structure

Monorepo using **pnpm workspaces** + **Lerna** (publishing) + **Nx** (caching/task deps).

```
packages/
  phoria-islands/       @phoria/phoria - Core Vite plugin + client/server entry
  phoria-react/         @phoria/phoria-react - React integration
  phoria-svelte/        @phoria/phoria-svelte - Svelte integration
  phoria-vue/           @phoria/phoria-vue - Vue integration
  vite-plugin-dotnet-dev-certs/  @phoria/vite-plugin-dotnet-dev-certs - A Vite plugin to integrate dotnet dev-certs
  Phoria/               Phoria (.NET) - NuGet package with TagHelpers, SSR, server process
e2e/
  framework-multiple/   Test app using React + Svelte + Vue together
  with-workspace/       Test app for workspace scenarios
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
pnpm lerna run build
```

Build order is handled by Nx: each package's `build` depends on `^build` (dependencies built first). Framework-specific packages (`phoria-react`, `phoria-svelte`, `phoria-vue`) depend on `@phoria/phoria`.

### Lint

```bash
pnpm lerna run lint
```

Runs **Biome** (`biome check`) on each package.

### Type Check

```bash
pnpm lerna run check
```

Runs `tsc` (no emit) on each package.

### Test

```bash
pnpm test            # Vitest unit tests across JS packages (via Lerna)
pnpm test:browser    # Vitest browser-mode component tests (Playwright provider)
dotnet test --solution Phoria.sln --configuration Release  # xUnit v3 tests for the Phoria .NET package
```

`global.json` sets `test.runner: Microsoft.Testing.Platform`, so `dotnet test` runs in MTP mode — pass `--solution <path>` (not a bare path) to run every project's test executable across both target frameworks.

E2E smoke test (requires a preview build running):

```bash
pnpm --filter framework-multiple test:smoke
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

Run Biome manually: `pnpm biome check <path>` or `pnpm biome check --write <path>` to auto-fix.

### C# (.NET)

- Nullable reference types: enabled
- Implicit usings: enabled
- Language version: latest
- Central package management via `Directory.Packages.props`
- The `Phoria.csproj` targets `net8.0;net10.0` (net9.0 dropped — see `docs/PROJECT.md`)

## Gotchas

- **Biome 2 formats `package.json` with `expand: always`**, matching Changesets output — no exclusion needed.
- **Framework packages need `cross-env NODE_ENV=production`** in their build scripts (e.g., `phoria-react`). The core `phoria-islands` package does not.
- **The .NET solution (`Phoria.sln`) only contains the `Phoria` NuGet package**, not the e2e apps. Build .NET projects via their individual `.csproj` or the e2e `package.json` scripts.
- **Each JS package has 4 entry points**: `.` (main), `./client`, `./server`, `./vite`. Changes to one entry don't affect others.
- **Workspace dependencies** use `workspace:*` protocol and are resolved by pnpm.
- **Peer dependencies matter**: framework packages peer-depend on `@phoria/phoria` at `~0.4.0` — version bumps need care. This must be reconciled when all packages reach `1.0.0` (Phase 5).
- **Vite 8 uses Rolldown/Oxc** — `rollupOptions` is deprecated in favour of `rolldownOptions` in build config.
- **`resolve.tsconfigPaths: true`** (built into Vite 8) replaces the separate `vite-tsconfig-paths` plugin — do not reintroduce the plugin.
- **All pnpm settings live in `pnpm-workspace.yaml`**, not `package.json`/`.npmrc` (e.g. `packageExtensions`, `peerDependencyRules`, catalogs).

## Versioning & Publishing

Uses **Changesets** (`pnpm changeset` to create). Release flow:

1. `pnpm changeset` — create a changeset describing the change
2. Merge to `main` — CI runs `changesets/action` which opens a "Release" PR
3. Merge the Release PR — publishes to npm (JS packages) and NuGet (Phoria .NET)

NuGet publishing uses the `scripts/dotnet/publish.js` script via Lerna.

## E2E Apps

The `e2e/` apps are full .NET + Vite applications used for integration testing. They are **not** part of the .NET solution. Each has its own `package.json` with:

- `build` — builds both Vite (islands + server) and .NET
- `dev` — runs the Vite dev server via tsx
- `preview` — runs production builds of both Vite server and .NET app
- `lint` / `check` — Biome and TypeScript checking
