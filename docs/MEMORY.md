# Memory

Dated log of durable decisions made while shaping the project. Later entries supersede earlier exploratory choices where noted; implementation details belong in the architecture and milestone docs.

## 2026-07-26 — v1 milestone scoping

- v1 = "stability + a few key features", not a full feature-complete vision — because the priority is a release the author can confidently talk about, not shipping every idea.
- Tests are Phase 0 (first) — for long-term health, to de-risk dep updates, and to re-familiarize with the codebase.
- Test stack chosen: Vitest (JS), xUnit (.NET), Playwright (e2e) — mainstream, well-supported fits for each layer.
- Dependency/platform updates (Phase 1) reordered *before* server robustness (Phase 2) — because new deps/.NET 10 APIs may provide cleaner primitives for the robustness fixes, avoiding double work.
- Target .NET 8/10, not 10-only — .NET 8 remains the supported LTS consumer target while .NET 9 was dropped as STS with no distinct support window.
- Platform stance: current Vite 8, latest React/Svelte/Vue, and .NET 10; the .NET 10 memory-pool API remains deferred because it does not match the existing `Phoria.IO` stream and buffer-writer contract.
- "Vite bundling of .NET-referenced static assets" committed as the one new v1 feature; goal = route the whole app's assets (CSS/images/JS from Razor/MVC views, not just islands) through Vite.
- Big feature ideas (nested composition, streaming/Suspense, server actions, Deno adapters) handled as timeboxed spikes with go/no-go gates — keeps v1 shippable while still exploring value.
- Web components library deferred to post-v1 — not worth v1 scope.
- Versioning: independent per-package versions, but all packages must reach 1.0.0 (reconciles current drift: 0.4.2 / 0.3.2 / 0.2.1).
- API stance: best-effort stable, not a formal freeze/audit — accept minor-bump evolution post-1.0.
- Riskiest unknown named as Vite-bundling-of-.NET-assets (potential fundamental design changes), ahead of the server shutdown bug — gated behind a design spike.

## 2026-07-26 — Phase 0 test foundation

- Chose Vitest (unit + browser mode via Playwright provider) for JS, xUnit with built-in `Assert` for .NET — one runner per ecosystem, browser mode avoids a separate Playwright toolchain for component-focused tests.
- Deliberately avoided FluentAssertions: v8+ is commercially licensed, incompatible with an MIT OSS project; xUnit's built-in asserts are sufficient.
- Co-located JS tests (`*.test.ts` / `*.browser.test.ts`); `Phoria.Tests` mirrors source namespaces with `Method_Scenario_ExpectedBehaviour` naming.
- Added a minimal full-stack smoke test (build + preview framework-multiple, assert rendered island) to cover the .NET↔vite SSR path not exercised by browser-mode component tests.
- Browser + smoke tests run in a separate CI job (need browsers + dotnet runtime); no enforced coverage threshold this phase.

## 2026-07-27 — Phase 1 planning (dependency & platform updates)

