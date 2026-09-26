# Memory

Dated log of durable decisions made while shaping the project. Later entries supersede earlier exploratory choices where noted; implementation details belong in the architecture and milestone docs.

## 2026-08-16 — Examples parity spec gap

- Storybook 10.5.8 no longer publishes a compatible `@storybook/addon-essentials` package; its essentials features are in Storybook core. The parity plan nevertheless requires the addon package and an Essentials addon configuration. Task 9 uses Storybook core and documents the incompatibility, but the plan/spec must be reconciled before implementation can continue.
- Approved reconciliation: keep Storybook 10 core-only essentials, remove the impossible addon dependency/configuration requirement, and document the rationale. Downgrading to Storybook 8 or using a 9.0 alpha was rejected because either violates the Storybook 10/Vite 8 requirements or introduces an unstable peer mismatch.

## 2026-08-16 — Workspace package component paths

- Historical context, resolved by Task 8: directly registering a component from a workspace package rendered but lost production preloads because the framework transform excluded `node_modules/**` and its cwd-relative path format did not match root-relative SSR manifest keys for modules resolved outside the WebApp root. The app-root re-export shim worked because it restored the `__phoriaComponentPath` chain. Direct imports now work for explicitly opted-in workspace packages; the original cause, constraints, and candidate directions are documented in [`docs/2026-08-16-component-path-for-workspace-packages.md`](2026-08-16-component-path-for-workspace-packages.md).

## 2026-08-16 — Workspace component-path implementation decisions

- Chose an explicit `workspacePackages: string[]` opt-in over package inference or marker files because the behavior is predictable, discoverable, and avoids transforming unrelated external modules.
- Adopted a root-relative, no-leading-slash manifest-key wire format globally. The .NET preload helper therefore removes its legacy `Root` prefix strip while retaining `TrimStart('/')` for existing values.
- Approved Shape A: move the complete shared framework-plugin shell into `@phoria/phoria/vite` as `createPhoriaFrameworkPlugin`, leaving React, Svelte, and Vue as thin framework-specific composers. This centralizes the subtle workspace resolution and transform logic, removes duplicated dependencies, and provides one deep test suite in core.
- Tighten framework peer lower bounds to the core minor that ships the factory, preventing a new framework plugin from resolving against a core package without the factory export.
- Sequence React first, prove the direct workspace import through the `with-workspace` e2e test, then mirror the implementation to Svelte and Vue.

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

## 2026-08-15 — Phase 4 canary & release workflow

- A single shared `release.yml` runs on pushes to both `main` and `canary`; `.changeset/config.json.baseBranch` is normalized from `GITHUB_REF_NAME` immediately before Changesets runs, while `pre.json` presence/exit state determines beta versus stable behavior. This avoids carrying the canary branch identity into a stable cut and is required because npm trusted publishing allows one workflow filename per package.
- npm publishing uses trusted publishing (OIDC) with `id-token: write` and no `NPM_TOKEN`; staged publishing was investigated and deferred post-1.0 because Changesets has no staged mode, a new package cannot be staged, and the maintainer already has human gates through version PRs, stable cuts, and branch protection. NuGet likewise uses trusted publishing through `NuGet/login@v1`.
- `canary` replaces `develop` as the integration branch. Feature PRs target canary, main receives coordinated stable cuts, and the first beta stream stays in the natural 0.x family. Rehearsal verified that framework peer ranges of `>=0.5.0-0 <2.0.0` prevent the Changesets peer cascade; update that tuple before later beta cycles and reconcile to `^1.0.0` at the 1.0.0 cut.
- Successful publishes open release-specific `chore/examples-sync-<branch>-<commit>` PRs using `pnpm examples:bump`; the workflow never deletes or overwrites a fixed branch, pushes tags only, and has explicit failure boundaries: build failure prevents Changesets, publish failure prevents tag/examples-sync, and examples-sync failure cannot republish packages.
- Branch protection exposed GH006 when the workflow tried to push directly; the examples sync therefore lands as a `gh`-created pull request. Protection on `main` and `canary` requires PRs and CI, restricts pushes to maintainers, and has no required approvals because GitHub does not count a sole maintainer's own approval. `CONTRIBUTING.md` is the public-facing counterpart to this agent guidance.

