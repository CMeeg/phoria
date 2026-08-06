# Design: Convert e2e apps to standalone examples and remove the e2e directory

## Problem

The repo has three `e2e/` apps (`framework-multiple`, `with-sidecar`, `with-workspace`) that are full .NET + Vite integration-test apps in the pnpm workspace. The `examples/` directory already hosts standalone, user-facing examples that are deliberately outside the workspace and reference published packages.

The author wants to:

1. Convert `e2e/framework-multiple` into a standalone example at `examples/framework-multiple`, following the `examples/getting-started` conventions.
2. Remove `e2e/with-sidecar` and `e2e/with-workspace` completely.
3. Remove every reference to and usage of the e2e apps across the repo.

This is a deferred item in `examples/TODO.md`: "Convert `e2e/framework-multiple`, `e2e/with-sidecar`, `e2e/with-workspace` into standalone examples, and decide how CI runs their e2e tests."

## Decisions (confirmed with the author)

- **CI**: Drop the `test-e2e` CI job entirely. A future CI job that exercises the examples will replace it later.
- **Fold-ins from the deleted apps**: Preserve the `IPhoriaIslandComponentFactory` samples (`Components/ReactCounterTagHelper.cs`, `Components/ReactCounterViewComponent.cs` + `ReactCounterProps`) and the `?framework=` query-filter demo from with-workspace by folding them into `examples/framework-multiple`. The with-sidecar developer-owned-Node dev mode is dropped (framework-multiple already covers AppHost-owned dev/preview and WebApp-owned production).
- **Committed state**: All examples are committed in **registry state** per AGENTS.md (`examples:check` must pass). `examples/getting-started` is currently committed in a linked state (`file://` refs + ProjectReference) and must be brought up to the convention too.
- **Azure/infra/Dockerfile**: Dropped from the converted example (getting-started has none). A TODO records bringing them back later.

## Known limitation (WIP)

The examples are committed in registry state while the `feature/server` branch's development changes are **not yet released**. The published packages (`@phoria/phoria@0.4.2`, `@phoria/phoria-react@0.4.2`, `@phoria/phoria-svelte@0.3.2`, `@phoria/phoria-vue@0.3.2`, `@phoria/vite-plugin-dotnet-dev-certs@0.2.1`, `Phoria` NuGet `0.4.2`) lag the in-repo source. Therefore the examples **will not build or run without error against published versions right now** — `pnpm install && pnpm build` / `aspire` flows resolving registry ranges are expected to fail or error until the WIP changes ship.

Consequences:

- `examples:check` (structural) must pass.
- Full build/run verification of the registry-state examples is **deferred** until the packages are released.
- The converted example is smoke-tested in **linked** state (`pnpm examples:link`) against in-repo builds instead.

## Approach

### 1. Convert `e2e/framework-multiple` → `examples/framework-multiple`

Restructured to match `examples/getting-started`:

```
examples/framework-multiple/
  .editorconfig  .gitignore  .nvmrc  aspire.config.json  biome.jsonc
  Directory.Build.props  Directory.Packages.props  FrameworkMultiple.slnx
  global.json  nuget.config
  AppHost/                       # renamed from Phoria.AppHost
  WebApp/                        # JS workspace root lives here
    package.json  pnpm-workspace.yaml  pnpm-lock.yaml (committed)
    vite.config.ts  tsconfig.json  tsconfig.node.json  vitest.e2e.config.ts
    svelte.config.js  postcss.config.cjs  WebApp.csproj  Program.cs
    appsettings*.json  Properties/launchSettings.json  Pages/  Components/
    ui/                          # public/, src/, tests/e2e/smoke.test.ts
    wwwroot/
```

Conversion deltas:

