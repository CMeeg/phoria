# Phase 3 — Test Consolidation & Review Design

## Purpose

Phase 3 hardens the regression net that Phases 0–2 built so the remaining feature work (Phases 6–10) lands on the strongest possible foundation. It delivers four outcomes, all already scoped in `docs/PROJECT.md`: coverage tooling that makes test holes measurable (reporting-only, no thresholds), full Svelte/Vue ↔ React test parity, a defined and applied signal-to-noise review, and mock/stub/fake dedup. The problem definition, success criteria, and scope decisions live in `docs/PROJECT.md` (Phase 3) and `docs/MEMORY.md` (2026-08-13 entry); this document decides *how*.

Non-goals: enforced coverage thresholds (reporting-only by design), cross-package JS test sharing (packages publish independently), true Vue `csr` hydration (out of scope), browser-test coverage inclusion, and any change to the e2e smoke tests in the examples.

## Test-strategy architecture

Three test layers with unchanged boundaries:

- **Node-env unit tests** — Vitest `src/**/*.test.ts` per JS package; xUnit v3 on Microsoft.Testing.Platform for `Phoria.Tests`.
- **Browser-mode component tests** — Vitest browser mode via `@vitest/browser-playwright` (chromium), one `vitest.browser.config.ts` + `test:browser` script per framework package, `.browser.test.ts(x)` files. Only the framework packages have these; they exercise the CSR services (`csr.tsx`/`csr.ts`) against a real DOM.
- **E2e smoke** — the examples' `test:e2e` (build + preview, assert rendered island). Untouched this phase.

Two test seams:

- **JS fixtures are shared in-package only.** Cross-package test utilities are deliberately not shared because the packages publish independently; framework plugin tests stay parallel copies. This is a documented decision, not a missed dedup.
- **.NET test seams consolidate into `Phoria.Tests/TestUtilities/`** (the existing folder, today containing only `GoldenManifestFixture.cs`), as `internal` top-level classes in namespace `Phoria.Tests.TestUtilities`. No new `InternalsVisibleTo`.

## Task 0 — Non-major dependency sweep

Sweep all non-major dependencies across the workspace (pnpm catalog + non-catalog deps + root devDeps + `Directory.Packages.props` versions). The explicit objective for the test toolchain is to eliminate known Vite-8 coverage quirks by upgrading rather than mitigating:

- `@vitest/coverage-v8` ignore-hint loss with elided type-only imports (fixed upstream in the vite/oxc line; the repo is already past the first fix at `vite ^8.1.5`).
- Uncovered `.tsx` files being dropped with Rolldown parse noise during `all: true` coverage generation (upstream issue; may still be open).

Only keep a mitigation if a quirk still reproduces after the sweep, documented with the exact version it applies to. A narrow coverage `exclude` for the browser-only `csr.tsx`/`csr.ts` modules is the accepted fallback, not a workaround layer. Everything else that moves is a routine patch/minor bump with no API impact.

## Coverage tooling

### JS (Vitest)

Add `@vitest/coverage-v8` to the pnpm catalog (version-locked to `vitest`, currently `^4.1.10`). Each package's node-env `vitest.config.ts` gains a `coverage` block:

- `provider: "v8"`
- `all: true` — report every `src` file, untested or not, so the report honestly surfaces holes (the "documented + triaged" goal).
- `include: ["src/**/*.{ts,tsx}"]`, `exclude` for test files (`.test.ts`, `.browser.test.ts(x)`) and trivial re-export entry files.
- `reporter: ["text"]` in CI; `html` optional for local use.

Coverage runs through per-package scripts: each package gains `test:coverage` (`vitest run --coverage`) and the root gains `test:coverage` (`turbo run test:coverage`). The default node-env suite is the coverage surface; browser-mode tests do not contribute (accepted gap, below). Interpretation note: CSR-only modules (`csr.tsx`/`csr.ts`) show ~0% because they are exercised only by the browser config — this is intended, not a hole to chase.

### .NET (coverlet.MTP)