## 2026-08-16 — Phase 4 first-publish recovery

- A brand-new npm package cannot be created by trusted publishing (OIDC), so its first version requires a maintainer bootstrap publish. That bootstrap must use `pnpm publish`, not plain `npm publish`, because pnpm literalizes workspace `catalog:` specifiers in the published manifest.
- The manual `@phoria/opentelemetry@0.2.0-beta.0` publish left `catalog:` dependencies in the npm artifact and blocked example installs. The recovery republishes `0.2.0-beta.1` through the workflow's pnpm-based Changesets path; no source catalog literalization is needed.
- After a publish recovery, verify the published manifest with `npm view <package>@<version> dependencies` before running the examples sync. The missing `@phoria/opentelemetry@0.2.0-beta.0` git tag belongs on the `5d99cde` release commit for history consistency.

## 2026-08-16 — NuGet trusted publishing

- NuGet.org supports GitHub Actions trusted publishing. The release workflow uses `NuGet/login@v1` with the `meeg` nuget.org profile name, exchanges the job's OIDC token for a short-lived API key, and passes that key to the existing `scripts/dotnet/publish.js` path through `NUGET_API_KEY`.
- The NuGet trusted-publishing policy is tied to repository owner `CMeeg`, repository `phoria`, and workflow file `release.yml`; no GitHub Actions environment is configured. The long-lived `NUGET_API_KEY` GitHub secret should remain only until the OIDC publish is verified, then be removed.

## 2026-08-16 — Main branch source guard

- Added `.github/workflows/canary-to-main.yml`, a checkout-free required check for PRs targeting `main`. It allows `canary`, `changeset-release/*`, and `chore/examples-sync-*`; other head branches fail with an explicit error.
- The check is required only on `main`, in addition to the existing CI checks. It is intentionally not required on `canary`, where feature PRs land.
- The workflow is first merged into `canary`, then a `canary` to `main` PR is opened and left parked. The check can then be selected in the `main` branch rule without merging the stable cut. Until the guard file reaches `main`, other PRs targeting `main` remain blocked with an expected-but-unreported status; merging the parked cut makes the guard report explicit failures for disallowed sources.

## 2026-08-16 — Examples parity design

- Split the Examples phase: parity with the archived `phoria-examples` repository is a new Phase 5 before Docs; the existing new-example scope moves to Phase 10 after Docs, Vite assets, DX/tooling, and exploration.
- Parity includes all seven missing examples: `framework-react`, `framework-vue`, `framework-svelte`, `with-workspace`, `with-tailwind`, `with-styled-components`, and `with-storybook`.
- Rebuild parity examples on the current AppHost/Aspire, OpenTelemetry, standalone-workspace, and e2e template. Reuse useful old content but remove obsolete Lerna/Nx and root-package markers.
- `with-workspace` uses `apps/WebApp` plus `packages/ui` under a root pnpm workspace. Existing examples keep `WebApp/`; example tooling must discover both layouts and derive relative package, project-reference, and lockfile paths.
- Local examples e2e runs all discovered examples by default. The examples-sync workflow uses the quota-bounded allow-list `getting-started,framework-multiple,with-workspace`; ports for the seven new examples are 5173, 5273, 5473, 5673, 5773, 5873, and 5973 respectively.
- `with-tailwind` uses Tailwind v4.2.2+ through `@tailwindcss/vite`. `with-styled-components` uses the existing `renderComponent` seam with `ServerStyleSheet`; no framework API change is required. `with-storybook` pins Vite to `~8.0.16` until the Vite 8.1.x/Rolldown Storybook regression is fixed.
- Add per-example READMEs, an `examples/README.md` index, and a contributor checklist; archive the old repository only after parity reaches `main`.

