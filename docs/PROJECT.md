# Phoria — v1 Milestone

> Islands architecture framework for .NET powered by Vite. Renders islands of
> interactivity (React, Svelte, Vue) within .NET web apps (Razor Pages / MVC)
> via both CSR and SSR.

This document defines the **v1 milestone**: what a stable, production-ready,
honestly-describable `1.0.0` release means and why. Architecture, schema, and
detailed task breakdown belong in the spec/plan that follows.

## Problem

Phoria has been dormant for a while and sits at `0.4.x` (with package version
drift). The v1 work began with no automated tests, a known server-process
shutdown bug, rough edges in production error handling, and dependencies/platform
targets that had fallen behind. The test foundation, dependency updates, and
Phase 2 server-robustness close-out have now addressed those baseline risks.

As the completed phases landed, a backlog of follow-up items was collected in
`TODO.md` (categorised by area: tests, examples, docs, canary workflow, DX &
tooling, misc). Reviewing that inventory expanded the v1 milestone beyond the
original Phase 3-5 plan: closing the remaining test gap (Svelte/Vue), a pre-1.0
beta publishing pipeline, a current and consistent docs set, and a polished
examples catalog.

The goal is to bring the project up to date, harden it, and reach a
**`1.0.0` release the author is happy to talk about publicly**.

## Who it's for

- **Primary:** .NET web developers (Razor Pages / MVC) who want modern
  front-end islands (React/Svelte/Vue) without adopting Blazor or a full BFF
  split.
- **Also (this milestone):** the author, re-familiarizing with the codebase and
  establishing long-term project health.
- **Not for:** teams wanting a full SPA framework, or a Blazor replacement.

## Goals

- Ship a **stable, tested, production-ready `1.0.0`** of the existing feature
  set.
- Establish a **test foundation** (unit + integration) as the base for everything else.
- **Modernize** dependencies and platform (Vite 8, latest React/Svelte/Vue,
  .NET 10).
- **Harden the server** for production robustness (top author concern).
- Deliver one committed new feature: **Vite bundling of .NET-referenced static
  assets**.
- Close remaining **test-coverage gaps** (Svelte/Vue) and review Phase-0 test
  quality (signal-to-noise, duplication).
- Establish a **canary/beta publishing pipeline** so pre-1.0 work is tested
  against real published packages.
- Ship a **polished examples catalog** and **docs consistent with completed
  work**.
- **Explore** additional feature ideas via timeboxed spikes, without blocking
  the release.

## Success

v1 is successful when:

- Unit tests (Vitest for JS, xUnit for .NET) and Playwright browser tests exist and
  run in CI; the root `test` script is real.
- The known vite-process shutdown bug is fixed and no longer reproducible.
- Server-process lifecycle events and errors — when the observability logging signal is enabled — emit structured logs via OpenTelemetry in both the .NET host and the Node/Vite sidecar, while opt-in tracing and metrics provide production operators cross-runtime visibility into requests, start/stop/crash behavior, and health.
- Production error handling degrades gracefully when the SSR server is
  unhealthy (no raw attribute/vite-client leakage).
- All packages are on current deps and target .NET 8/10.
- Vite bundling of .NET-referenced static assets works end-to-end.
- Svelte and Vue packages have test coverage on par with React's.
- `beta` versions of the JS and NuGet packages publish from a canary branch and
  are consumable by the examples (including docker-compose).
- A committed example installs and builds standalone (giget fetch end-to-end).
- Docs (READMEs, `docs/guides`, `ARCHITECTURE.md`) are consistent with the
  shipped code.
- All packages reach `1.0.0` (independent versioning thereafter), docs are
  updated, and remaining inline TODOs are resolved or tracked as issues.

## Scope

### In

- Test foundation: Vitest (JS packages), xUnit (.NET), Playwright (browser tests).
- Dependency/platform updates: Vite 8, latest React/Svelte/Vue, .NET 10;
  `net8.0;net10.0` (net9.0 dropped — see Open questions).
