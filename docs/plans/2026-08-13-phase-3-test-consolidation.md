# Phase 3: Test Consolidation & Review Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate and harden the test suites across all JS packages and the Phoria .NET package — reporting-only coverage tooling wired into CI, a dedup pass, a signal-to-noise review with deterministic rewrites, Svelte/Vue parity with React, and coverage-hole fixes at the public-contract bar.

**Spec:** `docs/superpowers/specs/2026-08-13-phase-3-test-consolidation-design.md` is the authority on scope and intent; this plan is its executable decomposition. Task order: (0) dependency sweep, (1) coverage tooling + CI, (2) dedup consolidation, (3) signal-to-noise review, (4) Svelte/Vue ↔ React parity, (5) coverage-hole fixes, (6) docs reconcile closeout.

**Tech Stack:** Vitest v4 (`v8` coverage provider via `@vitest/coverage-v8`), Vitest browser mode (Playwright provider), coverlet.MTP for xUnit v3 / Microsoft Testing Platform, pnpm workspaces, Turborepo, GitHub Actions.

## Global Constraints

- Coverage is **reporting-only** — no thresholds.
- **No `InternalsVisibleTo`** anywhere. "Public seams only": internal .NET types are covered through their public consumers.
- **No new test dependencies** beyond `@vitest/coverage-v8` (JS) and `coverlet.MTP` (.NET). No API changes.
- No cross-package JS test sharing — framework plugin tests stay parallel copies (packages publish independently).
- No new CI job — coverage steps append to the existing `build-and-test` job.
- Browser-mode tests do **not** contribute to v8 coverage (they run under `vitest.browser.config.ts` only); CSR-only modules at ~0% is the intended interpretation, not a gap to chase.
- Examples' `test:e2e` untouched; `pnpm examples:check` must stay green.
- Vue `csr` hydration is out of scope — lock the current mount-always behavior with a test.
- Repo rules that apply to every edit: Biome (tabs, no trailing commas, semicolons as-needed, width 120) — run `pnpm biome check --write <path>` to auto-fix; C# comments only when explaining a non-obvious decision; JS test files co-located (`*.test.ts` / `*.browser.test.ts`); shared test utilities live in `<pkg>/tests/utilities/` (JS) or `Phoria.Tests/TestUtilities/` (.NET) as `internal` classes.
- New JS test deps go through the pnpm catalog in `pnpm-workspace.yaml` (`catalog:`), referenced without version in package.json.
- New NuGet deps are pinned in `Directory.Packages.props` (central package management), referenced without version in the `.csproj`.
- Coverlet.MTP version: resolve latest at implementation time (`dotnet add package coverlet.MTP` in a scratch project to see what nuget resolves), then pin.
- jsdom is **not** used; islands client behavior is tested in browser mode (mirroring the framework packages).

## Sequencing decisions (reconciled into `docs/MEMORY.md`)

- The two Vite-8 coverage quirks (ignore-hint loss, uncovered-`.tsx` parse drop) are handled upgrade-over-mitigation: Task 0 sweeps non-major versions first; the narrow `exclude` for `csr.tsx`/`csr.ts` is the documented fallback only if they still reproduce.
- `test:coverage` is a turbo task (`dependsOn: ["^build"]`, `outputs: ["coverage/**"]`) aggregated by a root `pnpm test:coverage` script.
- Internal .NET types (`PhoriaOptionsExtensions`, `PhoriaServerHttpClientFactory`, `ViteDevServerHmrProxy`) are tested exclusively through public consumers (`PhoriaServerMonitor.Url`, the middleware proxy path, tag-helper dev URLs, `IViteDevServerHmrProxy`). The live HMR transceive loop and singleton-state observability paths are documented accepted gaps.
- Framework parity includes svelte/vue browser configs + a `Hello.svelte` fixture; Vue CSR keeps mount-always and locks it with a test.

---

## Task 0 — Dependency sweep (upgrade-over-mitigation)

**Goal:** Rule out fixes in newer tool versions before building mitigation around the Vite-8 coverage quirks.

**Steps:**