Add `coverlet.MTP` (MIT — consistent with the repo's FluentAssertions licensing stance) to `Directory.Packages.props` and as a `PackageReference` in `Phoria.Tests.csproj`. Run `dotnet test --solution Phoria.sln --configuration Release --coverlet --coverlet-output-format cobertura`, with `--coverlet-file-prefix` because the project targets two TFMs and coverlet.MTP writes per-run reports into the same directory. `coverlet.collector`/`coverlet.msbuild` are not usable under MTP; the Microsoft CodeCoverage extension was rejected (closed-source licensing, same model as FluentAssertions v8).

### CI

Append to the existing `build-and-test` job (no new job — avoids the per-push setup cost flagged in `TODO.md`): a `pnpm test:coverage` step, a `dotnet ... --coverlet` step, and `actions/upload-artifact` for the reports. Reporting-only; no gate, no thresholds.

## Framework test parity

React today has `vite/plugin.test.ts`, `client/csr.browser.test.tsx`, `vitest.browser.config.ts`, and a `test:browser` script. Svelte and Vue have only `vite/plugin.test.ts`. Full parity means:

- **Svelte/Vue browser setup** — add `@vitest/browser-playwright` + `playwright` devDeps (both already in the catalog), a `vitest.browser.config.ts` (framework plugin + `optimizeDeps` entries for `svelte`/`vue`), a `test:browser` script, and `src/client/csr.browser.test.ts` covering both mount and hydrate branches (Svelte `Svelte.hydrate`/`Svelte.mount`, Vue `createApp().mount`).
- **SSR unit tests** — each framework gets `src/server/ssr` tests (node env): render a component and assert HTML, assert the wrong-framework guard throws, assert `isXIsland` behaves (React `renderToString`/`renderToReadableStream`, Svelte `render`, Vue `renderToString`).
- **Registration tests** — each framework's `src/main.ts` registers its CSR and SSR services; assert `getCsrService`/`getSsrService` return them under the framework name (the register-fake pattern already exists in `phoria-islands` tests).
- **Plugin-test parity** — Vue's missing `setSsrEnvironment` externalization test (mirror Svelte's); React's untested `transform` (`__phoriaComponentPath` injection) and `applyToEnvironment`.
- **React CSR gap** — add a `mode: "hydrate"` case to `csr.browser.test.tsx` (only `"render"` is tested today).
- **Vue `csr.ts` asymmetry** — Vue's CSR service ignores `mode` (always `createApp().mount`). The parity test locks current behavior; real Vue hydration is an accepted gap, not a fix this phase.

## Signal-to-noise criteria

The definition (from `docs/PROJECT.md`, resolved 2026-08-13): a test earns its place when it catches a real behavior regression of the code it claims to cover (regression-catching), is re-evaluated against the task that introduced it so development-artifact tests are reconsidered for forward value (value-per-task), and its value outweighs its maintenance cost — flakiness, brittleness, duplication (ratio-with-size/cost). Default disposition is delete unless the test catches a real regression, otherwise rewrite. Because Phoria is pre-1.0 there is no public contract, so the criteria are reapplied at the end of every phase — this phase applies them to every test written so far.

### Dispositions for the identified low-signal candidates

