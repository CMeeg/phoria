# Examples Parity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recreate the seven archived examples with the current Aspire/OpenTelemetry/WebApp/e2e template, make example tooling support both layouts, document the contract, and renumber the v1 milestone phases.

**Architecture:** Use `examples/framework-multiple` as the production template for the six single-app examples, removing unused framework integrations or adding the feature-specific integration. Build `with-workspace` with `apps/WebApp` and `packages/ui`, using computed paths and the example-root pnpm workspace. Extend the existing Node scripts rather than introducing a second example-management system.

**Tech Stack:** .NET 10, Aspire 13.4.6, Node.js 24.18.0, pnpm 11.17.0, Vite 8, TypeScript 6, React 19, Vue 3, Svelte 5, Vitest 4, Tailwind CSS 4, Storybook 10, styled-components, OpenTelemetry, Razor Pages, Docker Compose.

**Spec:** `docs/superpowers/specs/2026-08-16-examples-parity-design.md`

## Global Constraints

- Recreate `framework-react`, `framework-vue`, `framework-svelte`, `with-workspace`, `with-tailwind`, `with-styled-components`, and `with-storybook`.
- All seven new examples are single-framework examples; register the application island as `Counter` and use `StartAt = 5` on the application page. Only the existing `framework-multiple` example retains framework-prefixed registrations and different starting values.
- Preserve published package references and standalone `giget` consumption; examples must not contain `file:`/`link:` Phoria references or a Phoria `ProjectReference` in committed state.
- Existing examples retain `WebApp/` at the example root; only `with-workspace` uses `apps/WebApp` and `packages/ui`.
- Every example provides AppHost, Aspire config, Docker Compose, NuGet config, solution, shared .NET configuration, build/dev/preview/check/lint/e2e scripts, health checks, and a README.
- `scripts/examples.js` and `scripts/examples-e2e.js` must discover `WebApp/package.json` and `apps/WebApp/package.json`.
- Install, frozen-install, and lockfile operations use the example root when it contains `pnpm-workspace.yaml`; otherwise they use the WebApp directory.
- `EXAMPLES_E2E` is comma-separated; unset runs every discovered example; unknown names fail clearly.
- WebApp/e2e ports are 5173, 5273, 5473, 5673, 5773, 5873, and 5973 for the seven new examples respectively.
- Tailwind uses `@tailwindcss/vite` and Tailwind CSS v4.2.2 or newer with `@import "tailwindcss"`; no legacy config file.
- Styled-components uses the existing `renderComponent` option and an example-local SSR adapter; no Phoria framework API changes.
- Storybook uses 10.x and pins Vite to `~8.0.16` because of the current Vite 8.1.x/Rolldown regression.
- Do not add a unit-test harness for the allow-list parser; verify it through allow-list and unknown-name command paths as requested.
- Do not create changesets; no published package API or package source changes are part of this work.

---

## File Structure Map

### Existing template

Use `examples/framework-multiple` as the source for the AppHost, WebApp, OpenTelemetry, Docker, Razor Pages, and e2e contract. Use `examples/getting-started` only where its simpler React presentation or existing port pattern is useful.

### New single-app examples

Each of `framework-react`, `framework-vue`, `framework-svelte`, `with-tailwind`, `with-styled-components`, and `with-storybook` contains:

- `AppHost/` with `AppHost.csproj`, `Program.cs`, `appsettings.json`, and `Properties/launchSettings.json`.
- Root `aspire.config.json`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `nuget.config`, solution, `docker-compose.yml`, `biome.jsonc`, `.editorconfig`, `.gitignore`, `.dockerignore`, `.nvmrc`, and `README.md`.
- `WebApp/` with `WebApp.csproj`, `package.json`, `vite.config.ts`, `vitest.e2e.config.ts`, TypeScript configs, Dockerfile, four appsettings files, Razor Pages, `Program.cs`, `ui/src/server.ts`, `ui/src/entry-client.ts`, `ui/src/entry-server.ts`, `ui/src/register.ts`, framework components, styles, and e2e smoke tests.

### Alternate workspace example