- Targeted **Vite 8**, not the Vite 7 named in PROJECT.md — 8 is current, and `@vitejs/plugin-react@6` / `@sveltejs/vite-plugin-svelte@7` both *require* it, so stopping at 7 would ship 1.0.0 on already-superseded plugin majors. Accepted that Vite 8 means Rolldown/Oxc, i.e. the riskiest possible bundler change.
- **Task 1 closes the ssr-manifest → `__phoriaComponentPath` → preload tag helper test gap before any dependency moves.** It is the only completely untested seam in the repo and is exactly what Rolldown's module-ID changes (and the disappearance of `?commonjs-*` suffixes) would break silently.
- Sequenced every bundler-*independent* upgrade (Node/pnpm → Biome/Lerna/Changesets → TypeScript → Vitest → .NET) ahead of Vite 8, so a red run has one candidate cause. Vitest 4 specifically lands on Vite 6 because it declares `vite: ^6 || ^7 || ^8`.
- Chose **TypeScript 6, not 7**. TS 7 (the native Go port) ships no programmatic API, so `unplugin-dts` cannot emit `.d.ts` and Volar cannot serve Vue/Svelte — Microsoft says so explicitly. Revisit when TS 7.1 lands its new API.
- Stayed on **h3 1.15.x**. v2 is still an RC (`2.0.1-rc.26`, docs self-describe as beta) and h3 is a *runtime* dependency of the published package; 1.0.0 will not depend on an RC. Instead, decouple h3 from the public API now (`PhoriaRequestHandler`, `PhoriaIslandRequest`) so a later v2 swap is a non-breaking minor rather than a consumer break. Side benefit: `PhoriaIsland.create` becomes unit-testable without mocking h3.
- **Dropped net9.0**, against PROJECT.md's "keep all three". net9 is STS and reaches EOL the same day as net8 (2026-11-10), so it cost a TFM leg and conditioned `PackageVersion` entries for zero extra coverage. Kept net8.0 — it is the LTS consumers are actually on.
- Migrating to **xunit.v3 on Microsoft.Testing.Platform** (via `global.json` `test.runner`) as part of the .NET 10 move: xunit v2 + Test SDK 18 + a new TFM is precisely where VSTest/MTP mismatches bite, and the .NET 10 SDK makes MTP the native `dotnet test` path.
- Pinning `LangVersion` to `13.0` instead of `latest` — `latest` is not per-TFM, so on the .NET 10 SDK C# 15 features would be offered to the `net8.0` compilation too.
- **Node 24 LTS** (not 26): 22.11 was below both the Vite 7+ floor (22.12) and the pnpm 11 floor (22.13); 24 is Active LTS, 26 does not become LTS until 2026-10-28.
- Removing the `package.json` Biome-formatting exclusion is **conditional on empirical proof**, not assumption: Biome 2 defaults `package.json` to `expand: "always"`, which should match what Changesets writes, but the plan verifies it by running `changeset version` then `biome check` and reverts if not clean.
- Adopting `builder.buildApp` and deleting both `vite.server.config.ts` files — the three build passes currently run *in parallel with no ordering guarantee*, even though the `client` environment emits the `ssr-manifest.json` that later consumers depend on. Working today by luck.
- Adding `applyToEnvironment` to the three framework plugins is mandatory once a `server` environment exists, otherwise the Phoria Server bundle gets rewritten with `__phoriaComponentPath`.
- **`IMemoryPoolFactory` / .NET 10 memory pools moved out of Phase 1 into Phase 2.** `StreamPool` exposes `RecyclableMemoryStream` publicly and consumers rely on `GetReadOnlySequence()`, the `IBufferWriter<byte>` cast, and `Stream` semantics — none of which `MemoryPool<byte>` provides. It is a public-API refactor, not a dependency bump.
- Also deferred to Phase 2 to keep this phase a pure upgrade: `Process.Kill()` missing `entireProcessTree` (the likely root cause of the known shutdown bug), the `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, and the unconditional `DangerousAcceptAnyServerCertificateValidator`.
- Left open: `@meeg/vite-plugin-inspect-config` (author-owned) caps its `vite` peer at `^6` and needs a `0.3.0` release; unblocked meanwhile via `pnpm-workspace.yaml` `peerDependencyRules`. Kept rather than removed because it produces the resolved-config snapshots used as the Vite 6→8 diff baseline.

## 2026-07-27 — Phase 1 execution findings

- **Biome `package.json` exclusion removal confirmed empirically (Task 3).** Ran `biome check --write .`, then simulated a Changesets version bump (`changeset version`) and re-ran `biome check` against the rewritten `package.json` files — clean, no formatting diagnostics. The exclusion in `AGENTS.md`/`biome.jsonc` was removed for real, not just planned.
- **Rolldown DID drop the `?commonjs-exports` suffix from ssr-manifest keys (Task 7), exactly as the plan's hazard #1 predicted — but it doesn't matter to this consumer.** Building the real e2e app on Vite 8/Rolldown confirms the live `ssr-manifest.json` has zero `?commonjs-exports` keys, versus several present in the Task 1 golden fixture (`packages/Phoria.Tests/TestData/ssr-manifest.json`). The regression net still passes only because `PhoriaIslandPreloadTagHelper`/`ViteSsrManifest` never look up manifest entries by those raw CJS module-ID keys — they resolve by component source path, then by `Path.GetFileName`, so the dropped suffix is a change in Rolldown's output that this codebase never queried in the first place. Task 1's preload assertion passed unmodified for that reason, not because the key contract was preserved; no `ssr-manifest.rolldown.json` fallback or golden-file update was needed.
- **TypeScript 6 silently required `paths` values to be relative (Task 4).** The plan assumed `tsconfig.json` `paths` entries were unaffected by dropping `baseUrl`, but TS 6 requires `./`-prefixed relative paths once `baseUrl` is absent — all 9 tsconfig files needed `"src/*"` → `"./src/*"`. This also exposed that `phoria-react`, `phoria-svelte`, and `phoria-vue` were missing an explicit `@types/node` devDependency, previously masked by `baseUrl` auto-resolution.
- **Most catalog version bumps had already landed by Task 7, ahead of Task 11 (the dedicated dependency-sweep task).** Task 7's Vite 8 catalog rewrite incidentally brought react/react-dom, svelte, vue, magic-string, empathic, cross-env, postcss-preset-env, destr, and several other transitive deps to their Task-11 target versions as part of getting Vite 8 to build cleanly. Task 11 correctly scoped itself down to only the remaining direct (non-catalog) deps: `defu`, `mime`, `tinyexec`, `std-env`, and `CliWrap`.
- **`net9.0` dropped from the TFM list (Task 6), against `PROJECT.md`'s original "keep all three."** net9 is STS and reaches EOL the same day as net8 (2026-11-10), so carrying it cost a TFM leg and conditioned `PackageVersion` entries for zero additional coverage. `PROJECT.md` is amended in Task 12 to reflect `net8.0;net10.0`.
- **`.NET 10` memory pools stayed out of Phase 1 as planned, and `PROJECT.md`'s Phase 1 goals bullet is corrected in Task 12** to stop implying they shipped this phase — they move to Phase 2 alongside the other `Phoria.IO`/server-robustness fixes (`Process.Kill()` process-tree bug, `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, unconditional `DangerousAcceptAnyServerCertificateValidator`).
- **CI required more than an action-version bump.** The .NET 10 SDK ships only the .NET 10 runtime, but `Phoria.Tests` still targets `net8.0`, so every `setup-dotnet` step in `ci.yml` now also installs `8.0.x` explicitly. `global.json`'s `test.runner: Microsoft.Testing.Platform` also changed the `dotnet test` invocation shape: `dotnet test Phoria.sln` (VSTest-style) must become `dotnet test --solution Phoria.sln` in MTP mode, or the solution path is silently misinterpreted.
- No task's verification run (`build` → `lint` → `check` → `test` → `dotnet test` → browser tests → e2e smoke) needed more than a same-task fix round to go clean — the two fix rounds that did occur (Task 3's Svelte/Vue Counter variable-name regression from a lint autofix, Task 7's overly-strict `@sveltejs/vite-plugin-svelte` peer range) were code-review findings, not CI failures. That is a stronger-than-expected result for a twelve-task, bundler-major-version phase.

