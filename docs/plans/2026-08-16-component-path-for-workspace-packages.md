# Component Paths for Workspace Packages

**Date:** 2026-08-16

**Spec:** [`docs/2026-08-16-component-path-for-workspace-packages.md`](../2026-08-16-component-path-for-workspace-packages.md)

## Goal

Make direct component imports from opted-in workspace packages emit production `modulepreload` links. The implementation must make `__phoriaComponentPath` equal the consuming app's root-relative `ssr-manifest.json` key for both app-root modules and modules resolved outside the Vite root.

## Architecture

The shared framework-plugin shell belongs in `@phoria/phoria/vite`, not in three parallel framework implementations. Add a parameterized `createPhoriaFrameworkPlugin` factory to the core package. It owns workspace-package resolution, environment hooks, root capture, and the component-path transform. React, Svelte, and Vue retain their framework-specific plugin wrappers and pass their include/exclude globs, optimize-dependency entries, and SSR externals to the factory.

The factory is exported from the existing `@phoria/phoria/vite` entry. Framework packages already peer-depend on `@phoria/phoria`, their Vite builds externalize that peer, and consuming apps already import this entry for the `phoria` app plugin. Tighten the framework peer lower bound to the core minor that ships the factory.

Keep the established component-path wire-contract names unchanged: `__phoriaComponentPath`, `componentPath`, `PhoriaIsland.ComponentPath`, and `x-phoria-island-path`. The new `framework` naming applies to the shared factory, options type, module, and tests.

## Global Constraints

- The transform applies to `client` and `ssr`, never the Phoria Server `server` environment.
- The injected value is root-relative, has no leading slash, and matches the SSR manifest key exactly.
- Workspace opt-in is explicit through `workspacePackages: string[]`; do not infer packages or use marker files.
- Resolve configured packages by walking upward from `cwd` for `node_modules/<packageName>`, then use `realpathSync` to match Vite's default symlink behavior.
- Preserve `TrimStart('/')` in the .NET tag helper for existing leading-slash values, but remove the legacy Vite-root prefix stripping.
- Framework packages should no longer duplicate the shared transform implementation or depend on `magic-string` and `@rollup/pluginutils` solely for it.
- Linked example verification must finish with `pnpm examples:sync`; committed examples must pass `pnpm examples:check`.
- Use co-located tests and existing repository test seams. Do not add `InternalsVisibleTo`.

## Task 1: Core Framework Plugin Factory

**Files:** `packages/phoria-islands/src/vite/framework.ts`, `packages/phoria-islands/src/vite/framework.test.ts`, `packages/phoria-islands/src/vite/plugin.ts`, `packages/phoria-islands/package.json`

1. Add `@rollup/pluginutils: catalog:` and `magic-string: catalog:` to the core package dependencies.
2. Add `PhoriaFrameworkPluginOptions` with `name`, `include`, `exclude`, `cwd`, `workspacePackages`, `optimizeDeps`, and `ssrExternal` fields.
3. Implement `resolvePackageDir(packageName, cwd)` by walking parent directories, checking `node_modules/<packageName>`, and returning its real path. Throw an actionable error containing the package name and cwd when an opted-in package cannot be resolved.
4. Implement `createPhoriaFrameworkPlugin(options)` with these hooks:
   - `config`: create the `ssr` environment and merge the supplied optimize-dependency entries without duplicates.
   - `configEnvironment`: configure the supplied SSR external entries.
   - `configResolved`: capture `normalizePath(config.root)`.
   - `applyToEnvironment`: return true only for `client` and `ssr`.
   - `transform`: strip query/hash suffixes, bypass the framework filter only for modules under an opted-in realpathed workspace directory, derive `normalizePath(relative(root, cleanId))`, and append `export const __phoriaComponentPath = "...";` with a source map.
5. Re-export `createPhoriaFrameworkPlugin` and `PhoriaFrameworkPluginOptions` from `src/vite/plugin.ts`, keeping the existing `@phoria/phoria/vite` package entry.
6. Add core tests using temporary symlink fixtures covering:
   - root-relative app modules;
   - a Vite root different from `cwd`;
   - opted-in workspace source and `dist/index.js` modules;
   - unconfigured workspace modules remaining untouched;
   - `node_modules` exclusion;
   - unresolvable package errors;
   - optimize-dependency merging;
   - SSR external configuration; and
   - client/SSR/server environment selection.
7. Verify with `pnpm --filter @phoria/phoria exec vitest run src/vite/framework.test.ts`, `pnpm --filter @phoria/phoria build`, and Biome.

**Commit:** `feat: centralize framework island transform in @phoria/phoria/vite`

## Task 2: React Composer

**Files:** `packages/phoria-react/src/vite/plugin.ts`, `packages/phoria-react/src/vite/plugin.test.ts`, `packages/phoria-react/package.json`

1. Make `PhoriaReactPluginOptions` extend the shared options while retaining `react?: ReactOptions | false` and React's default include/exclude/cwd/workspace values.
2. Replace the local transform implementation with `createPhoriaFrameworkPlugin({ name: "phoria-react", ... })`, passing React's optimize dependencies and `@phoria/phoria-react/server` as the SSR external.
3. Keep `phoriaReact()` responsible for composing the official React Vite plugin array with the shared Phoria framework plugin.
4. Remove `magic-string` and `@rollup/pluginutils` from the React package dependencies.
5. Reduce tests to framework wiring: plugin composition, a `.tsx` transform smoke test, optimize-dependency merging, environment selection, and public option plumbing. Deep workspace-resolution cases remain covered by the core factory tests.
6. Verify with `pnpm --filter @phoria/phoria-react exec vitest run src/vite/plugin.test.ts`, package build, and Biome.