`with-workspace` contains `AppHost/`, `apps/WebApp/`, `packages/ui/`, root `pnpm-workspace.yaml`, root `pnpm-lock.yaml`, root shared .NET configuration, root solution, root Docker Compose, and root README. `packages/ui` is a React component library built to `dist` with Vite lib mode and `vite-plugin-dts`. `apps/WebApp` consumes the workspace package from its island registration and builds the library before building the island bundle.

### Tooling and documentation

- `scripts/examples.js` owns discovery, local linking, syncing, checking, bumping, and refresh operations.
- `scripts/examples-e2e.js` owns port configuration, layout discovery, allow-list parsing, preview process startup, health waiting, and e2e execution.
- `.github/workflows/examples-e2e.yml` bounds examples-sync e2e execution to three examples.
- `examples/README.md` indexes all examples and contains the contributor checklist; every example has its own README.
- `docs/PROJECT.md` receives the phase insertion and renumbering; `docs/deferred-issues-phase-1.md` receives the live Phase 6-to-7 cross-reference updates.

## Implementation Tasks

### Task 1: Make `scripts/examples.js` layout-independent

**Files:**
- Modify: `scripts/examples.js`

**Interfaces:**
- Produce `findExamples()` entries shaped as `{ name, exampleDir, webAppDir }`.
- Produce `installDir(example)` as `example.exampleDir` when `pnpm-workspace.yaml` exists there, otherwise `example.webAppDir`.
- Compute local JavaScript package paths and the .NET `ProjectReference` with `relative(example.webAppDir, repositoryTarget)`.

- [ ] **Step 1: Replace string-only discovery with layout-aware entries.** Search each `examples/<name>` for `WebApp/package.json` first and `apps/WebApp/package.json` second; ignore directories without either file.
- [ ] **Step 2: Update path helpers.** Read `Directory.Packages.props` from `exampleDir`, keep `WebApp.csproj` under `webAppDir`, and compute `ProjectReference` and `file:` refs with `relative()` rather than fixed `../../../` prefixes.
- [ ] **Step 3: Route package operations through `installDir`.** Use the derived install directory for `pnpm install`, `pnpm install --force`, frozen installs, non-frozen installs, and the lockfile path; continue running `dotnet restore WebApp.csproj` from `webAppDir`.
- [ ] **Step 4: Update link, sync, check, bump, and refresh callers for the new object shape.** Preserve current registry-state restoration and catalog literalization behavior.
- [ ] **Step 5: Verify existing layouts.** Run `pnpm examples:check`, then exercise `pnpm examples:link` and `pnpm examples:sync` on an existing example and confirm the original files and lockfile are restored.

### Task 2: Add e2e ports, layout discovery, and allow-list parsing

**Files:**
- Modify: `scripts/examples-e2e.js`

**Interfaces:**
- Produce `parseExamplesE2E(value, discoveredNames)` returning the selected names.
- `undefined` or an empty value returns all discovered names.
- A non-empty value is split on commas, trimmed, deduplicated, and validated against discovered names; an unknown name throws an error listing valid names.

- [ ] **Step 1: Add the seven new WebApp ports.** Keep the existing 5373 and 5573 entries and add 5173, 5273, 5473, 5673, 5773, 5873, and 5973 for the named examples.
- [ ] **Step 2: Replace the WebApp-only discovery check.** Resolve either `examples/<name>/WebApp` or `examples/<name>/apps/WebApp` and retain the example name separately.
- [ ] **Step 3: Implement and export the pure allow-list parser.** Call it from `main()` after discovery and before any process is started so invalid names fail before builds or servers run.
- [ ] **Step 4: Update `testExample`.** Run frozen install from the derived install directory, run `pnpm build`, `node ui/dist/server/server.js`, `dotnet run`, and `pnpm test:e2e` from the discovered WebApp directory.
- [ ] **Step 5: Verify selection behavior.** Run `EXAMPLES_E2E=getting-started,framework-multiple pnpm examples:e2e` and confirm only those examples execute; run `EXAMPLES_E2E=does-not-exist pnpm examples:e2e` and confirm a non-zero exit with a clear error.

### Task 3: Bound examples-sync CI e2e execution