## 2026-08-01 — Phase 1.5 execution findings

- **The Phase 1.5 plan's `pnpm --filter phoria-islands` verify commands were wrong.** `phoria-islands` is the directory name; the package is `@phoria/phoria`, so the filter matches no project and silently no-ops. Use `pnpm --filter @phoria/phoria` (all Tasks' verify commands were run with the correct filter; logged here as the canonical correction).
- **MTP-mode focused test runs need `--filter-method <fqn>`, not VSTest's `--filter`.** `dotnet test --solution Phoria.sln --configuration Release` runs under Microsoft.Testing.Platform (per `global.json`); to run a single test use `--filter-method` with the fully-qualified method name (e.g. `Phoria.Tests.Server.ViteSsrManifestTests.Handles_Missing_Manifest`). The plan's `--filter` syntax fails under MTP.
- **Task 7's "verify over HTTP" step cannot succeed as written, and no code change is needed — the premise was wrong.** (1) `app.UseHttpsRedirection()` (`Program.cs:34`) redirects `http://localhost:5247` → `https://localhost:7147`, so the browser is never served over plain HTTP. (2) Setting `phoria.server.https: false` does not produce an HTTP dev stack: `@phoria/vite-plugin-dotnet-dev-certs` unconditionally enables HTTPS on the Vite dev server in development (`packages/vite-plugin-dotnet-dev-certs/src/plugin.ts:126-155` never consults `phoria.server.https`), while `phoria.server.https` only drives the scheme the .NET side uses to reach Vite (`PhoriaOptionsExtensions.cs:10`) — so flipping it just makes the `PhoriaServerMonitor` health check fail against `http://localhost:5173`. Verified empirically; the HTTPS path (Step 1) plus the HTTP→HTTPS redirect path both render islands and Fast-Refresh cleanly, so the task's intent is covered.
- **`dotnet dev-certs https --trust` hangs on a sudo prompt in this environment** (no passwordless sudo). Non-interactive workaround that still yields a browser-verifiable cert: export the per-user OpenSSL trust via `SUDO_ASKPASS=/tmp/.../askpass.sh timeout 30 dotnet dev-certs https --trust` — `dotnet` imports the cert into `~/.aspnet/dev-certs/trust` and sets `SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust"` for curl, so the HTTPS dev server is trusted per-user without system trust.
- **Task 8's `node:22-slim` → `node:24-slim` pin uncovered a pre-existing, unrelated Docker build failure**: `pnpm --filter=<app> deploy --prod /app` fails with `ERR_PNPM_DEPLOY_NONINJECTED_WORKSPACE` on pnpm ≥10 unless the workspace opts into `injectWorkspacePackages: true` (or the deploy command passes `--legacy`) — broken since `c716413` pinned pnpm 11, `node:22-slim` would have failed identically. Chose `injectWorkspacePackages: true` over `--legacy`: pnpm's own documented Docker recipe uses the non-legacy (injected) path because it produces a self-contained, non-symlinked deploy directory — required once the deploy dir is copied into an isolated Docker stage, where symlinks back into the monorepo source tree wouldn't resolve. It's a workspace-wide setting, not deploy-scoped, but `dedupeInjectedDeps` (default `true`) keeps local workspace linking symlinked whenever peer deps match across consumers (true here, thanks to catalog-pinned versions), confirmed empirically (`node_modules/@phoria/*` still symlinks after `pnpm install`, full `pnpm build`/`lint`/`check`/`test` unaffected). Watch item if this ever regresses: injected (hard-linked) deps are frozen at install time and don't auto-update on rebuild — would need `syncInjectedDepsAfterScripts` configured with the relevant build script names, or a `pnpm install` re-run.
- **`@meeg/vite-plugin-inspect-config@0.3.0` was published after the Phase 1.5 pause.** Its Vite peer range now includes 6/7/8, so the temporary `pnpm-workspace.yaml` `peerDependencyRules.allowedVersions` exception was removed and the catalog/lockfile advanced from `~0.2.0` to `~0.3.0`. `pnpm --filter framework-multiple check` and `build` pass with no peer warnings; existing Svelte and CA1873 warnings are unrelated. The active 24-hour pnpm supply-chain policy rejected the fresh package during clean Docker `--frozen-lockfile` installs, so the intentionally approved package is listed under `minimumReleaseAgeExclude`; both no-cache Docker builds pass with that narrow exception.

## 2026-08-01 — Phase 2 scope refinement

- Folded the Phase 2 capture doc's concrete findings (CA1873 warning, node
  shutdown hardening via `closeIdleConnections()`, `run-p` replacement with a
  signal-forwarding orchestrator, a designed SIGTERM→grace-period→kill-tree
  `StopServer()` sequence, and the debugger-stop TODO) into `PROJECT.md`'s
  Phase 2 scope bullet, Phases list, and Riskiest unknowns — the capture doc
  (`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`) remains
  the task-by-task implementation plan; `PROJECT.md` now links to it.
- Added OpenTelemetry logging as the concrete delivery mechanism for the
  already-scoped "health/observability" Phase 2 bullet — not a new, separate
  scope line — because it's the *how*, not new scope.
- OTel scope covers **both** runtimes: the .NET host (`PhoriaServerProcess`/
  `PhoriaServerProcessService`) and the Node/Vite sidecar — because the
  sidecar model means production failures can originate in either process,
  and a .NET-only view would miss half the picture. Confirmed starting point:
  the .NET side has zero logging configuration today (default `ILogger<T>`
  injection only, nothing to migrate away from); the Node side has only
  `console.log` and framework built-ins.