## 2026-08-17 — Task 8 component-path decisions confirmed

- Workspace component transforms require explicit `workspacePackages` opt-in; package inference and marker files remain rejected so unrelated dependencies are not transformed.
- The component-path wire format is globally root-relative with no leading slash and matches the SSR manifest key; the .NET preload helper retains `TrimStart('/')` for compatibility but no longer strips the configured Vite root.
- Shape A is implemented: `@phoria/phoria/vite` owns the shared `createPhoriaFrameworkPlugin` shell, while React, Svelte, and Vue remain thin framework-specific composers.
- React-first sequencing proved direct workspace-package registration through the `with-workspace` e2e path before the behavior was mirrored to Svelte and Vue.

## 2026-09-25 — Phase 5 close-out: prod testing, and two retracted diagnoses

- Examples parity merged through `canary`: PR #37 (parity) → PR #38 (published the betas) → PR #39 (`chore/examples-sync-*`) merged 2026-09-24. Published: npm `@phoria/phoria` / `phoria-react` `0.5.0-beta.1`, `phoria-svelte` / `phoria-vue` `0.4.0-beta.1`, `@phoria/opentelemetry` `0.2.0-beta.2`; NuGet `Phoria 0.5.0-beta.1`. The workspace feature shipped in the `0.5.x` stream, not the `0.6.x` the close-out plan had predicted.
- **Prod testing of all nine examples: green, with no code change.** Sequential `docker compose up --build -d` per example, waiting for `GET /health` to report body `Healthy`, then that example's own e2e suite against the container on port 8080 (37 assertions across the nine). This closes the TODO item that was gated on "wait until next merge into canary".
- **Standalone giget consumption: 9/9 green.** `npx giget@latest gh:CMeeg/phoria/examples/<name>#canary`, then `pnpm install --frozen-lockfile` and `pnpm build` in the fetched copy — no source repo, no root workspace. Closes the giget TODO.
- **Preview e2e green again: 9/9 unrestricted, 3/3 on the CI allow-list** (both exit 0), after PR #39 synced the examples to `0.5.0-beta.1`. The `with-workspace` modulepreload failure recorded in the parity plan was registry version skew against `0.5.0-beta.0`, now resolved.
- **`with-workspace`'s Production appsettings is re-aligned with the other eight.** It had been the one artefact of the `fdca4e2` debugging session and diverged on four axes: a `host: 127.0.0.1` pin, no `observability` block, no `startupTimeout: 60` / `unavailableBehavior: "Fail"`, and no `maxRestartAttempts: 5`. The last three meant it was the only example whose Production container would silently **degrade** (serve without the node child) rather than **fail** when the Phoria server could not start. All four are now aligned; verified by a Production container reaching `Healthy` and passing its e2e suite under the stricter `Fail` policy.
- **Retracted — the `phoria.server.host` "core fix candidate".** The close-out review proposed changing the core default from `localhost` to `127.0.0.1`, on the theory that `localhost` resolves to `::1` only in the `aspnet:10.0` image while .NET's `HttpClient` prefers IPv4, making the Production supervisor unable to probe the node child it spawned. Measured in the real image: the node child binds `[::1]` **only**, the monitor's connection is ESTABLISHED on that same IPv6 socket, and `/health` returns `Healthy`. A/B on with-workspace: no host key and `127.0.0.1` pinned are both healthy. The `0.0.0.0:5573` probe URL in the review's note matches no committed config (`GetServerUrl()` uses `Server.Host` verbatim; no `0.0.0.0` in `packages/Phoria` or any example JSON; with-workspace's appsettings never had a `host` key before `fdca4e2` *added* one). The `127.0.0.1` pin papered over leftover local state from a debugging session. An implementation of the change was written, tested, and reverted rather than shipped on a false premise.
- **Retracted — "giget is blocked until parity reaches `main`".** The 404 was a default-branch mismatch, not an unpublished tree: the unqualified `gh:CMeeg/phoria/examples/<name>` resolves `main`, and `examples/` is not there yet because PR #36 (`canary` → `main`) is still open. `#canary` works today. Note the ref syntax is `#`, not `@` (`@` is npm dist-tag syntax). All nine example READMEs and `examples/README.md` still document the unqualified form, which will keep 404ing until PR #36 merges.
- **Two verification traps worth remembering.** The monitor reports `Unhealthy` until the node child binds, so a health probe fired in the first second or two sees a **genuine 503** that resolves moments later — allow for that startup window and require two consecutive healthy readings instead of trusting the first poll. The status code itself is right: every example calls `app.MapHealthChecks("/health")` with no options, so the ASP.NET Core default mapping applies, `Unhealthy` returns **503** (confirmed by forcing the node child to fail, and now pinned by `PhoriaServerHealthCheckEndpointTests`, which drives the real endpoint over a real socket) and `Degraded` returns 200 so a `Degrade` deployment is not killed by its own probe. Separately, in a shell harness, `if some-command | tail` tests *tail's* exit status, so a failing suite silently reports success; capture to a file and branch on the command's own status.
- **Archive condition unchanged:** `phoria-examples` is archived only after parity reaches `main`, i.e. after PR #36 merges. Not done, deliberately.