**Files:**
- Modify: `.github/workflows/examples-e2e.yml`

- [ ] **Step 1: Set the e2e step environment.** Add `EXAMPLES_E2E: getting-started,framework-multiple,with-workspace` to the step that runs `pnpm examples:e2e`.
- [ ] **Step 2: Keep the workflow gate unchanged.** Preserve the existing `chore/examples-sync-` branch condition, `examples:check`, and setup steps.
- [ ] **Step 3: Validate the configured command locally.** Run the same allow-list command from Task 2 after `with-workspace` exists.

### Task 4: Add the `framework-react` parity example

**Files:**
- Create: `examples/framework-react/**` using `examples/framework-multiple/**` as the template.

**Interfaces:**
- WebApp/e2e port: 5173.
- Phoria server port: 5073.
- Registered island: `Counter`.
- Smoke test marker: `react-counter`.

- [ ] **Step 1: Copy the current AppHost/WebApp contract.** Create the root shared .NET files, AppHost, solution, Docker Compose, appsettings, health endpoint, OpenTelemetry setup, Vite server, and standard e2e configuration.
- [ ] **Step 2: Remove unused framework integrations.** Keep React, `@vitejs/plugin-react`, the React counter, React types, and the React public logo; remove Vue/Svelte plugins, packages, components, shims, and assets.
- [ ] **Step 3: Make the page single-framework.** Register and render one `Counter` island with `Client.Load` and `StartAt = 5`; remove the multiple-framework query filtering from `Index.cshtml.cs`.
- [ ] **Step 4: Set ports and smoke assertions.** Set appsettings/AppHost ports to 5073/5173 and assert island markup, modulepreload, `react-counter`, and `/health` in the smoke test.
- [ ] **Step 5: Adapt the archived README.** Replace pre-Aspire, Lerna, and old giget references with `gh:cmeeg/phoria/examples/framework-react` and current `pnpm`/Aspire commands.
- [ ] **Step 6: Verify the example.** Run `pnpm examples:check`, `pnpm build`, `pnpm lint`, `pnpm check`, and `EXAMPLES_E2E=framework-react pnpm examples:e2e` from the repository root as appropriate.

### Task 5: Add the `framework-vue` parity example

**Files:**
- Create: `examples/framework-vue/**` using the current template.

**Interfaces:**
- WebApp/e2e port: 5273.
- Phoria server port: 5173.
- Registered island: `Counter`.
- Smoke test marker: `vue-counter`.

- [ ] **Step 1: Create the current AppHost/WebApp contract.** Copy the production template and rename the solution/configuration identity to `framework-vue`.
- [ ] **Step 2: Keep only Vue integration.** Retain `@vitejs/plugin-vue`, `vue-shim.d.ts`, `counter-button.vue`, and `vue.svg`; remove React/Svelte integration and dependencies.
- [ ] **Step 3: Render and register `Counter`.** Use the current SSR/client entry shape and `Client.Idle()` with `StartAt = 5`.
- [ ] **Step 4: Set ports, smoke assertions, and README.** Use 5173/5273, assert `vue-counter`, and adapt the archived README to current commands and the new repository giget path.
- [ ] **Step 5: Verify the example.** Run `pnpm examples:check`, `pnpm build`, `pnpm lint`, `pnpm check`, and `EXAMPLES_E2E=framework-vue pnpm examples:e2e`.

### Task 6: Add the `framework-svelte` parity example

**Files:**
- Create: `examples/framework-svelte/**` using the current template.

**Interfaces:**
- WebApp/e2e port: 5473.
- Phoria server port: 5373.
- Registered island: `Counter`.
- Smoke test marker: `svelte-counter`.

