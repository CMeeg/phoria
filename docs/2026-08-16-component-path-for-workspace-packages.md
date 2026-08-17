# Component Paths For Workspace Packages

## Summary

The `with-workspace` example can render a React component imported directly from a workspace package, but the production response does not contain the expected `modulepreload` link. The component still renders and hydrates because component loading and rendering are working. The missing preload is caused by a break in Phoria's `__phoriaComponentPath` chain, not by a failure to bundle or execute the component.

The current app-side re-export shim works because it gives the framework Vite plugin an app-root module to transform. It is therefore a workaround for the current component-path contract, rather than a requirement imposed by React or pnpm.

## Observed Behavior

With the direct package registration:

```ts
module: () => import("@phoriaexamples/ui")
```

the package component renders successfully, but the production smoke test's modulepreload assertion fails. With the current shim:

```ts
module: () => import("./counter")
```

where `counter.tsx` re-exports the package component, the same assertion passes.

The failure is silent because the preload path is optional at each runtime boundary. Development mode does not expose it because `PhoriaIslandPreloadTagHelper` does not read the SSR manifest in Development mode. The meaningful symptom is in the production-shaped build and preview flow.

## The Component-Path Chain

The path is carried through several independent pieces:

1. The framework Vite plugins inject `export const __phoriaComponentPath = "..."` into component modules in their `transform` hooks. React implements this in `packages/phoria-react/src/vite/plugin.ts:77-103`; Svelte and Vue have the same mechanism.
2. `packages/phoria-islands/src/phoria-island.ts:36-65` loads the component and copies `module.__phoriaComponentPath` to `PhoriaIslandComponent.componentPath`.
3. The Phoria SSR router emits `x-phoria-island-path` only when the loaded component has a path. The .NET SSR client reads that header in `packages/Phoria/Islands/PhoriaIslandSsr.cs:64-69` and stores it as `island.ComponentPath`.
4. `packages/Phoria/Islands/PhoriaIslandPreloadTagHelper.cs:52-76` skips islands without a component path. For a path it derives the SSR manifest key and emits modulepreload and related preload links for the mapped files.

This means a component can render correctly while still losing preloads. Rendering depends on the module and framework exports; preloading additionally depends on the path metadata matching the client build's `ssr-manifest.json`.

## Root Cause

There are two related causes.

### 1. Package modules are excluded from the transform

The framework plugins use a filter with these defaults:

```ts
include: ["**/*.jsx", "**/*.tsx"]
exclude: "node_modules/**"
```

The Svelte and Vue plugins use their corresponding component extensions. A workspace package is consumed through the app's `node_modules` boundary, even when pnpm has linked it to a local workspace directory. The resolved package import therefore matches the `node_modules/**` exclusion and the transform returns without adding `__phoriaComponentPath`.

The loaded package module consequently has no path metadata. `importComponent` returns `componentPath: undefined`, the SSR router omits `x-phoria-island-path`, and the .NET preload tag helper skips the island.

The exclusion is understandable for ordinary third-party dependencies: transforming all dependency code would be expensive and could rewrite code that is not a Phoria component. It is nevertheless too broad for workspace packages that contain application-owned island components.

### 2. The current path value is not the manifest key for external modules

The plugin currently computes the value with:

```ts
const path = id.replace(cwdRegex, "")
```

This produces a path relative to `process.cwd()` when the module is inside the app root. The .NET tag helper then removes the configured `/ui` root before looking up the path in the manifest.

That happens to align for an app-root component. In the workspace example, the shim produces a path equivalent to `/ui/src/components/counter.tsx`, which becomes `src/components/counter.tsx` for the manifest lookup.

The direct package import is different. Vite resolves the workspace package outside the WebApp root, and the built manifest contains a key such as:

```json
"../../../packages/ui/dist/index.js": ["/ui/assets/counter-p788nj5T.js"]
```

The module id is therefore not prefixed by the WebApp cwd. Removing the cwd either leaves an absolute filesystem path or, depending on the resolution form, produces a `node_modules/...` path. Neither is the root-relative, base-stripped key used by the SSR manifest. Removing the `node_modules` exclusion alone would therefore not fix production preloading; the injected path semantics would still be wrong for modules outside the app root.

### Why the shim works

The shim itself is under `apps/WebApp/ui/src`. It is transformed by the framework plugin, and its path maps to the manifest key `src/components/counter.tsx`. The client bundle associates that shim entry with the chunk containing the re-exported package component, so the preload emitted for the shim also preloads the actual component code.

## Constraints For A Fix

Any change should preserve these constraints:

- The component path is a Node-to-.NET wire contract carried by `x-phoria-island-path`; both sides must agree on its exact format.
- The value ultimately needs to match a key in the client build's `ssr-manifest.json`, including Vite's treatment of workspace symlinks and package paths.
- The same component module must expose the metadata in both the client and `ssr` environments. The framework plugin's `applyToEnvironment` guard must continue to exclude the Phoria Server `server` environment, which must not receive framework transforms.
- The path must be derived in the consuming app's Vite build. A workspace package cannot safely bake an app-relative path into its own separately built distribution because the package's path depends on the consuming app's root, base, symlink settings, and bundler output.
- The solution must distinguish application-owned workspace packages from arbitrary third-party dependencies, or provide an explicit opt-in. Transforming every `node_modules` module is unnecessarily broad.
- Linked-package behavior must be checked in both development SSR loading and the production client/SSR build. Vite dependency optimization and symlink resolution can change the module id that reaches the transform and the manifest.
- Tests need to cover framework transform output, the Node-to-.NET path/header chain, manifest lookup for an external workspace module, and production e2e preloads. The existing workspace smoke test is the regression test for the user-visible behavior.

## Candidate Directions

### A. Transform opted-in workspace modules and emit manifest-relative ids

Extend the framework plugin configuration so application-owned workspace packages can be included in the transform. At the same time, change the injected value to use the consuming Vite root and the same base-stripping rules as the SSR manifest, rather than using a cwd-prefix removal.

Conceptually, the transform would produce the manifest's module key for both kinds of module:

- app-root component: `src/components/counter.tsx`
- external workspace component: `../../../packages/ui/dist/index.js`

The exact normalization must be implemented using Vite's resolved root and module id rules rather than string assumptions. The .NET tag helper and the header documentation would need to be updated if the wire value changes from the current `/ui/...` form to a manifest-key form. An alternative is to keep the existing wire shape and add a shared normalization step that converts external ids to the manifest key before the header is emitted.

This is the recommended primary direction because it repairs the existing metadata chain at its source and keeps the preload implementation path-based. The main design work is defining a narrow, predictable opt-in for workspace packages and proving that the normalized id is identical in client and SSR builds.

### B. Make component metadata explicit at registration

Add an optional component-path or manifest-key field to the registration API. The app would provide the path alongside the loader instead of relying on a transformed module export.

This is relatively simple and avoids transforming dependency code, but it moves bundler-specific knowledge into application registration. It also makes published component packages awkward: the consuming app still has to know the package's resolved module id and keep that value aligned with Vite's manifest. It is better considered a fallback or escape hatch than the default solution.

### C. Capture the module id at SSR runtime

Have the SSR loader or framework service derive the component's module id from the loaded module at request time. This would avoid a source transform, but production SSR bundles can merge modules into chunks and discard the original module boundary. Runtime URLs and `import.meta.url` therefore do not reliably identify the original client manifest key. This approach is not recommended without a Vite-supported module metadata API that remains stable after bundling.

### D. Add a manifest lookup fallback based on registration name

When `ComponentPath` is absent, the .NET side could use a separate manifest mapping from registered island name to client chunk. This would preserve rendering without requiring path metadata for package components, but it introduces a second discovery protocol and does not naturally handle multiple registrations of the same module or framework-specific aliases. It may be useful as a later resilience mechanism, but it should not replace fixing the path contract.

## Recommended Investigation

Start with Direction A as a focused design spike:

1. Capture the exact module ids seen by the framework transform and the client SSR manifest for a linked workspace package under the current Vite configuration.
2. Define one canonical module-id normalization function in the Vite side that produces the manifest lookup key for app-root and external workspace modules.
3. Decide whether workspace inclusion is configured explicitly, inferred from the workspace root, or enabled by a package-level marker.
4. Update one framework plugin first, preferably React, and prove the direct `@phoriaexamples/ui` registration emits the existing modulepreload link.
5. Mirror the behavior and tests across Svelte and Vue only after the React contract is stable.
6. Keep the re-export shim test until the direct package path is covered, then remove the shim from `with-workspace` and retain the example as the regression case.

The key acceptance criterion is not merely that the package component renders. The direct package registration must produce a component path that the .NET preload tag helper resolves to the same client chunk recorded for the package module in `ssr-manifest.json`.

## Evidence

The built `with-workspace` manifest demonstrated both sides of the current behavior:

```json
"../../../packages/ui/dist/index.js": [
  "/ui/assets/counter-p788nj5T.js"
],
"src/components/counter.tsx": [
  "/ui/assets/counter-p788nj5T.js"
]
```

The first entry is the direct workspace package module. The second is the app-root shim. Both point to the same emitted chunk, but only the shim currently supplies `__phoriaComponentPath`, so only the shim reaches the preload lookup successfully.