| Candidate | Disposition |
|---|---|
| `PhoriaIslandPreloadTagHelperTests` `StubUrlHelper_ActionContext_ThrowsNotSupportedException` — asserts on a test stub, not production code | Delete |
| `ViteSsrManifestTests` `Indexer_FilenameOnlyKey_SupportsSecondLevelLookup` — written to pass either way | Rewrite deterministically |
| `phoria-islands/src/server/vite.test.ts` "uses the Vite module attached to the dev server" — only asserts `not.toThrow` on a fake | Rewrite to exercise the handler |
| `phoria-islands/src/server/vite.test.ts` `expect(server._vite).toBe(vite)` — internal detail | Delete |
| `ViteManifestTests` dictionary-semantics facts — near-tautological against a dictionary delegate | Delete (contract covered via manifest-reader tests) |
| `phoria-opentelemetry/src/appsettings.test.ts` — named for appsettings but only calls `createPhoriaLogger` with `observability: undefined` | Rewrite to call `getPhoriaObservabilityAppSettings` directly (also closes a coverage hole) |
| `phoria-opentelemetry/src/observability.instrumentation.test.ts` — prototype-patch identity inspection | Keep (real instrumentation contract; brittleness acceptable) |
| `phoria-react/src/client/csr.browser.test.tsx` react-refresh globals test | Keep (protects real dev-mode hydration behavior) |
| `PhoriaServerProcessTests` seam/counter tests (`beforeProcessIdAssignment` hooks, `hookCalls`) | Rewrite to assert observable spawn/stop behavior |
| `phoria-opentelemetry/src/observability.test.ts` "stable object on double-call" — order-dependent singleton re-test | Delete |
| Golden-fixture hash assertions (`ViteSsrManifestTests`, `PhoriaIslandPreloadTagHelperTests` — `Counter-D9HeyafA.js` etc.) | Relax to structural asserts (path prefix + extension) while keeping the golden JSON fixture as the ssr-manifest format contract |
| Real-time-wait asserts (`Task.Delay(50)` in `PhoriaServerMonitorTests`, fixed-delay asserts in `PhoriaServerProcessTests`) | Rewrite to the shared polling helper with timeout |
| `PhoriaIslandHtmlContentTests` misplaced DI test + handler-chain traversal loop | Rewrite/move to a DI-oriented test class, structural assertion |

Deletes are confirmed against the coverage report: no deleted test may be the sole coverer of a line that the public-contract fixes do not then cover.

## Test file layout & dedup

File-layout convention (applies to all JS packages from now on):

- **Test files are co-located with the system under test** (`.test.ts` beside `.ts`).
- **Shared or multi-consumer test utilities live in `<pkg>/tests/utilities/`** at the package root, sibling to `src/` — mirroring the .NET `TestUtilities/` folder, and isolating utilities so any future integration-style tests can land directly under `tests/` without mixing concerns.
- **One concern per file, specific names** — no generic `test-utils`/`utils` dumping ground.

Concrete dedup moves:

- `phoria-islands` — extract `tests/utilities/appsettings-fixture.ts` (the `PhoriaAppSettings` fixture duplicated across `routing.test.ts` and `vite.test.ts`) and `tests/utilities/register-fakes.ts` (the `registerSsrService`/`registerComponent`/`registerCsrService` fakes duplicated across `register.test.ts` and `server/phoria-island.test.ts`).
- `phoria-opentelemetry` — extract `tests/utilities/otel-appsettings-fixture.ts` (the `PhoriaOtelAppSettings` fixture duplicated across all four test files, with `createPhoriaOtelAppSettings(overrides)` so only the `observability` block varies).
- `.NET` `Phoria.Tests/TestUtilities/` — one file per stub family, consolidating the duplicated private nested classes: `StubServerMonitor.cs` (unified ctor over health/mode/url, covering all five copies plus the `UnhealthyServerMonitor` twin), `StubHttpClientFactory.cs` (three ctors + nested `StubHttpMessageHandler`, covering three copies), `ListLogger.cs`, `StubUrlHelper.cs` (+ `StubUrlHelperFactory`), `StubManifestReaders.cs` (`IViteManifest` + `IViteSsrManifest` variants), `TrackingSsr.cs`, `WaitUntilAsync.cs`, `TagHelperTestFactory.cs` (context/output factories), `StubHmrProxy.cs`. Delete the vestigial empty `Phoria.Tests/Health/` directory.

Config implications: `tsconfig` `include` and Biome must cover `tests/`; vitest `include` is unchanged (utilities are imported by co-located tests, not run directly); coverage `include` stays `src/**` so `tests/utilities/` is naturally excluded from reports.

## Coverage-hole fixes (public-contract bar)

Fix what consumers hit; document deep internals as accepted gaps.

### JS fixes