- [ ] Bump `pnpm-workspace.yaml` catalog: `vitest` ^4.1.10 → latest 4.x, `@vitest/browser-playwright` → latest matching 4.x, `playwright` → latest 1.x, `vite` → latest 8.x. Add `@vitest/coverage-v8` (pin = vitest version).
- [ ] `pnpm install` and `pnpm build`.
- [ ] Reproduce both quirks on this branch with a temporary `coverage: { provider: "v8", all: true, include: ["src/**/*.{ts,tsx}"] }` block in `packages/phoria-react/vitest.config.ts`:
  - Q1: Rolldown parse noise / duplicate-file reporting for `.tsx` under `all: true`.
  - Q2: `.browser.test.tsx` files matched by `*.{ts,tsx}` counted as uncovered source themselves.
- [ ] Record the verdict in `docs/MEMORY.md` (fixed by the sweep, or still reproducing).
- [ ] If still reproducing, Task 1 uses the documented narrow `exclude` for `src/client/csr.tsx` / `src/client/csr.ts` only — nothing wider.

**Verification:** `pnpm build && pnpm test` green on bumped versions; quirk verdict recorded.

---

## Task 1 — Coverage tooling + CI

**Goal:** Wire coverage across all 6 JS packages + the .NET suite; reporting-only, no thresholds.

**Steps:**

- [ ] Root `package.json`: add `"test:coverage": "turbo run test:coverage"`. `turbo.json`: add `"test:coverage": { "dependsOn": ["^build"], "outputs": ["coverage/**"] }`.
- [ ] Each of the 6 JS package.json: add `"test:coverage": "vitest run --coverage"` and `@vitest/coverage-v8` devDep.
- [ ] Add the standard coverage block to each package's `vitest.config.ts` (NOT the browser config):
  ```ts
  coverage: {
      provider: "v8",
      all: true,
      include: ["src/**/*.{ts,tsx}"],
      exclude: [
          "**/*.test.ts", "**/*.test.tsx", "**/*.browser.test.ts", "**/*.browser.test.tsx",
          "src/main.ts", "src/client/main.ts", "src/server/main.ts"
      ]
  }
  ```
  Trivial re-export entry files excluded per package: islands + react/svelte/vue → `src/main.ts`, `src/client/main.ts`, `src/server/main.ts`; otel → `src/main.ts`; dev-certs → none. If Task 0 still reproduces a quirk, add the documented narrow `exclude` for `src/client/csr.tsx`/`csr.ts`.