## 2026-09-26 — Phase 6 scope: the docs slice is coupling, not completeness

- Phase 6 was reframed from "flesh out placeholders" into a reader-journey restructure plus a documentation-review trigger. The placeholder count is the cheap fix; the rot mechanism is the actual defect, and filling placeholders would leave it untouched.
- **The documentation-review convention already exists** — `CONTRIBUTING.md` line 90 asks contributors to review the READMEs and `docs/guides/` when a change alters how Phoria works — and six phases of v1 work drifted through it. So the fix is to give the convention a *trigger* (a required step in task definitions, in `CONTRIBUTING.md`, `AGENTS.md`, and the plan documents), not to restate it. Its efficacy is unproven and unenforced by choice; review at the next phase boundary rather than treating it as settled.
- **Automated documentation checks are explicitly declined** — no link checker, no referenced-symbol check, no code-block runner. The rot-prevention weight is carried by the review convention alone.
- Three doc audiences, three media: `README.md` plus the front of `docs/guides/` (see it and try it), the guides body (dig deeper), `ARCHITECTURE.md` plus `AGENTS.md` (contribute). Guides carry **concepts, never runtime internals** — promotion rule: if a reader cannot make a correct configuration decision without the detail it stays in the guide; if it describes how Phoria is built rather than used it moves to `ARCHITECTURE.md`.
- The getting-started and first-island path is the priority; deeper guides and reference material may still carry placeholders when the slice lands. Reorganisation must be additive at the entry-point level — reorder and add, never delete and rewrite the five substantial guides (52KB, the work of six phases).
- **The current branch is treated as `main`.** The `canary` → `main` cut is the final step of the slice, so branch-sensitive refs are written unqualified (`gh:CMeeg/phoria/examples/<name>`) with `#canary` as an aside. Consequence accepted: the ten `giget` references (nine example READMEs plus `examples/README.md`) **cannot be verified until after the cut**, so verification is a post-cut step.
- Verified while scoping: `examples/` does not exist on `main` at all, `canary` is 19 commits ahead, and the ten documented `giget` commands 404 today. The README's guides index is honest — it links only the 5 real guides, never a placeholder — and the 9 placeholder-bearing files are therefore orphaned rather than mis-advertised. Preserve that property when restructuring.
- Out of scope for the slice: docs website (already post-1.0), automated docs checks, the HTTPS-in-Preview decision (research stays in the DX and tooling phase), and Phase 7 / Phase 10 work.
- `gh` is unavailable in the working environment, so PR #36 can be neither inspected nor actioned from an agent session. Spec: [`docs/superpowers/specs/2026-09-26-phase-6-docs-journey-design.md`](superpowers/specs/2026-09-26-phase-6-docs-journey-design.md). `PROJECT.md` was deliberately left unchanged; the slice is scoped in its own spec.

