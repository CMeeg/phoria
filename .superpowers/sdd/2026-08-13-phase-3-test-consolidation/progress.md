# SDD ledger — plan: docs/plans/2026-08-13-phase-3-test-consolidation.md

Branch: feature/improve-tests-coverage (work in place, no worktree — user consent)

Baseline: `pnpm test` 7 tasks green, `dotnet test` 158 passed (both TFMs). BASE at start: 9ff773c.

## Preflight conflict scan

| Tasks | Shared surface | Finding | Ruling |
|---|---|---|---|
| T0→T1 | pnpm catalog versions; quirk verdict | T0 bumps + adds `@vitest/coverage-v8`; T1 consumes verdict (narrow exclude only if still reproducing). Consistent. | none |
| T1→T4 | svelte/vue main `vitest.config.ts` | svelte/vue main configs lack the `.browser.test.*` exclude (React/islands have it). T4 adds browser tests that `include: ["src/**/*.test.ts"]` would match and run under node. | T1 adds `exclude: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"]` to svelte + vue main configs. otel/dev-certs left untouched (no browser tests this phase). |
| T1 vs CI | `.github/workflows/ci.yml` | `build-and-test` timeout-minutes is 5; adding coverage steps likely exceeds it. | Bump `build-and-test` timeout-minutes to 15 (matches `test-browser`). No job-structure change. |
| T2→T3 | `tests/utilities/register-fakes.ts` | Utility fixes html to `<div></div>`; T3(b) asserts HTML contains `<span>Counter</span>`. Contradiction between the two plan texts. | `registerSsrComponentFramework(name = "react", html = "<div></div>")` — optional html param. |
| T3(b) | fake-runner `runner.import` dispatch | Plan: "server entry → router factory; component path → loaded module". Actual code (`routing.ts:194`) calls `runner.import(ssrEntry)` ONLY for the server entry; component modules load via the registered component `loader`. | Fake runner dispatches by `appsettings.ssrEntry` id → `{ renderPhoriaIsland: (island) => island.render() }`; component module comes from `registerComponent`'s loader. |
| T3 vs .NET seam | `PhoriaServerProcessTests` | Plan keeps seam ctor (`processId: 9999`, `stopGracePeriod`) + `beforeProcessIdAssignment` hook but asserts observable marker-file lines; `MaxRestartAttempts: 1` + guard `restartAttempts > MaxRestartAttempts` → exactly 2 launches. Retain `Assert.Same(firstStop, secondStop)`. Consistent. | none |
| T3→T5 | otel `request-spans.test.ts` | File already exists (4 tests, mock-span style). Plan says "new `request-spans.test.ts`". | T5 rewrites/extends the existing file with real H3 events per plan. |
| T2 vs spec | `Phoria.Tests/TestUtilities/` | Spec lists 9 granular files (StubServerMonitor.cs, StubHttpClientFactory.cs, ListLogger.cs, StubUrlHelper.cs, StubManifestReaders.cs, TrackingSsr.cs, WaitUntilAsync.cs, TagHelperTestFactory.cs, StubHmrProxy.cs); plan groups into 4 (ServerMonitorStubs.cs, HttpClientStubs.cs, LoggerStubs.cs, WebStubs.cs). Both satisfy the spec's scope (internal classes in `Phoria.Tests.TestUtilities`, no InternalsVisibleTo). | Follow the plan's 4-file grouping (executable decomposition); ensure all spec-listed stubs exist within them (TrackingSsr, WaitUntilAsync, StubManifestReader included). |
| T5 islands browser | islands `vitest.browser.config.ts` | islands ALREADY has browser config + `test:browser` script + `@vitest/browser-playwright`/`playwright` devDeps. | T5 only adds the new `src/client/*.browser.test.ts` files; existing config already matches `src/**/*.browser.test.ts`. |
| T5 .NET | `ViteDevServerHmrProxy.IsHmrRequest` | `IsHmrRequest` is `public static` on an `internal sealed class` — not directly testable without InternalsVisibleTo (forbidden). MEMORY's "public static IViteDevServerHmrProxy.IsHmrRequest" is inaccurate about accessibility. | Direct-test file covers `ProxyAsync` only via public `IViteDevServerHmrProxy` resolved from DI (`TryAddScoped`, ServiceCollectionExtensions.cs:46). `IsHmrRequest` true/false branches covered by the middleware HMR tests (public `UsePhoria()` seam). |
| T5 dev-certs | `getPackageName`/`getDevCertsBasePath`/export args | Verified against source: `@x/y` → `x_y`; args `["dev-certs","https","--export-path",<cert>,"--format","Pem","--no-password"]`; APPDATA → Windows, isLinux → trust dir, else macOS. Aligns. | none |