- The initial Phase 2 observability scope was logging-only, with traces and metrics left open pending a design. This was superseded by the 2026-08-09 example observability decision, which opted into independently gated logging, tracing, and metrics.
- Consolidated `docs/deferred-issues-phase-1.md`'s five Phase-2-labeled
  entries into the capture doc's own "Known deferred issues" section and
  trimmed them from the source file (rather than duplicating) — the
  remaining entries there (dependency tracker, phase-1/phase-3 code-quality
  follow-ups) are unrelated to Phase 2 and were deliberately left in place,
  not pulled forward just because Phase 2 is starting next. `gh` remains
  unavailable in this environment, same as at Phase 1 close-out, so none of
  these are filed as real GitHub issues yet.

## 2026-08-01 — Phase 2 Aspire orchestration

- Chose Aspire AppHost, rather than only a standalone dashboard container, for local preview convenience: it starts the .NET WebApp, the sibling Node server, and the Aspire dashboard with one command and owns resource coordination.
- Development and Preview examples use `AddJavaScriptApp` with the WebApp package's `dev:server` or `preview:server` script; Production can instead use `PhoriaServerProcess` to own Node from the .NET host.
- Narrowed the `run-p` replacement to parallel build scripts. Aspire supersedes
  the old `run-p preview:*` orchestration instead of adding a second preview
  runner.

## 2026-08-03 — Phase 2 server-robustness close-out

- Hardened `PhoriaServerProcess` around timer capture, spawn-window shutdown,
  shared stop tasks, process-tree termination, and semaphore ownership.
- Made SSR stream-pool disposal deterministic across successful, failed, and
  abandoned renders; production certificate validation uses the system trust
  store while development retains dangerous local-cert acceptance.
- Isomorphic islands degrade to client-only when the server is unhealthy and
  return to SSR after monitor recovery. The monitor waits for its first healthy
  check before startup proceeds.
- Added the optional OTel-independent `PhoriaLogger` seam to Node handlers;
  e2e servers adapt their OTel loggers without adding OTel to the published
  package.
- Centralized full numeric logger event IDs in nested `EventId` feature groups,
  preserving emitted IDs while removing composite `EventFeature` expressions.
- The maintained examples use sibling Aspire AppHosts for Development and Preview orchestration; the .NET-owned Node-process model remains documented and covered by the production configuration.
- Deferred `IMemoryPoolFactory<byte>` post-1.0 because it lacks the stream and
  buffer-writer semantics required by current consumers. Aspire 13.4.6 SIGINT
  cleanup remains an environmental limitation, and Node shutdown OTel delivery
  remains an observability gap requiring collector-backed testing.

## 2026-08-04 — E2E app polish

- Dropped the unused `Cwd` option rather than retaining configuration with no effect.
- Removing the production `root = "."` override fixes Docker production asset resolution by preserving the content-root-relative UI path.
- Dropped explicit `--apphost` arguments in favour of committed `aspire.config.json` files and `aspire stop --all` for non-interactive teardown.
- Renamed the E2E smoke test to `test:e2e` to describe the full end-to-end test command.
- Split CI into a `test-e2e` job covering the maintained example apps.

## 2026-08-09 — Example OpenTelemetry observability

- Adopted a shared `phoria:observability` configuration for the .NET WebApp and Node/Vite sidecar, with independent opt-in gates for logging, tracing, and metrics; tracing uses the `Phoria` ActivitySource and a `phoria.ssr.render` span around each SSR call.
- The Node `@phoria/opentelemetry` package parses the shared appsettings files, configures NodeSDK instrumentation, enriches h3 spans for SSR and CSR requests, falls back to console logging when OTel logging is disabled, and flushes providers during shutdown.
- `/hc` is filtered from Node spans and metrics and from .NET spans; the .NET host's `/hc` client metrics remain a residual limitation because OpenTelemetry .NET 1.17.0 exposes no per-request filter for the runtime-built `System.Net.Http` metrics. `logHealthChecks` controls periodic health logging so the monitor logs status changes without repeating a stable result.
- Examples use local package linking during development before publication; release sync restores registry ranges and must update the new package references after publication.

## 2026-08-11 — Configurable Phoria Server unavailable policy

- New `PhoriaServerUnavailableBehavior` (`Degrade`/`Fail`) plus `Server.StartupTimeout` (seconds, 0 = wait indefinitely) and `Server.Process.MaxRestartAttempts` (0 = unlimited), all defaulting to today's behavior and all .NET-only (the Node appsettings parser ignores unknown keys).
- `Fail` makes unavailability explicit: islands throw (page 500), unclaimed GETs return 503, and the process supervisor gives up after the restart limit; `Degrade` keeps best-effort serving with the server-only suppression now logged instead of silent.
- Startup fail-fast is a monitor concern (a `WaitAsync` timeout on `firstHealthy`); the monitor service stops the monitor cleanly then rethrows, so the host stops under `BackgroundServiceExceptionBehavior.StopHost`.
- The monitor preserves the last-known healthy `Mode`/`Frameworks` through downtime, and the entry tag helpers suppress asset output while unhealthy (fixes dev URLs leaking into production when status defaults to `Development`).
- New opt-in `AddPhoriaServerHealthCheck` (`IHealthCheck` reporting `Healthy`/`Degraded`/`Unhealthy` per policy) wired into both examples alongside `/health`, with a `Fail` + `startupTimeout: 60` production/preview policy so orchestrators can restart the app when recovery has failed.
- The restart counter is a plain count reset on healthy; a time-window bound was considered and rejected for v1.