## 2026-09-26 — Phase 6 design: extraction, not invention

- **The slice is an extraction, not an invention.** `getting-started.md` is already the reader journey (clone → prerequisites → Vite → component → register → Phoria Server → client entry → server entry → .NET wiring → Tag Helpers → island → run → preview), and the eight placeholder guides are named after exactly the concepts it walks through. **Seven links into those empty files are already live** across the two real guides, the sharpest being `getting-started.md:434`, which defines "Phoria Web App" *by* linking to an empty file. This is why the work is cheap and why the completeness problem and the rot problem share one fix.
- **The seam test replaces per-paragraph judgement:** does this paragraph explain *this concept* (→ concept guide) or *this step in this order* (→ journey)? A mechanical test is reviewable paragraph by paragraph, which is what keeps the next change to the structure small and local.
- **The coupling trigger is a mandatory named docs-review task in every plan**, recorded in `CONTRIBUTING.md`, `AGENTS.md` and the writing-plans convention. Rejected: automated checks (declined — no link or symbol checker, no code-block runner); a formal "when is N/A allowed" rule (declined); a per-package docs-surface declaration (over-engineering for a single maintainer pre-1.0); and merely sharpening the existing `CONTRIBUTING.md` wording, which is the shape that already failed to fire across six phases. **The eighteen existing plans are deliberately not retrofitted** — they are historical records of completed work, and rewriting them prevents no future rot.
- **The trialist path leads with `giget` + `docker compose up` + localhost:8080**, because it is the only route needing no local toolchain (.NET, Node, pnpm and Aspire all absent is survivable). Rejected: leading with the dev loop (a four-item prerequisite wall, not a minute); a hosted demo (needs new infrastructure, and the docs site is already deferred). Its two halves are already proven 9/9, and it is the same work as repairing the ten broken `giget` refs. At most three commands as one block may be restated in `README.md`; anything longer has exactly one home.
- **Concept guides stay whole in `docs/guides/`; docs do not move into packages.** Phoria's concepts are not package-decomposable — "how do I get an island working" spans `phoria-islands`, a framework package, the `Phoria` NuGet package, the dev-certs plugin and `@phoria/opentelemetry`, so splitting by package separates the framework-agnostic half of a concept from its specific half. The framework packages are *deliberately* thin (that is what the shared factory is for), and a thin package earns a thin README. Independent versioning would also multiply the surfaces that must agree across a release.
- **Placement rule, uniform and with no exception:** a document describing one package's own public surface lives next to that code; anything about how Phoria's pieces cooperate stays whole in the guides. `README.md` and `docs/guides/` are **human-first** and must stand alone; `ARCHITECTURE.md` is **agent-first** and readable by humans, so it may summarise a canonical document and link to it. **Duplication is a permitted fallback, not a defect** — if an agent reads `ARCHITECTURE.md` and does not follow a link, the fix is to duplicate, decided by observation rather than in advance.
- **"Does it work with X / show me X" links the example; "what does this export do" links the package.** `examples/framework-svelte` is executable and already green under e2e, whereas a package README answers a different question — and five of the seven are 70-79 byte stubs today.
- **`createPhoriaFrameworkPlugin` is a package-local recipe, canonically at `packages/phoria-islands/docs/framework-plugin.md`.** `ARCHITECTURE.md:263-288` describes what the three existing adapters do; nothing described how to write a fourth. `ARCHITECTURE.md` keeps its summary and links, because it is where agents arrive. This is the **first `docs` folder inside a package**, and it establishes the convention: **a package's documentation lives in its README, or in a `docs` folder inside the package** — one file per topic, linked from that README, not shipped in the published artefact. A README holds what fits in a README; the folder is created when it does not. Never a single page placed ad hoc beside a source file (`vite/framework.md` is invisible to anyone browsing the package rather than already reading it), because discoverability beats proximity. It is the prerequisite for the open `phoria-preact` item in `TODO.md` — writing it before that work is the test of whether it is sufficient.
- **All six design questions were closed before planning.** `phoria-islands` takes the **concept register** (three files: concept, how-to, reference) because the how-to is React sample code while the concept is framework-agnostic. A retained placeholder must **name what the guide will cover, point to the nearest working alternative, and promise no date** — replacing the generic warning that named neither. Example READMEs take a **uniform heading skeleton with existing prose verbatim**, because rewriting would lose the per-example detail that makes each legible. The `#canary` aside goes in **two places, not ten** (root `README.md` trialist block and the `examples/README.md` catalog), noting the separator is `#` not `@`; the nine per-example READMEs document the released state. Post-cut verification of the ten `giget` refs becomes **step 6 of the existing stable-cut runbook** at `CONTRIBUTING.md:72-77`, run by the maintainer doing the cut (agents cannot — no `gh`). `README.md` gets a **journey-shaped index** replacing the flat five-item list, keeping the honesty property of advertising only real content.
- **Package READMEs are the highest-leverage fix for a registry-arriving reader**: `Phoria`, `phoria-islands`, `phoria-react`, `phoria-svelte` and `phoria-vue` are 70-79 byte stubs and they are the npm and NuGet landing pages. Uniform shape is purpose → install → use → link back. `Phoria.Tests` is unpublished and gets none.
- **All nine example READMEs get one uniform shape** (what it demonstrates → try it with Docker → develop it → test it → learn more). None currently has a single `##` heading and none mentions Docker at all. Uniformity is possible precisely because the examples are at parity.
- **Verification is proportionate per document class.** The trialist path is *executed* — it is new, and its commands are the ones currently broken. The extracted concept guides are read-through only, because the prose is being moved rather than rewritten, so executing would test the movement rather than the content. The ten `giget` refs are verifiable only *after* the `canary` → `main` cut, since an unqualified ref resolves the default branch.
- **Prerequisites stay inline in `getting-started.md`** (`:26-40`, including the Linux `SSL_CERT_DIR` note). This closes the open question about a standalone requirements page: there is already one.
- **The slice is two plans, not one** — extraction + trialist path + trigger, then the sixteen-file README sweep. Sixteen mechanical rewrites in one plan would bury the only part carrying real risk.
- **Correction — `with-workspace` was never missing Docker.** A `find -maxdepth 2` could not reach `apps/WebApp/Dockerfile` at depth 3, so only the one example with the alternate layout reported as having no Dockerfile, and that false negative nearly put a phantom "add docker compose to `with-workspace`" task into the spec. All nine examples have a compose file, a Dockerfile and a `.dockerignore` on port 8080, corroborated by the phase 5 close-out. **When checking layout parity across examples that have two different layouts, do not bound the search depth.** This is also why the slice writes no product code.