No task-pair conflict left unresolved. Baseline clean.

## Task log

Task 0: pending.

Task 0: complete (commits 9ff773c..b86001e, review clean — spec ✅, quality Approved; ⚠️ items resolved: registry versions verified live by reviewer, quirk verdict internally consistent and empirically re-verified by Task 1's test:coverage runs)

Task 1: pending.

Task 1: implementer DONE_WITH_CONCERNS (4 deviations). Controller verified each empirically before ruling:
- Ruling: accept `.gitignore` `coverage/` — biome scans generated coverage-final.json and fails lint without it (repo ignores `.turbo/`/`.vite-config/` the same way). Cost if wrong: coverage dirs stay out of git (correct regardless).
- Ruling: accept `reporter: ["text", "json"]` — empirically confirmed `["text"]` writes NO coverage dir in vitest 4.1.10 (scratch repro), so the plan's `coverage/coverage-final.json` verification + turbo `outputs: ["coverage/**"]` require json. Cost if wrong: html artifact missing locally (fine); the plan block omitted reporter entirely.
- Ruling: accept `xunit.v3` → `xunit.v3.mtp-v2` (same 3.2.2) — PLAN DEFECT correction. coverlet.MTP 10.0.1 nuspec pins Microsoft.Testing.Platform 2.2.2; xunit.v3 3.2.2 nuspec pins xunit.v3.mtp-v1 (MTP 1.9.1); mixing crashes plain dotnet test (TypeLoadException, verified by implementer's stash/revert + controller's current-state green run 158/158). xunit.v3.mtp-v2@3.2.2 exists (nuget flat-container) and depends on xunit.v3.core.mtp-v2. This is the only path to the mandated coverlet.MTP; it is a same-version package swap, not a new dependency. Cost if wrong: .NET test platform integration flavor changes (MTP v2) — CI + local both green.
- Ruling: accept CI corrections `--coverlet-file-prefix ""` + globs `**/coverage/**` and `**/TestResults/*cobertura*.xml` — confirmed cobertura files are timestamped (coverage.cobertura.140826113231902.xml) so `*.cobertura.xml` matches nothing, and JS coverage lives at packages/*/coverage/. Cost if wrong: artifact upload would silently capture nothing.

Task 1: under review (commits b86001e..e612c88).

Task 1: complete (commits b86001e..e612c88, review clean — spec ✅, quality Approved; 4 deviations adjudicated above, all accepted). Deferred minors for final review: (1) dev-certs main config lacks `.browser.test.*` exclude — covered by preflight ruling (no browser tests planned), (2) redundant `*.browser.test.*` globs in coverage exclude (plan-mandated verbatim), (3) coverage-block duplication across 6 configs (inherent to per-package design). ⚠️ actual CI run of new coverage steps is a verification gap, not a diff defect — confirm on first CI run.

Task 2: pending.

Task 2: dispatch. Pre-surveyed inventory (7 test files, duplicated nested stubs):
- StubServerMonitor 5 copies (health x2: HealthCheck:65, Middleware:178; mode x2: Entry:197, Preload:276; ctorless x1: Process:494) + SettableServerMonitor (Process:507) + UnhealthyServerMonitor twins (SsrLifecycle:314, Entry:210).
- StubHttpClientFactory 3 copies (SsrLifecycle:233 Func send, Middleware:190 ctorless, Monitor:385 HttpStatusCode) + nested StubHttpMessageHandler x3 + ScriptedHttpClientFactory (Monitor:364) + StubHmrProxy (Middleware:223).
- ListLogger x3 (SsrLifecycle:280 generic, Entry:175 generic+optional list, Monitor:346 non-generic).
- StubUrlHelper + StubUrlHelperFactory x2 (Entry:234/285, Preload:307/359).
- StubManifestReader 2 variants (Entry:229 IViteManifest, Preload:294 IViteSsrManifest).
- TrackingSsr x2 (SsrLifecycle:265, Monitor:424); WaitUntilAsync x2 (Process:448, Monitor:319, identical static signatures); CreateTagHelperContext/Output x2 (TagHelperTests + EntryTagHelperTests).
- Health dir empty; TestUtilities has only GoldenManifestFixture.cs; islands+otel have no tests/ dir yet.
Rulings: (1) plan's 4 files for plan-named stubs; TrackingSsr.cs + WaitUntilAsync.cs as separate small files (spec-listed, fit no plan family). (2) WebStubs.cs holds both manifest-reader variants (spec's StubManifestReaders consolidated). (3) StubHmrProxy → HttpClientStubs.cs. (4) ListLogger<T> generic with optional IList<string>? messages; non-generic → ListLogger<PhoriaServerMonitor>. (5) SettableServerMonitor retained in ServerMonitorStubs.cs; ctorless StubServerMonitor folds into unified StubServerMonitor (Mode/Health/SetServerStatus); UnhealthyServerMonitor twins fold into unified stub (spec: covering the twin). (6) otel refactor = the 4 files that inline appsettings (appsettings/logger/observability.instrumentation/observability); request-spans.test.ts is Task 5's, out of scope. (7) tsconfig include: append "tests/**/*.ts" to all 6 (react keeps tsx). (8) register-fakes consumers: vi.resetModules + dynamic import, with the mandated brief comment. (9) no behavior change; refactor only.

Task 2: complete (commit a6bede8 `test: consolidate shared test utilities`, review clean — spec ✅, verification green). One PLAN DEFECT correction to adjudicate:
- Ruling: accept `dts({ entryRoot: "src", exclude: ["tests/**/*"] })` in all 6 vite.config.ts — adding `tests/**/*.ts` to tsconfig include shifts unplugin-dts 1.0.3's TS6 implicit-rootDir `entryRoot` (queryPublicPath over the tsconfig include) from `src/` to the package root, emitting declarations to `dist/src/…` + `dist/tests/…` and breaking package.json `types` paths (dependent `pnpm check` fails: `@phoria/phoria/server` "Could not find a declaration file"). The dts `exclude` removes tests/ from the program and `entryRoot: "src"` restores the original layout (verified byte-shape-identical, nothing escapes dist/). Cost if wrong: package build output layout changes on next build of a package that drops the option. Also noted: WaitUntilAsync.cs hosts `AsyncTestWaits` (C# forbids member/enclosing-type name collision); Task 5 must know `request-spans.test.ts` is untouched.

Task 2: implementer DONE (commit a6bede8). Deviation adjudication:
- Ruling: accept `dts({ entryRoot: "src", exclude: ["tests/**/*"] })` in all 6 vite.config.ts — REQUIRED correction. Verified: with the fix, rebuilt islands dist layout matches original exactly (main/server/client/vite .d.ts, no dist/src/, no dist/tests/); without it, unplugin-dts 1.0.3 (TS6 implicit-rootDir) derives entryRoot from the widened tsconfig include and emits dist/src/… breaking package.json types → dependent pnpm check fails (implementer's repro during work). This is a plan-defect correction, not creep.
- Rulings on notes: WaitUntilAsync.cs → `internal static class AsyncTestWaits` (C# member-name collision; consumers `using static` — file name matches brief) ACCEPTED. StubServerMonitor two-ctor unification ACCEPTED. ListLogger<T> unified Messages+Entries ACCEPTED. StubManifestReader dual-interface ACCEPTED. TagHelperTestFactory.CreateTagHelperOutput requires tagName ACCEPTED (per-callsite). Shared StubHttpClientFactory BaseAddress localhost vs localhost:5173 ACCEPTED (handler never connects). StubHmrProxy consolidated despite single copy (plan family) ACCEPTED.

Task 2: under review (commits e612c88..a6bede8).

Task 2: complete (commit a6bede8, review clean — spec ✅, quality Approved). Deferred minors for final review: report arithmetic (net -227, 9 new files — not -567/8), ServerMonitorStubs mode-ctor hardcodes Health=Healthy (unobserved; only Mode read), dts({ entryRoot }) pin in 6 vite.config.ts merits a one-line rationale comment (AGENTS.md non-obvious-decision rule) to prevent future removal.

Task 3: pending.

Task 3: dispatch. Controller pre-read the sources and recorded bindings for the implementer brief:
- register-fakes gains optional html param: registerSsrComponentFramework(name = "react", html = "<div></div>") — dev-SSR render test asserts <span>Counter</span>. Existing call sites unchanged (default).
- runner.import is called ONLY with appsettings.ssrEntry (routing.ts:194); importComponent uses the registered loader, not runner.import — fake dispatches ssrEntry id only; plan's "(component path → loaded module)" not exercised.
- createPhoriaDevSsrRequestHandler throws SYNCHRONOUSLY when env not runnable (vite.ts:189) — assert with .toThrow.
- Task.Delay verification grep will still match legitimate helper polling (WaitForMarker Task.Delay(25)) and Process reset test — plan intent: order-dependent Monitor:49 delay gone + counters gone; report remaining matches.
- Process counter tests → bash `echo $$ >> marker` per launch; assert lines; keep Assert.Same(firstStop,secondStop) + cancellation-test PID capture.
- otel: getPhoriaObservabilityAppSettings defaults (logging:false, tracing:{enabled:false,samplingRatio:0.1}, metrics:false), partial merge, unknown keys preserved (defu keeps input extras); delete observability double-call test (observability.test.ts:17).

Task 3: implementer DONE (commit dcb2852). Deviations, all controller-verified:
- sync throw at routing.ts:189 (plan said vite.ts/reject) — correct, .toThrow.
- runner.import only ssrEntry id — matches my trace; fake throws on other ids (contract enforcement).
- [InlineData(Environments.X)] CS0182 (static readonly not const) — string literals used. Correct.
- healthy-reset waits on log signal "Phoria server process is healthy." (PhoriaServerProcess.cs:409) instead of Task.Delay(1.5s) — deterministic, in plan's spirit.
- ViteManifestTests had 6 facts not plan's 7 — stale count; .NET count reconciles 158→146 (79→73 per TFM: −6 −1 −1 +2).
- Note: report line 44 says "−7" but −6−1−1+2=−6; 73×2=146 matches the run — cosmetic typo, deferred.

Task 3: under review (commits a6bede8..dcb2852).

Task 3: complete (commit dcb2852, review clean — spec ✅, quality Approved). Deferred minors for final review: (1) the two rewritten Process tests hard-depend on bash on PATH — Windows local runs now fail where node-based tests passed (plan mandates bash $$; consider IsWindows skip guard in final triage); (2) report arithmetic typo ("−7" should be "−6"; 73×2=146 correct).

Task 4: pending.

Task 4: dispatch. Controller pre-survey + rulings:
- RULING (plan gap): react node vitest include is ["src/**/*.test.ts"] — a .tsx node test won't run. Keep plan's ssr.test.tsx filename and add "src/**/*.test.tsx" to react's node config include (exclude already drops *.browser.test.tsx; passWithNoTests=true).
- Guard messages verbatim (framework.name interpolated): react/svelte/vue all `"${name} cannot render the ${component.framework} component named \"${component.name}\"."` — e.g. `react cannot render the svelte component named "Counter".` etc.
- vue plugin.ts setSsrEnvironment external = ["@phoria/phoria-vue/server"] ONLY (no "vue" — verified plugin.ts:29); svelte's = ["@phoria/phoria-svelte/server", "svelte"] (plugin.ts:37).
- Wiring = add test:browser scripts to svelte/vue package.json only (turbo.json test:browser task already exists, dependsOn ^build — no turbo change).
- svelte browser config: @sveltejs/vite-plugin-svelte plugin + optimizeDeps include ["svelte"]; vue: @vitejs/plugin-vue + ["vue"]; both include ["src/**/*.browser.test.ts"], playwright provider chromium, tsconfigPaths.
- svelte needs src/vite-env.d.ts `declare module "*.svelte"` + src/client/Hello.svelte fixture; vue uses inline options component with h() (no .vue fixture; locks mount-always).
- devDeps @vitest/browser-playwright + playwright via catalog → pnpm install; run playwright install chromium if browsers missing.
- register.test.ts pattern: vi.resetModules() then dynamic imports of @phoria/phoria + ./client/csr + ./server/ssr then ./client/main + ./server/main; assert getCsrService(frameworkName) === csr.service and getSsrService === ssr.service. No static registry imports in the test file.
- react csr hydrate test follows the existing browser test file's react-refresh preamble pattern.

Task 4: complete (commit 18fe649 `test: add framework test parity`; report: task-4-report.md). Verification passed: `pnpm install`; `pnpm build` (7 tasks); `pnpm lint` (6 tasks); `pnpm check` (7 tasks); `pnpm test` (57 tests across 23 files); `pnpm test:browser` (9 Chromium tests across 4 files in islands, React, Svelte, and Vue); `git diff --check`. Deviations: Svelte 5 SSR adds fragment markers around the hand-rolled component, so the test asserts the required span is present rather than requiring marker-free output; node registration tests define a minimal `HTMLElement` because the shared client entry declares its custom element during import; Svelte's plugin uses the named `svelte` export in the browser config. Reviewer focus: registry identity after `vi.resetModules`, React transform filtering/path export, hydration retention, exact wrong-framework guards, and Svelte/Vue browser config isolation from node tests.

Task 4 review: initially found two important gaps: hydration tests asserted only text, and the React transform test passed a platform-native cwd path. Fixed in commit dd7396a (`test: strengthen framework parity coverage`): React/Svelte hydration now assert the original child node remains; React plugin test normalizes paths with `normalizePath`; all three registration tests restore the temporary HTMLElement global in finally blocks. Targeted framework tests passed (React 9, Svelte 5, Vue 5); `pnpm test:browser`, `pnpm lint`, `pnpm check`, and `git diff --check` passed. Review is approved; remaining minor note is the harmless Svelte config warning (`no Svelte config found`).

Task 5: implementation complete locally, pending final review. Coverage additions span islands, OpenTelemetry, dev-certs, and Phoria .NET tests. Final verification: `pnpm build`, `pnpm lint`, `pnpm check`, `pnpm test`, `pnpm test:browser`, `dotnet test --solution Phoria.sln --configuration Release`, and `git diff --check` all passed. Final counts: 79 JS tests in the regular run, 15 Chromium browser tests, and 204 .NET tests across both TFMs. Adjudicated deviations: explicit appsettings environment instead of NODE_ENV; public UsePhoria HMR seam instead of internal direct access; current explicit 80/443 URL behavior; HashSet CSS membership assertions. Report: task-5-report.md. Review follow-up added active-span capture assertions, HMR error logging/plain-WebSocket coverage, dev-cert APPDATA/non-Linux branches, and deterministic idle fallback coverage.