- [ ] **Step 1: Create the current AppHost/WebApp contract.** Copy the production template and rename the solution/configuration identity to `framework-svelte`.
- [ ] **Step 2: Keep only Svelte integration.** Retain `@sveltejs/vite-plugin-svelte`, `svelte.config.js`, `svelte-env.d.ts`, `counter.svelte`, and `svelte.svg`; remove React/Vue integration and dependencies.
- [ ] **Step 3: Render and register `Counter`.** Use the current SSR/client entry shape and `Client.Visible("50px")` with `startAt = 5`.
- [ ] **Step 4: Set ports, smoke assertions, and README.** Use 5373/5473, assert `svelte-counter`, and adapt the archived README to current commands and the new repository giget path.
- [ ] **Step 5: Verify the example.** Run `pnpm examples:check`, `pnpm build`, `pnpm lint`, `pnpm check`, and `EXAMPLES_E2E=framework-svelte pnpm examples:e2e`.

### Task 7: Add the `with-tailwind` parity example

**Files:**
- Create: `examples/with-tailwind/**` using the React parity template.
- Reuse/adapt: archived `examples/with-tailwind` CSS and README content.

**Interfaces:**
- WebApp/e2e port: 5773.
- Phoria server port: 5673.

- [ ] **Step 1: Create the React AppHost/WebApp contract.** Use the same server, health, Docker, Aspire, and smoke-test structure as `framework-react`; register the application island as `Counter` with `StartAt = 5`.
- [ ] **Step 2: Add Tailwind v4.** Add `tailwindcss` and `@tailwindcss/vite` at v4.2.2 or newer; call `tailwindcss()` from `vite.config.ts`.
- [ ] **Step 3: Replace the global stylesheet.** Use `@import "tailwindcss";` and `@source "../../../Pages"` in `ui/src/styles/global.css` so Razor markup is scanned; do not create a legacy Tailwind config.
- [ ] **Step 4: Demonstrate Tailwind in Razor markup.** Add utility classes to the page layout/cards and assert a representative class is present in the rendered HTML.
- [ ] **Step 5: Set ports and README.** Use 5673/5773 and document Tailwind v4 setup, install, dev, preview, and build commands.
- [ ] **Step 6: Verify the example.** Run `pnpm build`, inspect the generated CSS for Tailwind output, and run `EXAMPLES_E2E=with-tailwind pnpm examples:e2e`.

### Task 8: Add the `with-styled-components` parity example

**Files:**
- Create: `examples/with-styled-components/**` using the React parity template.
- Create: `examples/with-styled-components/WebApp/ui/src/entry-server.tsx`.
- Remove/replace: `examples/with-styled-components/WebApp/ui/src/entry-server.ts`.

**Interfaces:**
- WebApp/e2e port: 5873.
- Phoria server port: 5773.
- SSR adapter consumes `PhoriaIsland.render({ renderComponent })` and returns the same SSR result with style tags prepended to `html`.

- [ ] **Step 1: Create the React example contract.** Copy the current React template and add `styled-components` with its matching TypeScript types if required by the selected package version.
- [ ] **Step 2: Convert the `Counter` styling.** Register the application island as `Counter` with `StartAt = 5`; use styled-components for the visible counter/button styles while retaining the `react-counter` marker used by e2e.
- [ ] **Step 3: Implement the server adapter.** Use `isReactIsland`, `ServerStyleSheet`, `StyleSheetManager`, and `renderToString`; render non-React islands through `island.render()` and return `{ ...result, html: sheet.getStyleTags() + result.html }` for React islands; always call `sheet.seal()` in `finally`.
- [ ] **Step 4: Update entry and TypeScript configuration.** Rename the SSR entry to `.tsx`, update `phoria.ssrEntry`, and enable React JSX in the Node TypeScript config.
- [ ] **Step 5: Add SSR style assertions.** Assert that the page contains a `style[data-styled]` tag and the React counter markup; do not add a Babel transform unless verification finds a real hydration/class-name mismatch.
- [ ] **Step 6: Set ports and README.** Use 5773/5873 and document the example-local SSR adapter and the no-Babel-plugin default.
- [ ] **Step 7: Verify the example.** Run `pnpm build`, `EXAMPLES_E2E=with-styled-components pnpm examples:e2e`, and a development hydration check with `pnpm dev`.

### Task 9: Add the `with-storybook` parity example

**Files:**
- Create: `examples/with-storybook/**` using the React parity template.
- Create: `examples/with-storybook/WebApp/.storybook/main.ts`.
- Create: `examples/with-storybook/WebApp/ui/src/components/counter/Counter.stories.tsx`.