Spec: [`docs/superpowers/specs/2026-09-26-phase-6-docs-structure-design.md`](superpowers/specs/2026-09-26-phase-6-docs-structure-design.md).

## 2026-09-26 — Phase 6 plan: sequencing, and two corrections to the spec

Plan: [`docs/superpowers/plans/2026-09-26-phase-6-docs-structure.md`](superpowers/plans/2026-09-26-phase-6-docs-structure.md) (Plan A of two; Plan B is the sixteen-file README sweep).

- **The trigger lands before the work, not after.** Task 1 records the documentation-review requirement in `CONTRIBUTING.md`, `AGENTS.md` and the `writing-plans` skill, and Tasks 2-12 then execute under it. A convention introduced at the end of a slice protects nothing inside that slice.
- **The `writing-plans` record lives outside the repository**, at `~/.local/share/opencode/packages/superpowers/skills/writing-plans/SKILL.md`. It is unversioned and appears in no commit, so the two in-repository records must stand on their own.
- **The three `getting-started.md` extractions are sequential, not parallel**, despite touching disjoint line ranges. They mutate one 24KB file, so concurrent delegation conflicts on every hunk — and the plan's line ranges (`206-386`, `387-408`, `432-493`) are its load-bearing detail.
- **Task 4 deliberately leaves one line behind.** The `configuration` note at `getting-started.md:455` sits inside the range Task 4 extracts, and it is a configuration concept, not a Phoria Web App one. Task 4's loss check expects exactly `1`; Task 7 moves the note and the same check must then report `0`. The changing expectation is stated in both tasks so neither reads as a failure.
- **Task 9 depends on Tasks 2-6.** The guide index may only link non-placeholder guides, so it cannot be written until the extraction has decided which those are.
- **Task 0 exists because the spec's largest risk had no detector.** "Extraction can quietly lose content" was mitigated only by a read-through — the kind of check that misses what it is looking for. Two shell helpers now make it mechanical, and Task 0 *proves the loss detector fires* before any task relies on it. A detector that has never fired is not a detector.
- **The nine per-example `giget` references are Plan B's, not Plan A's** — a deviation from the spec's plan decomposition. They live one-per-file in `examples/*/README.md`, which Plan B rewrites wholesale to add the `## Try it` section that contains the command. Repairing them in Plan A would edit a line Plan B then moves. All ten are still repaired across the two plans; only the attribution changed.
- **Correction — there are eight dead links, not seven.** The spec's Context table omits `getting-started.md:63`, which links to `supported-ui-frameworks.md`. The work is unaffected, since that file is covered either way, but the spec's count is wrong.
- **Correction — the ten `giget` references are not syntactically broken.** They use lowercase `gh:cmeeg/phoria`, which GitHub resolves case-insensitively. They fail only because `examples/` is absent from `main` until the `canary` → `main` cut, which is why verification is step 6 of the stable-cut runbook rather than something an agent session can complete.
- **Three facts about the framework factory that the sources contradict, found while writing the recipe:** all three framework packages' option types `Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal">`, so only four of the seven options are caller-settable and the other three are package-determined; `registerComponent` throws `Cannot register component "X" because the "Y" framework has not been registered.` at **registration** time, not at render; and Vue's Vite plugin is called unspread (`vue(...)`) where React's and Svelte's are spread (`[...react(...)]`, `[...svelte(...)]`), so a copy-paste of the React composer into another package would produce a subtly wrong plugin list.