- Server robustness: process shutdown bug (`Process.Kill()` process-tree, `StartServer`/`StopServer` semaphore race), production error handling, lifecycle hardening (in-process start, monitor/reconnect, graceful degradation, SIGTERM→grace-period→kill-tree stop sequence), hardened shutdown paths for the Node/Vite sidecar (signal handling, `run-p` replacement with a signal-forwarding orchestrator), and opt-in OpenTelemetry logging, tracing, and metrics across the .NET host and Node/Vite sidecar. Each signal is independently gated; the scope also includes an assessment of `.NET 10` memory pools. `IMemoryPoolFactory<byte>` adoption is explicitly deferred post-1.0 because it does not provide the stream and buffer-writer semantics used by `Phoria.IO`. Task-by-task detail is split between the main plan and its close-out:
  [`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`](superpowers/plans/2026-08-01-phase-2-server-robustness.md)
  and [`docs/superpowers/plans/2026-08-02-phase-2-server-robustness-closeout.md`](superpowers/plans/2026-08-02-phase-2-server-robustness-closeout.md).
- Test consolidation & review: Svelte/Vue package coverage, coverage-holes
  assessment, signal-to-noise review of the Phase-0 tests, mock/stub dedup
  (`TODO.md` `## Tests`).
- Canary & release workflow: canary branch + `beta` publishing to npm and NuGet
  (changesets prereleases), the unpublished-`@phoria/opentelemetry` docker
  blocker, and examples install+build against published packages as a release
  gate rather than per-PR CI (`TODO.md` `## Canary workflow`).
- Docs & guides: drift pass over READMEs/`docs/guides`/`ARCHITECTURE.md` against
  completed work, flesh out placeholders, human/agent writing-style consistency,
  the deliberate `<outDir>/server` layout rationale, and the HTTPS-in-Preview
  decision (`TODO.md` `## Docs`).
- DX & tooling: tsx/tsconfig-paths migration decision, npm-package build review
  (type output, cjs vs ESM-only, exports map), vite dev certs plugin DX
  improvement + Linux verification (`TODO.md` `## Misc`,
  `## Vite dev certs plugin`).
- Examples: getting-started polish + new examples (v1/post-v1 subset triaged at
  phase start) + giget verification (`TODO.md` `## Examples`).
