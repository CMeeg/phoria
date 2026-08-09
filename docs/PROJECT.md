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
Phase 2 server-robustness close-out have now addressed those baseline risks;
remaining milestone work is tracked below.

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
- **Explore** additional feature ideas via timeboxed spikes, without blocking
  the release.

## Success

v1 is successful when:

- Unit tests (Vitest for JS, xUnit for .NET) and Playwright browser tests exist and
  run in CI; the root `test` script is real.
- The known vite-process shutdown bug is fixed and no longer reproducible.
- Server-process lifecycle events and errors — in both the .NET host and the Node/Vite sidecar — emit structured logs via OpenTelemetry, while opt-in tracing and metrics provide production operators cross-runtime visibility into requests, start/stop/crash behavior, and health.
- Production error handling degrades gracefully when the SSR server is
  unhealthy (no raw attribute/vite-client leakage).
- All packages are on current deps and target .NET 8/10.
- Vite bundling of .NET-referenced static assets works end-to-end.
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
- Vite bundling of .NET-referenced static assets (committed feature).
- Timeboxed exploration spikes (go/no-go): nested component composition,
  streaming/Suspense, server actions, Deno/other adapters.
- Release prep: version reconciliation to `1.0.0`, docs pass, changesets,
  GitHub v1 milestone/issues, inline-TODO cleanup.

### Out

- Formal API freeze / audit (approach is **best-effort stable** for v1; API may
  still evolve with minor bumps post-1.0).
- Web components library (deferred to post-v1).
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
   stays deferred to Phase 3. Task-by-task detail:
   [`docs/superpowers/plans/2026-08-01-phase-1.5-close-out-deferred-issues.md`](superpowers/plans/2026-08-01-phase-1.5-close-out-deferred-issues.md).
2. **Server robustness & production-readiness** — **complete.** The shutdown bug (including the `Process.Kill()` process-tree bug), the `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, the unconditional `DangerousAcceptAnyServerCertificateValidator`, prod error handling, lifecycle hardening, hardened shutdown paths for the Node/Vite sidecar (signal handling, `run-p` replacement), opt-in OpenTelemetry logging, tracing, and metrics across the .NET host and Node/Vite sidecar, `.NET 10` memory-pool assessment, and deferred-issue close-out are complete. The memory-pool replacement remains deferred post-1.0. Task-by-task detail:
   [`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`](superpowers/plans/2026-08-01-phase-2-server-robustness.md)
   and [`docs/superpowers/plans/2026-08-02-phase-2-server-robustness-closeout.md`](superpowers/plans/2026-08-02-phase-2-server-robustness-closeout.md).
3. **Vite bundling of .NET-referenced static assets** — includes a design spike
   first (riskiest unknown).
4. **Exploration spikes** — composition, streaming/Suspense, server actions,
   Deno adapters; timeboxed with go/no-go gates.
5. **Release prep** — version reconciliation to `1.0.0`, docs pass, changesets,
   GitHub milestone/issues, inline-TODO cleanup. NOTE: framework peer ranges on
   `@phoria/phoria` are widened to `>=0.4.0 <1.0.0` during pre-1.0 (to prevent
   premature `1.0.0` releases via the changesets peer cascade); tighten them to
   `^1.0.0` as part of this phase.

## Riskiest unknowns

- **Vite bundling of .NET-referenced static assets** *(highest)* — the
  mechanism is unproven and could force more fundamental design changes across
  the Vite plugin and .NET manifest/TagHelper layers. Gated behind a design
  spike before implementation.
- **Server-process shutdown bug** — likely root cause narrowed to
  `Process.Kill()` missing `entireProcessTree` plus a `StartServer`/
  `StopServer` semaphore race; a debugger-stop-specific TODO in
  `PhoriaServerProcessService` still needs confirming after the fix lands.
  Tied to the author's top worry (server robustness).
- **Dual-runtime OpenTelemetry observability** — the .NET host and Node/Vite sidecar are separate runtimes, so the examples use a shared `phoria:observability` contract with independent opt-in logging, tracing, and metrics gates; tracing propagates across the SSR HTTP boundary and `/hc` is excluded from spans and metrics.
- **.NET 10 adoption** — memory-pool and lifecycle API changes may interact
  with the server process/monitor design.
- **Dependency upgrade breakage** — Vite 6→8 (Rolldown/Oxc) and framework
  majors may surface breaking changes; mitigated by tests-first ordering.

## Open questions (TODO)

- TODO: Confirm the concrete mechanism/design for Vite bundling of
  .NET-referenced assets (resolve in the Phase 3 spike).
- TODO: Decide go/no-go outcomes for each exploration spike (composition,
  streaming, server actions, Deno).
- RESOLVED (Phase 1): `net9.0` is dropped now — it is STS and reaches EOL the
  same day as `net8.0` (2026-11-10), so keeping it cost a TFM leg for zero
  extra coverage. `net8.0` is retained until its Nov 2026 EOL, then revisited.
- Aspire AppHosts in the examples use an OTLP HTTP exporter for dashboard
  visibility; decide later whether a production exporter/target and .NET↔Node
  log correlation across the sidecar boundary are required for v1.