**Interfaces:**
- WebApp/e2e port: 5973.
- Phoria server port: 5873.
- Storybook build command: `pnpm build:storybook`.

- [ ] **Step 1: Create the React example contract.** Copy the current React template and add Storybook 10 dependencies with `@storybook/react-vite`; Storybook 10 provides the former essentials features through core, so do not install the incompatible `@storybook/addon-essentials` 8.x package.
- [ ] **Step 2: Pin Vite locally.** Set this example's Vite dependency to `~8.0.16`; do not change Vite versions in other examples.
- [ ] **Step 3: Configure Storybook.** Add the React Vite framework, stories under `WebApp/ui/src`, and Storybook 10's core-provided essentials behavior, with a `viteFinal` that merges the app's Phoria/dotnet-dev-certs/Vite configuration without introducing a second application server.
- [ ] **Step 4: Add the basic `Counter` stories.** Reuse the archived story structure, using `startAt: 5` for the application-facing default story.
- [ ] **Step 5: Add scripts and README.** Add `storybook` and `build:storybook`; document the Vite pin, the Rolldown regression, and the condition for removing the pin.
- [ ] **Step 6: Verify both surfaces.** Run `pnpm build:storybook`, `pnpm build`, and `EXAMPLES_E2E=with-storybook pnpm examples:e2e`.

### Task 10: Add the canonical `with-workspace` example

**Files:**
- Create: `examples/with-workspace/AppHost/**`.
- Create: `examples/with-workspace/apps/WebApp/**`.
- Create: `examples/with-workspace/packages/ui/**`.
- Create: root shared configuration, solution, Docker Compose, workspace manifest, lockfile, and README under `examples/with-workspace/`.

**Interfaces:**
- WebApp/e2e port: 5673.
- Phoria server port: 5573.
- Workspace package: `@phoriaexamples/ui`.
- Workspace package build: Vite library build with `vite-plugin-dts`.

- [ ] **Step 1: Create the example-root workspace.** Add `pnpm-workspace.yaml` with `apps/*` and `packages/*`, allow the required build scripts, and generate the committed root lockfile.
- [ ] **Step 2: Create `packages/ui`.** Export the React `Counter`, set package exports/types to the built output, configure Vite lib mode with React externalized and `vite-plugin-dts`, and add `build`, `check`, and `lint` scripts.
- [ ] **Step 3: Create `apps/WebApp`.** Adapt the React parity example without a nested workspace file or nested lockfile; depend on `@phoriaexamples/ui: workspace:*`; register the shared component as `Counter` with `StartAt = 5` in the application page.
- [ ] **Step 4: Build the workspace package before islands.** Set `build:islands` to `pnpm --filter @phoriaexamples/ui build && vite build --app`; keep the WebApp package scripts runnable from `apps/WebApp` while pnpm resolves the nearest example-root workspace.
- [ ] **Step 5: Adapt Aspire and .NET paths.** Point AppHost at `apps/WebApp`, reference `../apps/WebApp/WebApp.csproj`, keep `Directory.Build.props` and `Directory.Packages.props` at the example root, and use an example-root solution.
- [ ] **Step 6: Adapt Docker.** Use example-root build context and root lockfile; build the UI package before the WebApp islands; publish `apps/WebApp/WebApp.csproj`; copy the resulting UI dist and node runtime into the final image.
- [ ] **Step 7: Add smoke assertions and README.** Assert the shared counter island plus standard markup/modulepreload/health checks; document the workspace boundary and UI package build order.
- [ ] **Step 8: Verify the alternate layout.** Run `EXAMPLES_E2E=with-workspace pnpm examples:e2e`, then run `pnpm examples:link` and `pnpm examples:sync` for this example to validate computed refs and root-lockfile handling.

### Task 11: Document the examples catalog and contributor contract

**Files:**
- Create: `examples/README.md`.
- Create or modify: `README.md` in each of the seven new examples.