## 2026-09-26 — The docs-review trigger has two layers, and only one of them is durable

The trigger installed by Phase 6 Plan A is recorded in three places, and they are **not equal**. Keeping the distinction straight matters, because the third is invisible in a diff and vanishes on a rebuild.

- **Layer 1 — the guarantee.** `CONTRIBUTING.md` `## Documentation` and `AGENTS.md` `## Documentation review`. Versioned in this repository, committed, reviewable in a diff, and surviving any machine change. **The slice depends only on this layer.**
- **Layer 2 — the safety net.** The `writing-plans` skill's plan-document template, at `~/.local/share/opencode/packages/superpowers/skills/writing-plans/SKILL.md`. This is what makes a documentation review appear in a new plan *without anyone remembering it* — the difference between a convention and a habit. It is a global tool installation, so it is unversioned, uncommitted, unreviewable, and **destroyed by a tool-installation rebuild**.

Losing Layer 2 degrades rather than breaks: a plan author reading this repository's conventions still gets the requirement. But the loss is silent, and it applies to **every project on the machine**, not just this one. The exact snippets are inlined in Plan A's Task 1 Step 3 so re-applying needs no recollection — **that plan is the durable record of a change that itself is not recorded anywhere.** If the skill path has moved, locate it with `find ~ -path '*superpowers/skills/writing-plans/SKILL.md'` rather than creating a stray copy.

**Also outside every plan:** the `canary` → `main` cut, which is the final step of the overall Phase 6 task and cannot be executed from an agent session because `gh` is unavailable. Executing Plan A does **not** finish Phase 6. Plan A extends the cut runbook with step 6 so the deferred verification of the ten `giget` references happens at the right moment, but the cut itself is a maintainer action with no plan behind it.