## 2026-08-13 — v1 milestone scope expansion (TODO.md review)

- Reviewed the `TODO.md` backlog and expanded the v1 milestone from the original Phase 3-5 plan into new Phases 3-10: Test consolidation, Canary & release workflow, Docs & guides, Vite assets, DX & tooling, Exploration spikes (incl. props generator), Examples, Release prep.
- Test consolidation sequenced first (before the asset-bundling feature) so feature work lands on the strongest possible regression net.
- Canary/beta publishing is a pre-v1 requirement: beta packages on npm/NuGet unblock example/CI/docker-compose integration testing (the unpublished `@phoria/opentelemetry` blocker). Changesets prereleases are the riskiest unknown here.
- Docs phase sequenced before the remaining feature work so docs stay current as features land; a final pass remains in Release prep.
- Props generator (TypeScript types → C# POCOs) is an exploration spike (go/no-go), not a committed v1 feature.
- Examples catalog triage deferred to Examples-phase start; docs website deferred post-v1 (would itself be a Phoria app on Render).
- ARCHITECTURE's `<outDir>/server` (no `phoria/` prefix) confirmed deliberate — the Docs phase adds the rationale, no code change.
- `develop` is only 3 commits ahead of `main`, each very large; the canary branch absorbs the integration rather than a single big-bang merge.

## 2026-08-13 — Phase 3 (test consolidation & review) scope

- Coverage tooling is in scope, thresholds are not: Vitest `v8` provider (`@vitest/coverage-v8`) per JS package and `coverlet.MTP` for `Phoria.Tests` (MIT, native MTP extension via `dotnet test --coverlet` — consistent with the FluentAssertions licensing stance that rejected Microsoft's closed-source CodeCoverage extension); coverage runs in CI as reporting-only steps.
- "High signal-to-noise" is defined by three criteria applied together — regression-catching, value-per-task, ratio-with-size/cost — with delete-unless-real-else-rewrite as the default disposition for the survey's 13 low-signal candidates.
- The signal-to-noise review is a standing end-of-phase practice, not a Phase-3 one-off: pre-1.0 there is no public contract, so behavior is still being shaped and tests must be kept current with development changes.
- The Svelte/Vue gap means full parity with React, not "some more tests": CSR browser tests (both packages gain `vitest.browser.config.ts` + `test:browser`), `server/ssr` unit tests, `main.ts` registration tests, and plugin-test parity (Vue's missing `setSsrEnvironment` test, React's untested `hydrate` path).
- Coverage-hole triage sits at the public-contract bar: fix what consumers hit (routing/`/hc`/CSR paths, `client/phoria-island` + `idle` directive, manifest readers, `ViteDevServerHmrProxy`, entry-scripts tag helper, preload html content, dev-certs); deep internals (buffer writers, client factories, singleton state, background-service wiring) are documented accepted gaps.
- Dedup: JS fixtures consolidated in-package; .NET stubs/fakes consolidated into `TestUtilities/` (StubServerMonitor ×5, StubHttpClientFactory/StubHttpMessageHandler ×3, ListLogger ×3, TrackingSsr ×2, WaitUntilAsync ×2, StubUrlHelper ×2, StubManifestReader ×2, TagHelper context/output helpers ×3; plus the vestigial empty `Health/` dir). Framework plugin tests deliberately stay parallel copies — the packages publish independently, so cross-package sharing isn't possible.

## 2026-08-13 — Phase 3 design (test consolidation & review spec)

- Coverage is `all: true` per package — report every `src` file, untested or not — because `all: false` hides the very holes the phase exists to document; CSR-only modules (`csr.tsx`/`csr.ts`) showing ~0% is the intended interpretation (they run only in the browser config), not a gap to chase.
- JS coverage runs only the node-env suite via a per-package `test:coverage` script (`vitest run --coverage`) + root turbo task; browser-mode tests don't contribute (accepted gap), and `coverage.include` stays `src/**` so `tests/utilities/` is excluded for free.
- Upgrading wins over mitigating: the phase starts with a non-major dependency sweep so known Vite-8 coverage quirks (ignore-hint loss, uncovered-`.tsx` parse drop) are fixed by a newer vite/vitest/coverage-v8 rather than excluded around; a narrow `exclude` for browser-only csr modules is the documented fallback, not a workaround layer.
- .NET coverage uses `coverlet.MTP` via `dotnet test --coverlet` with a file prefix for the two TFMs; `coverlet.collector`/`coverlet.msbuild` are dead under MTP, and Microsoft's CodeCoverage extension was rejected as closed-source (same licensing model as FluentAssertions v8).
- Coverage runs in the existing `build-and-test` CI job as reporting-only steps + artifact upload, not a new job — avoids the per-push setup cost and quota concern raised in TODO.md.
- Golden-fixture assertions relax to structural checks (path prefix + extension) while the golden JSON manifest stays as the ssr-manifest format contract — exact-hash asserts broke on every dependency bump without catching format regressions a structural assert wouldn't.
- Test file layout for JS packages, codified for future work: co-locate tests with `src`; shared/multi-consumer utilities go in `<pkg>/tests/utilities/` (mirroring .NET `TestUtilities/`) with one concern per file and specific names — no generic `test-utils` dump.
- Signal-to-noise dispositions decided per candidate (13 identified): delete the tautological/stub-self-test/order-dependent ones, rewrite the non-deterministic/seam-internal ones to observable behavior, keep the two real-contract ones, relax the hash asserts — and no deleted test may be the sole coverer of a line the public-contract fixes don't then cover.
- Framework parity is full: Svelte/Vue gain `vitest.browser.config.ts` + `test:browser` + CSR mount/hydrate tests, SSR unit tests, registration tests, and plugin-test parity (Vue's `setSsrEnvironment`, React's `transform`/`applyToEnvironment` + hydrate case); Vue's `mode`-less `csr.ts` is locked as current behavior, real Vue hydration is an accepted gap.

## 2026-08-13 — Phase 3 implementation sequencing (plan)

- Implementation order is the spec's task order with no parallelism: Task 0 (dep sweep) gates Task 1 (coverage tooling) because the two known Vite-8 coverage quirks get an upgrade-over-mitigation verdict before any `exclude` is committed; Task 2 (dedup) precedes Task 3 (signal-to-noise) and Task 5 (.NET coverage holes) so the extracted `TestUtilities/` stubs and `tests/utilities/` JS fixtures are consumed, not re-created.
- Tooling decision: `test:coverage` is a turbo task (`dependsOn: ["^build"]`, `outputs: ["coverage/**"]`) aggregated by a root `pnpm test:coverage`; CI appends three steps + one `actions/upload-artifact@v5` (JS `coverage/**`, .NET `TestResults/*.cobertura.xml`) to the existing `build-and-test` job — no new job.
- "Public seams only" is the .NET test seam: internal types (`PhoriaOptionsExtensions`, `PhoriaServerHttpClientFactory`, `ViteDevServerHmrProxy`) are covered through their public consumers (`PhoriaServerMonitor.Url`, the `UsePhoria()` middleware proxy path, tag-helper dev URLs, the public static `IViteDevServerHmrProxy.IsHmrRequest` + `ProxyAsync`). No new deps, no `InternalsVisibleTo`, no API changes.
- Confirmed public for direct tests: `PhoriaServerMonitor`/`IPhoriaServerMonitor`, `ViteManifestReader`, `ViteSsrManifestReader`, `ViteManifestExtensions` (`GetRecursiveCssFiles` with the cycle guard), `PhoriaIslandEntryScriptsTagHelper`/`EntryStylesTagHelper`, `PhoriaIslandPreloadHtmlContent`, and the `PhoriaServerProcess` seam ctor (`processId`, `stopGracePeriod`, `beforeProcessIdAssignment`) — these unblock Task 5 without internals access.
- Framework parity scope confirmed: svelte/vue gain `vitest.browser.config.ts` + `test:browser` + CSR mount/hydrate tests (svelte needs a `Hello.svelte` fixture + `*.svelte` ambient declaration; vue uses plain options components with `h()`), SSR unit tests, registration tests (`vi.resetModules()` + dynamic import of `./client/main`/`./server/main`, comparing to the service imported from `./csr`/`./ssr`), and plugin parity; Vue CSR keeps mount-always and locks it with a test.
- The shared-registry hazard behind the `register-fakes` utility: consumers must `vi.resetModules()` then dynamically import `tests/utilities/register-fakes` alongside their dynamic import of the module under test — a static import captures a stale registry across resets. The fixture file carries a comment explaining this.
- Accepted gaps to keep in the ARCHITECTURE coverage table: live HMR transceive loop (vendor Quetzal Rivera code), singleton-state observability paths, CSR-only modules at ~0% v8 (browser config doesn't contribute), internal `PhoriaServerHttpClientFactory.BaseAddress` (covered indirectly by middleware proxy tests).

## 2026-08-14 — Task 0: test-toolchain dependency sweep (quirk verdict)

- Catalog bumped in `pnpm-workspace.yaml`: `vite` ^8.1.5 → ^8.2.1 and `playwright` ^1.62.0 → ^1.62.1 (both the highest versions resolving in-range via `pnpm view <pkg>@^<major> version`), plus `@vitest/coverage-v8` added at ^4.1.10 pinned to the same range as `vitest`. `vitest` and `@vitest/browser-playwright` already sat at the newest available 4.x (`4.1.10`, confirmed via `pnpm view vitest@^4 version` and the `latest` dist-tag; 5.x is still beta/rc), so those two catalog entries were unchanged by the sweep. `@vitest/coverage-v8` was also added as a `catalog:` devDependency to `packages/phoria-react/package.json` (the only package.json change) so `vitest run --coverage` resolves the v8 provider.
- Q1 (Rolldown/Oxc parse noise and duplicate-file reporting for `.tsx` sources under `all: true`): FIXED by the sweep — reproduced at `vitest 4.1.10` / `@vitest/coverage-v8 4.1.10` / `vite 8.2.1` / `playwright 1.62.1` with the temporary `coverage: { provider: "v8", all: true, include: ["src/**/*.{ts,tsx}"] }` block, the `vitest run --coverage` output was pristine: zero Rolldown/Oxc parse errors, zero duplicate-file warnings, and a clean six-file v8 report (`src/main.ts`, `src/client/csr.tsx`, `src/client/main.ts`, `src/server/main.ts`, `src/server/ssr.tsx`, `src/vite/plugin.ts`). Because `vitest`/`@vitest/coverage-v8` are unchanged at 4.1.10, the fix correlates with the `vite 8.1.5 → 8.2.1` bump (the only changed variable in the transform pipeline).
- Q2 (`.browser.test.tsx` files matched by `*.{ts,tsx}` counted as uncovered sources): FIXED by the sweep — at `vitest 4.1.10` / `@vitest/coverage-v8 4.1.10` / `vite 8.2.1` / `playwright 1.62.1`, `src/client/csr.browser.test.tsx` appeared nowhere in the report; the "All files" aggregate and per-directory rows listed only the six real `src` modules (Vitest's default coverage excludes still hide `**/*.test.{ts,tsx}` sources, so no additional `exclude` is needed). With both quirks fixed, Task 1's fallback narrow `exclude` for `src/client/csr.tsx` / `src/client/csr.ts` is not required; the temporary coverage block was reverted from `packages/phoria-react/vitest.config.ts` before committing.

## 2026-08-14 — Phase 3 closeout

- Phase 3 coverage consolidation is complete: shared JS/.NET utilities are centralized, low-signal tests were rewritten or removed, Svelte/Vue test breadth matches React, and the public-contract coverage holes are covered without adding API surface or `InternalsVisibleTo`.
- Final verification is reporting-only and green: `pnpm build`, `pnpm lint`, `pnpm check`, `pnpm test`, `pnpm test:browser`, `pnpm test:coverage`, `dotnet test --solution Phoria.sln --configuration Release`, and `pnpm examples:check` are the closeout commands.
- Task 5 coverage results are recorded in ARCHITECTURE Table 4. Browser-mode tests are deliberately excluded from v8 reports; the remaining accepted gaps are the live HMR transceive loop, singleton-state observability paths, browser-only CSR modules in v8, and the internal HTTP-client base-address branch covered indirectly through middleware.

## 2026-08-15 — Phase 4 design (canary & release workflow spec)

- **A single shared `release.yml` runs on pushes to both `main` and `canary`**, replacing the earlier plan for a separate canary workflow. npm trusted publishing allows exactly one trusted-publisher config per package, keyed to one workflow filename, so both streams must publish from the same file; repo state (pre.json present on canary, absent/exit on main) decides beta vs stable, not the branch.
- **npm publishing moves from `NPM_TOKEN` to trusted publishing (OIDC)**: the workflow gains `id-token: write`, the token is removed, and the npm CLI auto-detects GitHub OIDC during `changeset publish` (adding provenance automatically). Driven by the 2026-07-08 GAT 2FA-bypass deprecation. NuGet is unchanged (`NUGET_API_KEY`) — nuget.org has no OIDC equivalent.
- **Staged publishing (`npm stage publish`) investigated and deferred**: `changeset publish` has no staged mode (would need a custom publish loop re-implementing dist-tag/git-tag handling), a brand-new package cannot be staged (so `@phoria/opentelemetry`'s first publish is direct regardless), and for a sole maintainer the human gate it adds already exists as the version PR + canary→main cut + branch protection. Trusted publishing alone satisfies the deprecation; revisit post-1.0.
- **`canary` replaces `develop`**: created at develop HEAD, `ci.yml` triggers move `develop` → `canary`, and feature PRs target canary; `main` gets only coordinated stable cuts. First betas use the natural 0.x prerelease versions from the 15 pending minor/patch changesets, so consumers benefit from the phased v1 work as stable releases as it matures; the `1.0.0` major changesets stay queued for Phase 10.
- **Examples sync to beta refs on canary** after each beta publish (`examples:bump`), fixing the `@phoria/opentelemetry` docker blocker and satisfying the docker-compose success criterion.
- **Branch protection on `main` + `canary`** (web UI; `gh` unavailable): require PRs, require CI checks, block force pushes, restrict push to maintainers, allow admin bypass as an emergency escape hatch, and **no required approvals** — GitHub never counts the PR author's own approval, so requiring them would deadlock a sole maintainer. Enable approvals when a second active maintainer exists.
- **`CONTRIBUTING.md` added at the repo root** (prerequisites, dev setup, branch model, contribution flow, release workflow + stable-cut runbook, publishing security) — the public-facing counterpart to the agent-focused AGENTS.md.
- The design is recorded in `docs/superpowers/specs/2026-08-15-canary-release-workflow-design.md` and the release flow in ARCHITECTURE's new `## Release workflow` section. Follow-ups: staged publishing (post-1.0), peer-range reconciliation to `^1.0.0` at the 1.0.0 cut, `gh`-based branch-protection automation.

## 2026-08-15 — Phase 4 rehearsal (canary prerelease flow)

- Rehearsed the Changesets prerelease flow in a throwaway worktree with the pending Phase 4 changesets. `pnpm install` completed cleanly using pnpm 11.17.0.
- The initial rehearsal with the old peer range produced the peer cascade: `@phoria/phoria` -> `0.5.0-beta.0`, the four peer packages -> `1.0.0-beta.0`, dev-certs -> `0.3.0-beta.0`, and `phoria-dotnet` -> `0.5.0-beta.0`. Changesets rewrote the peers to `>=0.5.0-beta.0` and dropped the old upper bound.
- The revised rehearsal first widened all four peer ranges to `>=0.5.0-0 <2.0.0`. Beta versions then stayed natural and in the 0.x family: `@phoria/phoria`/`@phoria/phoria-react` -> `0.5.0-beta.0`, `@phoria/phoria-svelte`/`@phoria/phoria-vue` -> `0.4.0-beta.0`, `@phoria/opentelemetry` -> `0.2.0-beta.0`, `@phoria/vite-plugin-dotnet-dev-certs` -> `0.3.0-beta.0`, and `phoria-dotnet` -> `0.5.0-beta.0`. The peer ranges remained unchanged because the core beta was in range.
- The generated `.changeset/pre.json` had `mode: "pre"`, `tag: "beta"`, `initialVersions` of `phoria-dotnet`/`@phoria/phoria`/`@phoria/phoria-react` = `0.4.2`, `@phoria/opentelemetry` = `0.1.0`, `@phoria/phoria-svelte`/`@phoria/phoria-vue` = `0.3.2`, and `@phoria/vite-plugin-dotnet-dev-certs` = `0.2.1`, plus 15 observed changeset IDs: `build-app-server-environment`, `decouple-h3-from-public-api`, `dotnet-10-xunit-v3`, `eager-rivers`, `graceful-server-process-stop`, `otel-observability-examples`, `quiet-island-styles-css-warning`, `quiet-otlp-requests`, `raise-node-and-pnpm`, `typescript-6-vite-plugin-dts-5`, `unavailable-server-policy`, `update-remaining-dependencies`, `vite-8-rolldown`, `vite-plugin-correctness`, and `widen-phoria-peer-range`.
- Stable-cut rehearsal with `pre enter beta`, `pre exit`, and `pnpm run version` removed `pre.json` cleanly and produced `@phoria/phoria` -> `0.5.0`, `@phoria/phoria-react` -> `0.5.0`, `@phoria/phoria-svelte` -> `0.4.0`, `@phoria/phoria-vue` -> `0.4.0`, `@phoria/opentelemetry` -> `0.2.0`, `@phoria/vite-plugin-dotnet-dev-certs` -> `0.3.0`, and `phoria-dotnet` -> `0.5.0`. Stable peers remained `>=0.5.0-0 <2.0.0` for all four peer packages. The revised rehearsal worktree was removed and pruned; the implementation worktree remained clean before these documentation edits.

## 2026-08-15 — Phase 4 implementation spec gap

- Task 4 review found that the documented branch-local Changesets config is not self-consistent across the stable-cut merge: `canary` commits `baseBranch: "canary"`, but merging it into `main` carries that value into the stable branch unless an explicit restoration step or workflow seam changes it back to `main`. The existing workflow does not provide that seam, so stable-cut behavior is not yet safe to implement.
- The same review found that deleting a fixed `chore/examples-sync` branch before every publish can orphan an existing maintainer PR and can collide across the independent `main` and `canary` concurrency groups. The examples-sync branch/PR identity needs a design decision before the workflow is finalized.
- The tag step also needs to use an explicit tag-only push rather than `git push --follow-tags`, which pushes the protected branch ref as well as tags.

## 2026-08-15 — Phase 4 spec reconciliation

- **Runtime branch normalization approved:** `.changeset/config.json.baseBranch` is normalized to `GITHUB_REF_NAME` immediately before Changesets runs. The canary commit remains `canary`; the main release path repairs the value to `main` after the canary→main merge; the stable-cut runbook restores `canary` before re-entering beta mode. This avoids pretending one merged file can retain two branch identities.
- **Release-specific examples branches approved:** successful publishes use `chore/examples-sync-<branch>-<commit>` and never delete or overwrite a fixed branch. This prevents abandoned maintainer PRs and cross-stream collisions; historical short-lived branches are an accepted cost.
- **Tags-only publishing approved:** the workflow pushes tags explicitly rather than using `git push --follow-tags`, which can push a protected branch ref.
- **Failure boundaries:** build failure prevents Changesets; publish failure prevents tag/examples steps; examples-sync failure cannot republish packages and is independently retryable.

## 2026-08-15 — Phase 4 execution (canary release workflow)

- **GH006 finding:** branch protection rejects the workflow's direct `git push`; the classic "allow specified actors to bypass required pull requests" setting only skips the pull-request requirement, not required status checks.
- **Approved resolution:** the examples sync lands as a `gh`-created pull request, which the maintainer merges.
- **Workflow shape:** one shared `release.yml` runs on `main` and `canary`, grants `id-token: write` for npm trusted publishing, and has no `NPM_TOKEN`; NuGet continues to use `NUGET_API_KEY`.
- **Branch-creation deviation:** `canary` was created at the current `HEAD`, not at bare `develop` `HEAD`.

## 2026-08-15 — Phase 4 Task 2 documentation alignment

- Public release documentation now records the implemented seam: `.changeset/config.json.baseBranch` is normalized from `GITHUB_REF_NAME` at runtime immediately before Changesets runs. Stable-cut recovery restores `baseBranch: "canary"` after merging `main` back into `canary`, before beta mode and `pre.json` are committed again.
- Examples synchronization is release-specific (`chore/examples-sync-<branch>-<commit>`), opened as a pull request for the publishing branch. The workflow does not delete or overwrite a fixed branch, and the maintainer merges the examples-sync PR separately.
- Publishing is tag-only (`git push origin --tags`), so the release step does not push a protected branch ref. Failure boundaries are explicit: build failure prevents Changesets, publish failure prevents tag and examples-sync steps, and examples-sync failure cannot republish packages and is independently retryable.
- This entry documents the implemented workflow only; external branch protection, trusted-publisher configuration, package publishing, and registry verification were not performed locally.

## 2026-08-16 - Phase 4 first-publish recovery

- A brand-new npm package cannot be created by trusted publishing (OIDC), so its first version requires a maintainer bootstrap publish. That bootstrap must use `pnpm publish`, not plain `npm publish`, because pnpm literalizes workspace `catalog:` specifiers in the published manifest.
- The manual `@phoria/opentelemetry@0.2.0-beta.0` publish left `catalog:` dependencies in the npm artifact and blocked example installs. The recovery republishes `0.2.0-beta.1` through the workflow's pnpm-based Changesets path; no source catalog literalization is needed.
- After a publish recovery, verify the published manifest with `npm view <package>@<version> dependencies` before running the examples sync. The missing `@phoria/opentelemetry@0.2.0-beta.0` git tag belongs on the `5d99cde` release commit for history consistency.