- [ ] **Step 1: Add the catalog index.** List all nine examples, their frameworks/features, layouts, WebApp/e2e ports, and primary commands.
- [ ] **Step 2: Add the contributor checklist.** Cover the required files/scripts, WebApp versus `apps/WebApp` discovery, port map, `/health`, `PHORIA_WEBAPP_URL` smoke tests, published dependency checks, and giget standalone build verification.
- [ ] **Step 3: Write each example README.** Document install, dev, preview, stop, e2e, and the feature-specific behavior; use `gh:cmeeg/phoria/examples/<name>` for giget and remove old Lerna/Nx/pre-Aspire instructions.
- [ ] **Step 4: Verify documentation references.** Check every command and path against the committed example scripts and run `pnpm examples:check`.

### Task 12: Renumber the milestone phases and live cross-references

**Files:**
- Modify: `docs/PROJECT.md`.
- Modify: `docs/deferred-issues-phase-1.md`.

- [ ] **Step 1: Insert Phase 5.** Add `Examples: parity` with links to the parity spec and this plan.
- [ ] **Step 2: Shift later phases.** Rename Docs to Phase 6, Vite assets to Phase 7, DX/tooling to Phase 8, exploration to Phase 9, the existing new-examples scope to Phase 10, and release prep to Phase 11.
- [ ] **Step 3: Sweep `PROJECT.md`.** Update the deferred `ViteChunk.Name` reference to Phase 7, HTTPS research to Phase 8, final docs pass to Phase 11, Vite asset spike to Phase 7, completed-phase ranges to include Phase 5, and stale prose referring to the original Phase 3-5 sequence.
- [ ] **Step 4: Sweep `deferred-issues-phase-1.md`.** Change all live references to the Vite assets phase from Phase 6 to Phase 7.
- [ ] **Step 5: Preserve historical records.** Do not rewrite historical plans, specs, or prior MEMORY entries merely because their phase numbering reflected the earlier milestone.
- [ ] **Step 6: Verify the sweep.** Search the two modified documents for phase references and check that links to the new spec and plan resolve.

### Task 13: Run the complete verification matrix

**Files:**
- No source changes expected; verification only.

- [ ] **Step 1: Check committed package references.** Run `pnpm examples:check` and confirm all nine examples pass.
- [ ] **Step 2: Verify standalone consumption.** Fetch each committed example with giget into a temporary directory, run standalone `pnpm install`, and run `pnpm build`.
- [ ] **Step 3: Run unrestricted local e2e.** Run `pnpm examples:e2e` and confirm all nine examples execute sequentially.
- [ ] **Step 4: Run the CI allow-list.** Run `EXAMPLES_E2E=getting-started,framework-multiple,with-workspace pnpm examples:e2e`.
- [ ] **Step 5: Verify invalid selection.** Run `EXAMPLES_E2E=does-not-exist pnpm examples:e2e` and confirm a clear non-zero failure before any stack starts.
- [ ] **Step 6: Run repository checks.** Run `pnpm build`, `pnpm lint`, `pnpm check`, and `pnpm test` in the repository's required order.
- [ ] **Step 7: Build Storybook separately.** Run `pnpm build:storybook` from `examples/with-storybook/WebApp` and confirm the pinned Vite build succeeds.
- [ ] **Step 8: Verify link/sync reversibility.** Run `pnpm examples:link` followed by `pnpm examples:sync` across all examples and confirm `git status` shows no generated link/sync changes.

## Risks and Mitigations

- **Alternate workspace paths:** Land and verify the tooling before adding `with-workspace`; validate both layouts through e2e and link/sync.
- **Storybook/Vite regression:** Keep the Vite pin local to Storybook and document the upstream condition for removal.
- **Styled-components hydration:** Verify SSR style tags and development hydration; add a transform only if a reproducible mismatch exists.
- **Example e2e cost:** Keep local execution unrestricted but bound examples-sync CI with the explicit three-example allow-list.
- **Concurrent development ports:** Preview/e2e ports are explicit and distinct; default Vite development behavior remains unchanged and is documented.

## Commit Strategy

Use one focused commit per task with repo-style messages such as `feat(examples): add framework-react parity example`, `feat(scripts): support workspace example layouts`, and `docs: document examples parity`. Do not commit until explicitly requested.