- Vite bundling of .NET-referenced static assets (committed feature).
- Timeboxed exploration spikes (go/no-go): nested component composition,
  streaming/Suspense, server actions, Deno/other adapters, props generator
  (TypeScript types → C# POCOs).
- Release prep: version reconciliation to `1.0.0`, docs pass, changesets,
  GitHub v1 milestone/issues, inline-TODO cleanup.

### Out

- Formal API freeze / audit (approach is **best-effort stable** for v1; API may
  still evolve with minor bumps post-1.0).
- Web components library (deferred to post-v1).
- Docs website (deferred to post-v1; a Phoria web app deployed to Render).
- Examples beyond the triaged v1 subset (deployment/styling variants pending
  Examples-phase triage).
- Dropping `net8.0` (LTS, reaches end of support Nov 2026 — revisit then).
  `net9.0` was already dropped during Phase 1: it is STS and reaches EOL the
  same day as net8.0, so it cost a TFM leg for zero extra coverage.
- Any exploration-spike feature that does not pass its go/no-go gate.

## Constraints

- Node.js v24.18.0, pnpm 11.17.0, .NET SDK 10.0.302 (rolls forward) — see repo
  config files.
- Monorepo: pnpm workspaces + Turborepo (task running/caching) + Changesets (publishing).
- CI order: `build` → `lint` → `check` (tests to be added to this pipeline).
- Independent per-package versioning; all packages must reach `1.0.0`.

## Phases (ordering)

Detailed tasks live in the implementation plan; this is the agreed sequence.

0. **Test foundation** — **complete.** Vitest, xUnit, Playwright; wire root `test` + Turborepo
   + CI. Written against *current* behavior as the regression net.
1. **Dependency & platform updates** — **complete.** Vite 8, React/Svelte/Vue latest, .NET 10.
   Done *after* tests so regressions are caught; new deps/APIs may also help
   later phases. `.NET 10` memory pools deliberately excluded from this phase
   (see Phase 2) — it is a public-API refactor of `Phoria.IO`, not a pure
   dependency bump.
1.5. **Close out remaining Phase 1 deferred items** — **complete.** The leftover entries in
   `docs/deferred-issues-phase-1.md` (test-code dedup, a fail-fast test stub,
   a whitespace regression, a TypeScript narrowing-guard cleanup, a changeset
   wording fix, a test-isolation alignment, a manual HMR/dev-cert
   verification, a Docker base-image pin, and the `@meeg/vite-plugin-inspect-config`
   Vite-8 peer bump) fixed directly rather than filed as GitHub issues (`gh`
   unavailable). `ViteChunk.Name` is the one item *not* included here — it
   stays deferred to Phase 6. Task-by-task detail:
   [`docs/superpowers/plans/2026-08-01-phase-1.5-close-out-deferred-issues.md`](superpowers/plans/2026-08-01-phase-1.5-close-out-deferred-issues.md).
2. **Server robustness & production-readiness** — **complete.** The shutdown bug (including the `Process.Kill()` process-tree bug), the `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, the unconditional `DangerousAcceptAnyServerCertificateValidator`, prod error handling, lifecycle hardening, hardened shutdown paths for the Node/Vite sidecar (signal handling, `run-p` replacement), opt-in OpenTelemetry logging, tracing, and metrics across the .NET host and Node/Vite sidecar, `.NET 10` memory-pool assessment, and deferred-issue close-out are complete. The memory-pool replacement remains deferred post-1.0. Task-by-task detail:
   [`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`](superpowers/plans/2026-08-01-phase-2-server-robustness.md)
   and [`docs/superpowers/plans/2026-08-02-phase-2-server-robustness-closeout.md`](superpowers/plans/2026-08-02-phase-2-server-robustness-closeout.md).
3. **Test consolidation & review** — close the Svelte/Vue package test gap,
   assess remaining coverage holes, review the Phase-0 tests for
   signal-to-noise, and dedupe mocks/stubs/fakes (`TODO.md` `## Tests`).
4. **Canary & release workflow** — a canary branch producing `beta` builds
   (changesets prereleases) published to npm and NuGet for integration and
   production testing; resolves the unpublished-`@phoria/opentelemetry` docker
   blocker; absorbs the develop-ahead-of-main integration rather than a single
   big-bang merge; examples install+build against published packages as a
   release gate, not per-PR CI (`TODO.md` `## Canary workflow`).
5. **Docs & guides** — drift pass over READMEs, `docs/guides`, and
   `ARCHITECTURE.md` against completed phases 0-2; flesh out placeholders;
   human/agent writing-style consistency; document the deliberate
   `<outDir>/server` layout; HTTPS-in-Preview pros/cons decision and docs
   (research in Phase 7). Sequenced before the remaining feature work so docs
   stay current as features land; a final pass remains in Phase 10
   (`TODO.md` `## Docs`).
6. **Vite bundling of .NET-referenced static assets** — includes a design spike
   first (riskiest unknown). `ViteChunk.Name` lands here (deferred from
   Phase 1.5).
7. **DX & tooling** — tsx/tsconfig-paths native migration decision; npm-package
   build review (type output via `vite-plugin-dts`, cjs vs ESM-only,
   `package.json` exports/entry points); vite dev certs plugin DX improvement +
   Linux verification (MacOS ratified via find-docs) (`TODO.md` `## Misc`,
   `## Vite dev certs plugin`).
8. **Exploration spikes** — composition, streaming/Suspense, server actions,
   Deno adapters, and the props generator (TypeScript types → C# POCOs);
   timeboxed with go/no-go gates. Run before Examples so passing spikes can be
   demonstrated there.
9. **Examples** — getting-started polish + new examples (v1/post-v1 subset
   triaged at phase start) + giget fetch verification (`TODO.md` `## Examples`).
10. **Release prep** — version reconciliation to `1.0.0`, docs pass, changesets,
   GitHub milestone/issues, inline-TODO cleanup. NOTE: framework peer ranges on
   `@phoria/phoria` are widened to `>=0.4.0 <1.0.0` during pre-1.0 (to prevent
   premature `1.0.0` releases via the changesets peer cascade); tighten them to
   `^1.0.0` as part of this phase.

## Riskiest unknowns

- **Vite bundling of .NET-referenced static assets** *(highest)* — the
  mechanism is unproven and could force more fundamental design changes across
  the Vite plugin and .NET manifest/TagHelper layers. Gated behind a design
  spike before implementation.
- **Changesets prerelease flow (canary/beta)** — changesets documents
  prereleases as "very complicated"; running a `beta` stream alongside the
  stable `1.0.0` cut (peer-range cascade, example refs, NuGet publishing via
  `scripts/dotnet/publish.js`) must not collide.
- **Examples scope (triage)** — the v1 examples subset is undecided until the
  Examples phase; over-scoping it is the largest schedule risk in the new work.
- **DX & tooling decisions** — dropping cjs is a breaking change to every
  published package; tsx removal touches the documented dev workflow.
- **Dual-runtime OpenTelemetry observability** — the .NET host and Node/Vite sidecar are separate runtimes, so the examples use a shared `phoria:observability` contract with independent opt-in logging, tracing, and metrics gates; tracing propagates across the SSR HTTP boundary; `/hc` is excluded from spans on the .NET host and from both spans and metrics on the Node sidecar, though the .NET host's `/hc` client metrics remain a residual limitation (OpenTelemetry .NET 1.17.0 exposes no per-request filter for the runtime's built-in HTTP client metrics).
- **.NET 10 adoption** — memory-pool and lifecycle API changes may interact
  with the server process/monitor design.
- **Dependency upgrade breakage** — Vite 6→8 (Rolldown/Oxc) and framework
  majors may surface breaking changes; mitigated by tests-first ordering.

## Open questions (TODO)

- TODO: Confirm the concrete mechanism/design for Vite bundling of
  .NET-referenced assets (resolve in the Phase 6 spike).
- TODO: Decide go/no-go outcomes for each exploration spike (composition,
  streaming, server actions, Deno, props generator).
- TODO: Decide the v1 vs post-v1 examples subset (triage at Examples-phase
  start).
- TODO: Define "high signal-to-noise" for the Phase-0 test review (Test
  consolidation start).
- TODO: HTTPS-in-Preview — document http vs https pros/cons for the Phoria
  Server in Dev vs Production and update examples (research in DX & tooling,
  written up in Docs).
- TODO: cjs vs ESM-only package output (DX & tooling).
- TODO: Replace tsx with native Node/tsconfig-paths tooling if a modern path
  exists (DX & tooling).
- TODO: Keep giget as the way to consume examples, or something better
  (Examples phase).
- TODO: Approach for human/agent writing-style consistency in docs (Docs
  phase).
- POST-V1: docs website (a Phoria app on Render) — deferred.
- TODO: Filter the .NET host's `/hc` client metrics once OpenTelemetry .NET (or the runtime) supports per-request metric filtering; the runtime-built `System.Net.Http` metrics cannot currently be filtered per request.
- RESOLVED (Phase 1): `net9.0` is dropped now — it is STS and reaches EOL the
  same day as `net8.0` (2026-11-10), so keeping it cost a TFM leg for zero
  extra coverage. `net8.0` is retained until its Nov 2026 EOL, then revisited.
- Aspire AppHosts in the examples use an OTLP HTTP exporter for dashboard
  visibility; decide later whether a production exporter/target and .NET↔Node
  log correlation across the sidecar boundary are required for v1.