- [ ] Confirm each package's main config `include` already excludes `*.browser.test.*` so node runs don't pick them up (React's setup is the reference).
- [ ] .NET: add `coverlet.MTP` to `Directory.Packages.props` + `packages/Phoria.Tests/Phoria.Tests.csproj` (`<PackageReference Include="coverlet.MTP" />`).
- [ ] `.github/workflows/ci.yml` — append to the `build-and-test` job (no new job): `pnpm test:coverage` → `dotnet test --solution Phoria.sln --configuration Release --coverlet --coverlet-output-format cobertura --coverlet-file-prefix` → `actions/upload-artifact@v5` for `coverage/**` + `**/TestResults/*.cobertura.xml`.

**Verification:** both coverage runs complete, artifact upload works, per-package `coverage/coverage-final.json` + .NET cobertura files exist.

---

## Task 2 — Dedup consolidation

**Goal:** One home per shared test asset; no behavior change.

**Steps:**

- [ ] **islands** `tests/utilities/appsettings-fixture.ts`: `createPhoriaAppSettings(overrides: Partial<PhoriaAppSettings> = {})` returning the full shape (root `"ui"`, base `"/ui"`, entry, ssrBase `"/ssr"`, ssrEntry, `server: { host, https }`, `build: { outDir }`, spread overrides), importing the type from `../../src/server/appsettings`. Replace the inline fixtures in `src/server/routing.test.ts`, `src/server/phoria-island.test.ts`, `src/server/vite.test.ts`.
- [ ] **islands** `tests/utilities/register-fakes.ts`:
  ```ts
  import { registerComponent, registerCsrService, registerSsrService } from "../../src/register"

  export function registerSsrComponentFramework(name = "react") {
      registerSsrService(name, { render: async () => ({ framework: name, html: "<div></div>" }) })
      registerComponent("Counter", { framework: name, loader: async () => ({ default: {} }) })
  }

  export function registerCsrFramework(name = "test") {
      registerCsrService(name, { mount: async () => {} })
  }
  ```
  **Critical constraint (add a brief comment explaining it):** consumers must `vi.resetModules()` then *dynamically* `import("../../tests/utilities/register-fakes")` alongside their own dynamic import of the module under test — a static top-level import would capture a stale module registry across resets and break the shared registry. Apply in `src/register.test.ts` + `src/server/phoria-island.test.ts`.
- [ ] **otel** `tests/utilities/otel-appsettings-fixture.ts`: `createPhoriaOtelAppSettings(overrides)`; refactor the 4 otel test files onto it.
- [ ] Add `"tests/**/*.ts"` to each JS package's tsconfig `include`.
- [ ] **.NET** — extract the duplicated nested stubs into `Phoria.Tests/TestUtilities/` as `internal` classes (test-project classes; no `InternalsVisibleTo`):
  - `ServerMonitorStubs.cs`: `StubServerMonitor` (Mode/Health/SetServerStatus), `SettableServerMonitor`.
  - `HttpClientStubs.cs`: `StubHttpClientFactory`/`StubHttpMessageHandler`, `StubHmrProxy`, `ScriptedHttpClientFactory` (tracking `RequestCount`).
  - `LoggerStubs.cs`: `ListLogger<T>`.
  - `WebStubs.cs`: `StubUrlHelper`, `StubUrlHelperFactory`, `StubManifestReader`, `TagHelperTestFactory` (the `CreateTagHelperContext`/`CreateTagHelperOutput` helpers).
  - Update all referencing test files. Delete the empty `packages/Phoria.Tests/Health/` directory.

**Verification:** `pnpm test` + `dotnet test --solution Phoria.sln --configuration Release` green; no behavior change.

---

## Task 3 — Signal-to-noise review (13 dispositions)

**Goal:** Replace order-dependent / tautological / exact-hash asserts with deterministic ones.

**Steps:**

- [ ] Delete `packages/Phoria.Tests/Vite/ViteManifestTests.cs` (7 dictionary-semantics facts; contract covered by new `ViteManifestReaderTests` in Task 5). Remove any now-unused usings/imports in remaining files.
- [ ] `ViteSsrManifestTests`: rename `Indexer_FilenameOnlyKey_SupportsSecondLevelLookup` → `Indexer_AssetFilenameKey_ReturnsNull`, `Assert.Null(manifest["Counter-D9HeyafA.js"])` (golden manifest has no filename-only keys). Relax the `?commonjs-exports` exact-hash assert → structural: `Assert.StartsWith("/ui/assets/client-", file)` + `Assert.EndsWith(".js", file)`.
- [ ] `PhoriaIslandPreloadTagHelperTests`: delete `StubUrlHelper_ActionContext_ThrowsNotSupportedException`; relax golden-hash asserts → structural, e.g. `Assert.Matches("rel=\"modulepreload\"[^>]*href=\"/ui/assets/Counter-[A-Za-z0-9_-]+\\.js\"", output.Content.GetContent())`.
- [ ] `PhoriaIslandHtmlContentTests`: keep `WriteTo_RendersContentAndDisposesSsrStreams`; move `AddPhoria_UsesDangerousCertificateValidationOnlyInDevelopment` (+ `GetPrimaryHandler` handler-chain traversal + `TestHostEnvironment`) into `PhoriaServiceCollectionTests` and keep it as a `[Theory]` over development/production.
- [ ] Rewrite `packages/phoria-islands/src/server/vite.test.ts`:
  - Keep the `createPhoriaViteDevServer` config assert (`appType: "custom"`, `server: { middlewareMode: true }`); delete `expect(server._vite).toBe(vite)`.
  - Replace the `not.toThrow` test with: (a) `createPhoriaDevSsrRequestHandler(fakeDevServerWithoutRunnableSsr, settings)` rejects with `Vite dev server does not have a runnable SSR environment.`; (b) a real dev-SSR render through a fake server whose `_vite.environments.ssr.runner.import` dispatches by id (server entry → router factory; component path → loaded module) using `registerSsrComponentFramework()` — POST `/ssr/render/Counter` returns 200, HTML contains `<span>Counter</span>`, `x-phoria-island-framework` header set. Trace the exact `runner.import` ids from `src/server/routing.ts` when writing.
- [ ] Rewrite `packages/phoria-opentelemetry/src/appsettings.test.ts`: replace the `createPhoriaLogger` observability test with direct `getPhoriaObservabilityAppSettings` tests — defaults (`logging: false`, `tracing: { enabled: false, samplingRatio: 0.1 }`, `metrics: false`), partial merge over defaults, unknown keys preserved.
- [ ] Delete the otel observability "stable object on double-call" test (module-singleton dependence).
- [ ] `PhoriaServerMonitorTests`: replace `Task.Delay(50)` with `WaitUntilAsync(() => factory.RequestCount >= 1)`; switch to `ScriptedHttpClientFactory`.
- [ ] `PhoriaServerProcessTests`: rewrite the `beforeProcessIdAssignment` counter asserts to observable PID-marker-file asserts — a bash script appends `$$` to a temp marker file per launch; with `maxRestartAttempts: 1` (public seam ctor: `processId: 9999`, `stopGracePeriod`) assert exactly 2 lines across start/stop/start; retain `Assert.Same(firstStop, secondStop)`; keep the PID-capture use in the cancellation test.

**Verification:** both suites green; `rg -n "Task\.Delay|beforeProcessIdAssignment|_vite\)" packages/` clean.

---

## Task 4 — Svelte/Vue ↔ React parity

**Goal:** The framework packages get the same test breadth React has.

**Steps:**

- [ ] **React gap-closing** (`phoria-react`):
  - `src/client/csr.browser.test.tsx`: add the hydrate case — island prefilled with `<span>Hello World</span>`, `mount(..., { mode: "hydrate" })`, assert content retained.
  - `src/vite/plugin.test.ts`: add `transform` tests — asserts `export const __phoriaComponentPath = "/src/Hello.tsx";` is injected for a `.tsx` id under `process.cwd()`, and `undefined` for a `node_modules/**` id (the negative case uses the `filter` exclude — `cwdRegex` only strips path, it doesn't gate); add an `applyToEnvironment` test (`client`/`ssr` → true, `server` → false).
  - `src/server/ssr.test.tsx` (new): `renderComponentToString` with an inline `createElement` component → HTML contains `Hello React`; `service.render` with a `framework: "svelte"` island rejects with the exact guard message (copy verbatim from `ssr.tsx`); `isReactIsland` true/false.
  - `src/register.test.ts` (new): `vi.resetModules()` + dynamic imports; assert `getCsrService("react") === (await import("./client/csr")).service` and `getSsrService("react") === (await import("./server/ssr")).service` after importing `./client/main` + `./server/main`.
- [ ] **Svelte** (`phoria-svelte`): add `vitest.browser.config.ts` (mirror React's; `@sveltejs/vite-plugin-svelte` plugin, optimizeDeps `svelte`), `test:browser` script, devDeps `@vitest/browser-playwright` + `playwright`; `src/client/Hello.svelte` fixture + `*.svelte` module declaration in `src/vite-env.d.ts`; `src/client/csr.browser.test.ts` (mount via `Svelte.mount`, hydrate via `Svelte.hydrate`, both asserting textContent); `src/server/ssr.test.ts` (hand-rolled SSR-able component exposing a `.render` method → `renderComponentToString` returns `<span>Hello World</span>`; `service.render` with the `options.renderComponent` override; wrong-framework guard; `isSvelteIsland`); `src/register.test.ts`.
- [ ] **Vue** (`phoria-vue`): same config/script/devDeps additions; `src/client/csr.browser.test.ts` (mount via plain options component with `h()`, asserting textContent — locks the current mount-always behavior); `src/server/ssr.test.ts` (options component + `renderToString` HTML, `options.renderComponent` override, wrong-framework guard, `isVueIsland`); `src/register.test.ts`; add the missing `setSsrEnvironment` test to `src/vite/plugin.test.ts` (mirror Svelte's — external `["@phoria/phoria-vue/server"]`, `import type { EnvironmentOptions } from "vite"`).
- [ ] Wire svelte/vue `test:browser` into whatever runs React's (inspect root `package.json`/`turbo.json` `test:browser` wiring and mirror it).

**Verification:** `pnpm test` + `pnpm test:browser` green across the three framework packages; `pnpm check` clean (new `.tsx`/`.svelte` types).

---

## Task 5 — Coverage-hole fixes

**Goal:** Cover the biggest gaps in `docs/ARCHITECTURE.md` Table 4, JS + .NET, at the public-contract bar.

### JS

- [ ] **islands `server/appsettings.ts`** — add disk-read tests to `appsettings.test.ts`: temp dir with `appsettings.json` + `appsettings.Development.json` → `parsePhoriaAppSettings({ cwd, environment: "Development" })` merges env-specific over base (covers `parseAppSettings` → `up()` + `getEnvAppsettingsFileName`); `getPhoriaAppSettings` environment branch via `vi.stubEnv("NODE_ENV", "Development")`.
- [ ] **islands `server/routing.ts`** — via the fake-runner dev handler: `/hc` returns 200 with `mode` + `frameworks` (trace the exact shape from `routing.ts`); a server entry that isn't a function → 500; CSR static success via a temp-dir file served with a JS mime type (verify `createPhoriaCsrRequestHandler` is exported; else exercise the dev handler's static route).
- [ ] **islands `register.ts` + `server/phoria-island.ts` error paths** — add to existing tests: `getSsrService`/`getCsrService` for an unregistered framework throw the exact source messages (copy verbatim); `PhoriaIsland.create` with an unknown component throws `Component "…" not found in registry.`-style message and with no SSR service throws.
- [ ] **islands `client/phoria-island.ts` + `client/directives.ts`** — add islands `vitest.browser.config.ts` + `test:browser` script + `@vitest/browser-playwright`/`playwright` devDeps (mirror React); new `src/client/phoria-island.browser.test.ts`: missing `component` attribute throws, unknown component throws, no CSR service throws, `client:only` mounts without SSR, `client:load` mounts on connect, `client:idle` falls back to `onload` after timeout (`vi.useFakeTimers` + `advanceTimersByTime`).
- [ ] **otel `observability.ts`** — tracing-enabled path: `vi.resetModules()` then dynamic import (fresh module singleton), `createPhoriaObservability(settings with tracing.enabled: true)`, assert `trace.getTracerProvider()` is no longer `NoOpTracerProvider`, then `await observability.shutdown()` (no network I/O at start/shutdown — the exporter only dials on span export). If this proves flaky, fold the sampler branch into the accepted gaps. **`request-spans.ts`** — new `request-spans.test.ts`: real H3 events via `createApp()` + `toWebHandler` inside `context.with(trace.setSpan(context.active(), mockSpan))`, calling `onRequest` → set `x-phoria-island-framework` header → `onBeforeResponse`; assert span `updateName`/`setAttribute` calls for the SSR-render branch, a CSR-asset branch, and the no-active-span early return.
- [ ] **dev-certs `plugin.ts`** — new tests (node env): config no-op outside development mode; dev + existing temp cert files + `basePath` → `config.server.https = { cert, key }` with no dotnet invocation and user server config preserved; missing certs → `vi.mock("tinyexec")` `x` invoked with export args, exitCode 0 returns paths, non-zero throws; missing basePath throws; `getPackageName` (temp `package.json`, `@x/y` → `x_y`, missing file/name throws); `getDevCertsBasePath` branches via `vi.stubEnv("APPDATA")` (Windows) and `vi.mock("std-env", () => ({ isLinux: true/false }))` (Linux trust dir / macOS dir).

### .NET (all listed types are public — direct tests)

- [ ] **`ViteManifestReaderTests`** (new): write a real temp `manifest.json`, `ReadManifest` → assert chunk dict incl. `Css` + `Imports`; file-not-found path behavior per source.
- [ ] **`ViteSsrManifestReaderTests`** (new): temp `ssr-manifest.json` in the golden shape (component-path keys) → assert mapping.
- [ ] **`ViteManifestExtensionsTests.GetRecursiveCssFiles`** (new): CSS propagated from imported chunks (assert source ordering — `.Reverse()` happens at the call site in `PhoriaIslandEntryTagHelper.cs:226`); cyclic imports terminate via the `proccessedChunks` set.
- [ ] **`PhoriaIslandEntryScriptsTagHelperTests`** (new, reuse Task 2 stubs): `Process` sets `TagName = "script"`, `type = "module"`, `PhoriaSrc = options.Entry`; dev mode renders the `@vite/client` pre-element + an entry `src` ending `/src/entry-client.ts` (relax the URL prefix); prod with manifest → module script from the manifest file URL; prod key-not-found → log + suppress output.
- [ ] **`PhoriaIslandPreloadHtmlContentTests`** (new): direct unit tests — `.js`/`.css`/`.woff`/`.woff2`/`.gif`/`.jpg`/`.jpeg`/`.png` → preload link containing the href; other extensions → empty (adapt to the exact ctor/extension API in `PhoriaIslandPreloadHtmlContent.cs`).
- [ ] **`PhoriaServerMiddlewareTests` HMR additions** (public `UsePhoria()` seam + `DefaultHttpContext` + fake `IHttpWebSocketFeature` in TestUtilities): HMR request (`IsWebSocketRequest` + `WebSocketRequestedProtocols` contains `vite-hmr`) → routed to `StubHmrProxy`, next middleware not called; non-HMR (plain ws / wrong protocol) → falls through, proxy untouched.
- [ ] **`IViteDevServerHmrProxy` direct tests** (new file): `IsHmrRequest(HttpContext)` static — true only for ws + `vite-hmr` protocol; `ProxyAsync` connect-failure — resolve the scoped proxy from a `ServiceProvider` built with `AddPhoria()` + `PhoriaOptions { ServerUrl = "http://localhost:1" }` (unreachable), `DefaultHttpContext`, assert it completes without throwing and `ListLogger` records the error.
- [ ] **`PhoriaServerMonitor.Url`** (public class — covers internal `PhoriaOptionsExtensions` branches): add http/https × port/no-port (443/80 → empty port) cases via the existing monitor test fixture.

**Accepted gaps (document in MEMORY + coverage table, no action):** live HMR transceive loop (vendor Quetzal Rivera code), singleton-state observability paths, CSR-only modules (~0% in v8), internal `PhoriaServerHttpClientFactory.BaseAddress` branch (covered indirectly by the middleware proxy tests).

**Verification:** both suites green; spot-check the coverage table lines the phase calls out.

---

## Task 6 — Docs reconcile (closeout)

**Steps:**

- [ ] Update `docs/ARCHITECTURE.md` Table 4 (coverage) with the new per-file numbers/notes from Task 5 output + the accepted-gaps and browser-coverage flags.
- [ ] Update `AGENTS.md` commands: `pnpm test:coverage`, the .NET coverlet run, `test:browser` across the framework packages.
- [ ] Update `docs/MEMORY.md`: Task 0 quirk verdict, Task 1 tooling decisions, the sequencing decisions above.
- [ ] `pnpm examples:check` green (examples untouched).

**Verification:** docs consistent with the code; `git diff --check` clean.

---

## Out of Scope

- Examples' `test:e2e`; new test deps beyond `@vitest/coverage-v8` + `coverlet.MTP`; any API changes; coverage thresholds; Vue real hydration.

## Risks / Notes

- Fake-runner dev-handler tests require tracing the exact `runner.import` ids from `src/server/routing.ts`.
- The otel tracing-enabled test relies on `vi.resetModules()` for the module singleton — flagged with a fallback to accepted gaps.
- `tests/utilities/register-fakes.ts` must be dynamically imported (module-registry-instance hazard) — the comment in the file must say why.

## Definition of Done

- All tasks complete with the per-task verification commands green: `pnpm build` → `pnpm lint` → `pnpm check` → `pnpm test` (→ `pnpm test:browser` for framework packages), and `dotnet test --solution Phoria.sln --configuration Release` wherever .NET changed.
- Task 1+ also verified via `pnpm test:coverage`, the coverlet run, `pnpm examples:check`, `git diff --check`.
- Coverage artifacts upload in CI; MEMORY/ARCHITECTURE/AGENTS reconciled.