- `phoria-islands/src/server/routing.ts` — `/hc` health endpoint, successful SSR path incl. `x-phoria-island-*` response headers, CSR static success path, invalid-server-entry 500.
- `phoria-islands/src/client/phoria-island.ts` — custom-element behavior: props `JSON.parse`, `client:only` short-circuit, directive dispatch loop, error logging.
- `phoria-islands/src/client/directives.ts` — `idle` directive incl. the `requestIdleCallback` fallback.
- `phoria-islands/src/server/appsettings.ts` — disk-reading path: `parseAppSettings`/`up()`, env-specific file merge, `getEnvAppsettingsFileName`.
- `phoria-islands/src/vite/plugin.ts` — `setRoot`/`setBase`/`setServer` (host/port/strictPort), outDir computation for client/ssr/server envs, `serverEntry: false` skip, the `buildApp` ordering handler.
- `phoria-islands/src/server/phoria-island.ts` and `src/register.ts` — the remaining error paths (component-not-found, no-SSR-service, unregistered-framework throws).
- `phoria-opentelemetry` — enabled-signal config paths (sampler, `sdk.start()`), `request-spans` `onRequest`, direct `getPhoriaObservabilityAppSettings` (paired with the rewrite above).
- `vite-plugin-dotnet-dev-certs` — cert generation, `dotnet dev-certs` invocation, HTTPS config behavior (today the plugin test only asserts its name).

### .NET fixes

`ViteManifestReader`, `ViteSsrManifestReader`, `ViteManifestExtensions.GetRecursiveCssFiles` (recursion + cycle guard), `ViteDevServerHmrProxy` (`IsHmrRequest` + the transceive loop), `PhoriaIslandEntryScriptsTagHelper`, `PhoriaIslandPreloadHtmlContent` (the `.woff/.woff2/.png/.gif` preload branches), `PhoriaOptionsExtensions`.

### Accepted gaps (documented, not fixed)

`TextWriterBufferWriter` internals, `PhoriaIsland.Client` directive factory, direct `PhoriaServerHttpClientFactory`/`PhoriaIslandUrlHelper` unit tests (covered indirectly), singleton-state observability paths, `BackgroundService` start/stop wiring, enum/data-holder types, `server/logger.ts` warn/error delegation, Vue `csr` hydration, browser-test coverage inclusion, dev-certs end-to-end behavior (dev-only).

## Docs deliverables

- This design doc.
- `docs/MEMORY.md` — dated entry recording the decisions, the whys, and the rejected alternatives (Microsoft CodeCoverage extension → licensing; `all: false` → hides holes; exact-hash assertions → bump-brittleness; cross-package JS test sharing → independent publishes).
- `docs/ARCHITECTURE.md` — new `## Testing strategy` section covering the layers, tooling, coverage approach and interpretation, the signal-to-noise criteria + standing end-of-phase review, the `TestUtilities`/`tests/utilities` seam, the parity requirement, and the triage bar.
- `AGENTS.md` — the test file-layout convention as a durable rule in Code Style (co-location; `tests/utilities/` for shared utilities; specific names). Coverage commands were already added.

## Task order

0. Non-major dependency sweep (with the upgrade-over-mitigation check for coverage quirks).
1. Coverage tooling + CI reporting steps + baseline reports (JS v8, coverlet.MTP, `test:coverage` scripts).
2. Dedup consolidation (JS `tests/utilities/`, .NET `TestUtilities/`, delete `Health/` dir) — net-neutral refactor.
3. Signal-to-noise review — apply the dispositions, confirming no deleted test drops unique coverage.
4. Framework parity — browser setup + SSR + registration + plugin tests, React hydrate case.
5. Coverage-hole fixes (public-contract list).
6. Docs — accepted-gaps write-up, ARCHITECTURE `## Testing strategy`, MEMORY entry.

## Verification

- `pnpm build` → `pnpm lint` → `pnpm check` → `pnpm test` → `pnpm test:browser`
- `dotnet test --solution Phoria.sln --configuration Release`
- `pnpm test:coverage` and `dotnet test --solution Phoria.sln --configuration Release --coverlet --coverlet-output-format cobertura` produce reports with no new Rolldown parse noise
- `pnpm examples:check`
- `git diff --check`