- **Registry refs**: `@phoria/phoria` `^0.4.2`, `@phoria/phoria-react` `^0.4.2`, `@phoria/phoria-svelte` `^0.3.2`, `@phoria/phoria-vue` `^0.3.2`, `@phoria/vite-plugin-dotnet-dev-certs` `^0.2.1`; all `catalog:` refs → literal ranges (copied from the root catalog). csproj → `<PackageReference Include="Phoria" />`; `Directory.Packages.props` gains `<PackageVersion Include="Phoria" Version="0.4.2" />`. `FrameworkMultiple.slnx` lists only AppHost + WebApp (no source-project reference).
- **Path/cwd rewrites**: `phoria({ cwd: "WebApp" })` → `phoria()`; `phoria.root: "WebApp/ui"` → `"ui"`; Production process args → `["ui/dist/server/server.js"]`; scripts `./WebApp/ui/...` → `./ui/...`; `~/*` alias → `./ui/src/*`.
- **AppHost**: `AddJavaScriptApp(webAppDirectory)`, no workspace-root computation; keeps `dev:server`/`preview:server`, the `Development` launch profile, and `WithHttpEndpoint` at the configured phoria port.
- **WebApp/Program.cs**: aligned to getting-started's simpler form. The Preview `Server.Process = null` override is dropped (Preview is AppHost-owned with no process config in `appsettings.Preview.json`; Production remains WebApp-owned via `appsettings.Production.json`).
- **Ports**: webapp `http 5573` / `https 6573`, phoria server `5473` — distinct from getting-started's `5373`/`6373`/`5273`.
- **Fold-ins**: `Components/ReactCounterTagHelper.cs`, `Components/ReactCounterViewComponent.cs`, `Components/ReactCounterProps`, and the `?framework=react|vue|svelte` query-filter in `Index.cshtml.cs`.
- **Tests**: moved to `WebApp/ui/tests/e2e/smoke.test.ts`; default URL `http://localhost:5573`; assertions extended to cover the three framework counters, the factory components, and the `?framework=` filter.
- **Formatting**: moved files reformatted to the example's Biome config (space indent, `trailingCommas: all`).
- **Dropped**: `azure.yaml`, `infra/`, `WebApp/Dockerfile` (TODO to bring back later).

### 2. Delete `e2e/` entirely

`git rm -r e2e` (with-sidecar, with-workspace, and framework-multiple after its content is moved). No e2e READMEs or docs to preserve.

### 3. Workspace / CI / config updates

- `pnpm-workspace.yaml`: remove the three e2e globs; remove `injectWorkspacePackages: true` + its comment (only needed by the e2e Dockerfiles' `pnpm deploy`).
- `.changeset/config.json`: remove the `ignore` list (the apps are no longer in the release graph).
- `turbo.json`: remove the now-dead `build:islands` and `preview` tasks.
- `.github/workflows/ci.yml`: remove the `test-e2e` job and the Aspire CLI install step.
- `scripts/examples.js`: add `@phoria/phoria-svelte` (`packages/phoria-svelte`) and `@phoria/phoria-vue` (`packages/phoria-vue`) to `jsPackages` so `link`/`sync`/`check`/`bump` handle them — otherwise `examples:check` cannot catch `link:`/`file:` refs on those two packages.
- Regenerate root `pnpm-lock.yaml` (`pnpm install`).

### 4. Bring `examples/getting-started` to registry state

- Run `pnpm examples:bump` (JS refs → `^0.4.2`/`^0.2.1`, csproj → `<PackageReference Include="Phoria" />`, adds the Phoria `PackageVersion`, regenerates the example lockfile).
- Manually remove the `../../packages/Phoria/Phoria.csproj` entry from `GettingStarted.slnx`.
- Result: **all** examples pass `examples:check`.

### 5. Docs (live docs only; historical plans/specs/MEMORY left as records)

- `AGENTS.md`: repo structure (e2e → examples), remove the E2E Apps section, rework the `test:e2e` command and e2e gotchas into examples guidance.
- `docs/PROJECT.md` + `docs/ARCHITECTURE.md`: reword e2e mentions to past/generic phrasing.
- `TODO.md` + `examples/TODO.md`: clear out e2e-specific open items; mark the conversion done; record the "bring Azure/infra/Dockerfile back" and "future examples CI job" TODOs.

## Verification

- `pnpm install` (regenerates the root lockfile).
- `pnpm examples:check` — must pass.
- `pnpm build` then `pnpm lint` then `pnpm check` then `pnpm test` then `pnpm test:browser`.
- `dotnet test --solution Phoria.sln --configuration Release`.
- In `examples/framework-multiple/WebApp`: `pnpm install`, `pnpm lint`, `pnpm check`, and build/run smoke-tested in **linked** state (`pnpm examples:link` + `pnpm build`) since registry builds are deferred until the WIP release.

## Out of scope

- A future CI job that runs the examples' e2e suites (tracked as a TODO).
- Azure deployment scaffolding / Dockerfile for the examples (tracked as a TODO).
- Rewriting historical plan/spec/MEMORY documents.
