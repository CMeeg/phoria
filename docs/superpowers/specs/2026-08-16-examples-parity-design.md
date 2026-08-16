# Examples Parity Design

## Context

The current repository contains `getting-started` and `framework-multiple`, while the archived `CMeeg/phoria-examples` repository contains seven additional examples: `framework-react`, `framework-vue`, `framework-svelte`, `with-workspace`, `with-tailwind`, `with-styled-components`, and `with-storybook`.

The examples phase is split so parity work can land before the existing Docs phase. The parity work reuses the current AppHost/Aspire, OpenTelemetry, standalone WebApp workspace, and end-to-end test template rather than restoring the obsolete example layout.

## Goals

- Recreate all seven missing examples using the current production template.
- Preserve standalone `giget` consumption and published-package references.
- Give every example a consistent build, preview, health-check, and e2e contract.
- Support the canonical `apps/WebApp` plus `packages/ui` workspace layout in the example tooling.
- Bound examples-sync CI quota with an explicit e2e allow-list while retaining full local coverage.
- Document the example contract for future contributors.

## Non-goals

- Adding new framework integrations such as Preact.
- Adding deployment-specific examples such as Render or Azure.
- Archiving the old repository during this phase; that happens after the parity work reaches `main`.
- Moving the existing single-app examples to the `apps/WebApp` layout.
- Changing Phoria framework APIs to support styled-components.

## Example Layouts

Existing examples retain `WebApp/` at the example root. `with-workspace` uses the canonical workspace layout:

```text
with-workspace/
  AppHost/
  apps/
    WebApp/
  packages/
    ui/
  Directory.Build.props
  Directory.Packages.props
  global.json
  nuget.config
  pnpm-workspace.yaml
  pnpm-lock.yaml
```

The workspace includes `apps/*` and `packages/*`. `packages/ui` owns a shared component used by a Phoria island in `apps/WebApp`; it exists to demonstrate a useful workspace boundary rather than merely adding a second package.

The WebApp does not contain a nested workspace file. Its commands run from `apps/WebApp`, while pnpm resolves the nearest workspace at the example root. The example root owns the workspace lockfile and shared configuration. Aspire and Docker paths explicitly target `apps/WebApp`.

## Example Contract

Each parity example must provide:

- AppHost, `aspire.config.json`, Docker Compose, `nuget.config`, solution, and shared .NET configuration.
- A WebApp package with `build`, `dev`, `dev:server`, `dev:webapp`, `preview`, `preview:server`, `stop`, `test:e2e`, `check`, and `lint` scripts.
- `ui/src/register.ts`, `ui/src/server.ts`, and `ui/src/entry-server.ts`.
- Preview and Production appsettings, a unique e2e port, and `/health` returning `Healthy`.
- `vitest.e2e.config.ts` and a smoke test using `PHORIA_WEBAPP_URL`.
- A README describing installation, development, preview, and the example-specific feature.

The existing examples remain valid examples of the contract and are included in all catalog and standalone-build verification.

## Tooling Changes

### Example Discovery

`scripts/examples.js` and `scripts/examples-e2e.js` must discover either `WebApp/package.json` or `apps/WebApp/package.json`. Discovery should return the example root and WebApp directory separately so all path calculations are layout-independent.

The examples utility must derive local package references and the .NET `ProjectReference` from the relative path between the discovered WebApp directory and the repository root. It must derive `Directory.Packages.props` from the example root rather than from a fixed WebApp parent.

Install, frozen-install, and lockfile operations must use the example root when it contains `pnpm-workspace.yaml`; otherwise they use the WebApp directory. This keeps the existing examples unchanged while correctly handling `with-workspace`'s root lockfile.

### E2E Allow-List

`scripts/examples-e2e.js` keeps an explicit port map and adds these ports:

| Example | Port |
| --- | ---: |
| `framework-react` | 5173 |
| `framework-vue` | 5273 |
| `framework-svelte` | 5473 |
| `with-workspace` | 5673 |
| `with-tailwind` | 5773 |
| `with-styled-components` | 5873 |
| `with-storybook` | 5973 |

The script accepts `EXAMPLES_E2E` as a comma-separated allow-list. An unset variable runs every discovered example. The examples-sync workflow sets it to `getting-started,framework-multiple,with-workspace` to cover the existing templates and the alternate workspace layout without starting all nine preview stacks. Local invocation remains unrestricted by default.

The allow-list parser should be a small pure function with focused tests. Unknown names should fail clearly rather than silently producing an empty run.

## Parity Examples

The old examples' components, pages, and explanatory content are reused where still relevant, but obsolete Lerna, Nx, root-package, and pre-Aspire markers are removed.

### Framework Examples

`framework-react`, `framework-vue`, and `framework-svelte` are single-framework versions of `framework-multiple`. Each retains the current server and e2e contract while removing the unused framework integrations. Its smoke test verifies the framework's island and counter.

### Tailwind

`with-tailwind` uses Tailwind CSS v4.2.2 or newer through `@tailwindcss/vite` and imports Tailwind from the application stylesheet with `@import "tailwindcss"`. No legacy Tailwind configuration file is required. The build verifies the Vite 8 integration.

### Styled Components

`with-styled-components` remains a React example and uses the existing `renderComponent` option on `PhoriaIsland.render`. Its server entry wraps the component in `ServerStyleSheet` and `StyleSheetManager`, renders to a string, and prepends `sheet.getStyleTags()` to the rendered HTML. This avoids coupling the example to the Node-stream-specific styled-components interleaving API.

The client initially uses styled-components without a Babel transform plugin. A plugin is only added if verification finds a real hydration or class-name mismatch. The e2e smoke test verifies that server-rendered style tags are present.

### Storybook

`with-storybook` adds Storybook 10.x with `@storybook/react-vite`, a basic story set, and a Storybook build command. Its application e2e test remains the standard Phoria smoke test. Because Storybook currently has an open runtime regression with Vite 8.1.x/Rolldown, this example pins Vite to `~8.0.16`; the README records the reason and the condition for removing the pin.

## Documentation

Add an `examples/README.md` index and a README in every example. Add a contributor checklist covering the example contract, discovery layout, port map, e2e smoke test, published dependency references, and standalone build check.

The milestone phase list must be renumbered as follows:

1. Phases 0 through 4 remain unchanged.
2. Phase 5 becomes Examples: parity.
3. Docs, Vite assets, DX/tooling, and exploration become Phases 6 through 9.
4. The existing new-examples scope becomes Phase 10.
5. Release prep becomes Phase 11.

All phase cross-references must be swept when the milestone is updated, including the deferred `ViteChunk.Name` reference.

## Verification

- Run `pnpm examples:check` against all nine examples.
- Fetch each committed example with giget and run standalone `pnpm install` and `pnpm build`.
- Run unrestricted local `pnpm examples:e2e`.
- Run the examples-sync allow-list with the three configured CI examples.
- Run the repository build, lint, check, and test commands after tooling changes.
- Build the Storybook example separately with its pinned Vite version.

## Risks

- The alternate workspace layout affects path, lockfile, and package-link logic across all example scripts; path handling must be tested against both layouts.
- Storybook remains dependent on an upstream Rolldown regression workaround. The pin is intentionally local so other examples can track current Vite 8.
- Styled-components SSR uses an example-local adapter seam. It must be verified for both server output and client hydration without changing framework package APIs.