**Commit:** `feat(react): resolve component paths for opted-in workspace packages`

## Task 3: React Workspace Example Proof

**Files:** `examples/with-workspace/apps/WebApp/ui/src/components/register.ts`, `examples/with-workspace/apps/WebApp/ui/src/components/counter.tsx`, `examples/with-workspace/apps/WebApp/vite.config.ts`

1. Change the registration to load `@phoriaexamples/ui` directly with `module: () => import("@phoriaexamples/ui")`.
2. Delete the app-side `counter.tsx` re-export shim.
3. Configure `phoriaReact({ workspacePackages: ["@phoriaexamples/ui"] })`.
4. Keep the existing modulepreload smoke assertion unchanged as the regression test.
5. Verify against linked in-repo packages with:

   ```bash
   pnpm build
   pnpm examples:link
   pnpm examples:refresh
   EXAMPLES_E2E=with-workspace pnpm examples:e2e
   pnpm examples:sync
   ```

**Commit:** `feat(examples): consume workspace package islands directly`

## Task 4: Svelte Composer

**Files:** `packages/phoria-svelte/src/vite/plugin.ts`, `packages/phoria-svelte/src/vite/plugin.test.ts`, `packages/phoria-svelte/package.json`

Mirror the React composer using Svelte's `**/*.svelte` defaults, current Svelte optimize-dependency entries, and `@phoria/phoria-svelte/server` SSR external. Remove the two shared transform dependencies, retain wiring tests, and verify with the Svelte package test/build commands.

**Commit:** `feat(svelte): resolve component paths for opted-in workspace packages`

## Task 5: Vue Composer

**Files:** `packages/phoria-vue/src/vite/plugin.ts`, `packages/phoria-vue/src/vite/plugin.test.ts`, `packages/phoria-vue/package.json`

Mirror the React composer using Vue's `**/*.vue` defaults and `@phoria/phoria-vue/server` SSR external. Remove the two shared transform dependencies, retain wiring tests, and verify with the Vue package test/build commands.

**Commit:** `feat(vue): resolve component paths for opted-in workspace packages`

## Task 6: Node Header Contract Tests

**Files:** `packages/phoria-islands/tests/utilities/register-fakes.ts`, `packages/phoria-islands/src/server/vite.test.ts`

1. Extend `registerSsrComponentFramework(name, html, componentPath?)` so the fake render result can include `componentPath`.
2. Assert the existing SSR dev-handler test omits `x-phoria-island-path` when no path exists.
3. Add a test registering a workspace manifest key such as `../../../packages/ui/dist/index.js` and assert that the router emits it as `x-phoria-island-path`.
4. Verify with `pnpm --filter @phoria/phoria exec vitest run src/server/vite.test.ts`.

**Commit:** `test(islands): cover x-phoria-island-path routing contract`

## Task 7: .NET Manifest Lookup

**Files:** `packages/Phoria/Islands/PhoriaIslandPreloadTagHelper.cs`, `packages/Phoria.Tests/Islands/PhoriaIslandPreloadTagHelperTests.cs`

1. Remove the `PhoriaOptions.Root` prefix-stripping block from `PhoriaIslandPreloadTagHelper`; retain `componentPath.TrimStart('/')`.
2. Add `Process_WorkspacePackageComponentPath_EmitsModulepreloadLinks` using a custom `ViteSsrManifest` entry keyed by `../../../packages/ui/dist/index.js` and verify the expected `/ui/assets/...js` modulepreload link.
3. Verify with `dotnet test --solution Phoria.sln --configuration Release` or the focused MTP `--filter-method` form.

**Commit:** `fix: drop legacy root prefix from island preload lookup`

## Task 8: Documentation, Release Metadata, and Memory

**Files:** `docs/ARCHITECTURE.md`, `docs/PROJECT.md`, `docs/MEMORY.md`, `.changeset/component-path-workspace-packages.md`, the three framework `package.json` files

1. Update `ARCHITECTURE.md` to document the root-relative manifest-key format, opted-in workspace packages, the shared `@phoria/phoria/vite` framework factory, and the complete component-path chain.
2. Mark the `with-workspace` shim TODO in `PROJECT.md` resolved.
3. Add a changeset with minor bumps for `@phoria/phoria`, `@phoria/phoria-react`, `@phoria/phoria-svelte`, and `@phoria/phoria-vue`.
4. Tighten each framework's `@phoria/phoria` peer lower bound to the core minor that ships `createPhoriaFrameworkPlugin` (for example, `>=0.6.0-0 <2.0.0` if that is the resulting core tuple).
5. Append the approved sequencing decisions to `MEMORY.md`: explicit workspace opt-in, global root-relative wire format with .NET root-strip removal, Shape A shared factory, and React-first e2e sequencing.

**Commit:** `docs: document component path resolution for workspace packages`

## Task 9: Full Regression

Run the repository CI order:

```bash
pnpm build
pnpm lint
pnpm check
pnpm test
dotnet test --solution Phoria.sln --configuration Release
```

Then run linked e2e coverage for `with-workspace`, `framework-multiple`, and `getting-started`, restore the examples with `pnpm examples:sync`, and run `pnpm examples:check`.

No commit is required for this verification task.

## Completion Criteria

- Direct workspace-package registration emits production modulepreload links.
- The injected path matches the SSR manifest key for app-root and workspace modules.
- The shared transform and plugin shell have one implementation in core.
- React, Svelte, and Vue retain their framework-specific composition and public options.
- Existing framework-multiple, getting-started, golden-manifest, Node header, and .NET preload tests pass.
- Peer ranges prevent a framework plugin from resolving against a core package without the shared factory.
