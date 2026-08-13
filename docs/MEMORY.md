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
