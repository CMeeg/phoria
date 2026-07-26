# Phoria — v1 Milestone

> Islands architecture framework for .NET powered by Vite. Renders islands of
> interactivity (React, Svelte, Vue) within .NET web apps (Razor Pages / MVC)
> via both CSR and SSR.

This document defines the **v1 milestone**: what a stable, production-ready,
honestly-describable `1.0.0` release means and why. Architecture, schema, and
detailed task breakdown belong in the spec/plan that follows.

## Problem

Phoria has been dormant for a while and sits at `0.4.x` (with package version
drift). It works, but it can't be confidently described as production-ready:
there are **no automated tests**, a **known server-process shutdown bug**, rough
edges in production error handling, and dependencies/platform targets that have
fallen behind (Vite 6, .NET 8/9). Without a safety net, dependency updates and
refactors are risky, and the public API can't be committed to.

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
- Establish a **test foundation** (unit + e2e) as the base for everything else.
- **Modernize** dependencies and platform (Vite 7, latest React/Svelte/Vue,
  .NET 10 incl. memory pools).
- **Harden the server** for production robustness (top author concern).
- Deliver one committed new feature: **Vite bundling of .NET-referenced static
  assets**.
- **Explore** additional feature ideas via timeboxed spikes, without blocking
  the release.

## Success

v1 is successful when:

- Unit tests (Vitest for JS, xUnit for .NET) and Playwright e2e tests exist and
  run in CI; the root `test` script is real (currently a stub).
- The known vite-process shutdown bug is fixed and no longer reproducible.
- Production error handling degrades gracefully when the SSR server is
  unhealthy (no raw attribute/vite-client leakage).
- All packages are on current deps and target .NET 8/9/10.
- Vite bundling of .NET-referenced static assets works end-to-end.
- All packages reach `1.0.0` (independent versioning thereafter), docs are
  updated, and remaining inline TODOs are resolved or tracked as issues.

## Scope

### In

- Test foundation: Vitest (JS packages), xUnit (.NET), Playwright (e2e apps).
- Dependency/platform updates: Vite 7, latest React/Svelte/Vue, .NET 10 +
  memory pools; keep `net8.0;net9.0;net10.0`.
- Server robustness: process shutdown bug, production error handling, lifecycle
  hardening (in-process start, monitor/reconnect, graceful degradation),
  health/observability.
- Vite bundling of .NET-referenced static assets (committed feature).
- Timeboxed exploration spikes (go/no-go): nested component composition,
  streaming/Suspense, server actions, Deno/other adapters.
- Release prep: version reconciliation to `1.0.0`, docs pass, changesets,
  GitHub v1 milestone/issues, inline-TODO cleanup.

### Out

- Formal API freeze / audit (approach is **best-effort stable** for v1; API may
  still evolve with minor bumps post-1.0).
- Web components library (deferred to post-v1).
- Dropping `net8.0`/`net9.0` (they reach end of support Nov 2026 — revisit
  later).
- Any exploration-spike feature that does not pass its go/no-go gate.

## Constraints

- Node.js v22.11.0, pnpm 9.15.0, .NET SDK 9.0.100 (rolls forward) — see repo
  config files.
- Monorepo: pnpm workspaces + Lerna (publishing) + Nx (caching/task deps).
- CI order: `build` → `lint` → `check` (tests to be added to this pipeline).
- Independent per-package versioning; all packages must reach `1.0.0`.
- `package.json` files are excluded from Biome formatting (do not re-enable).

## Phases (ordering)

Detailed tasks live in the implementation plan; this is the agreed sequence.

0. **Test foundation** — Vitest, xUnit, Playwright; wire root `test` + Nx/Lerna
   + CI. Written against *current* behavior as the regression net.
1. **Dependency & platform updates** — Vite 7, React/Svelte/Vue latest, .NET 10
   + memory pools. Done *after* tests so regressions are caught; new deps/APIs
   may also help later phases.
2. **Server robustness & production-readiness** — shutdown bug, prod error
   handling, lifecycle hardening, health/observability.
3. **Vite bundling of .NET-referenced static assets** — includes a design spike
   first (riskiest unknown).
4. **Exploration spikes** — composition, streaming/Suspense, server actions,
   Deno adapters; timeboxed with go/no-go gates.
5. **Release prep** — version reconciliation to `1.0.0`, docs pass, changesets,
   GitHub milestone/issues, inline-TODO cleanup.

## Riskiest unknowns

- **Vite bundling of .NET-referenced static assets** *(highest)* — the
  mechanism is unproven and could force more fundamental design changes across
  the Vite plugin and .NET manifest/TagHelper layers. Gated behind a design
  spike before implementation.
- **Server-process shutdown bug** — root cause unknown; reproduces mainly when
  stopping the debugger. Tied to the author's top worry (server robustness).
- **.NET 10 adoption** — memory-pool and lifecycle API changes may interact
  with the server process/monitor design.
- **Dependency upgrade breakage** — Vite 6→7 and framework majors may surface
  breaking changes; mitigated by tests-first ordering.

## Open questions (TODO)

- TODO: Confirm the concrete mechanism/design for Vite bundling of
  .NET-referenced assets (resolve in the Phase 3 spike).
- TODO: Decide go/no-go outcomes for each exploration spike (composition,
  streaming, server actions, Deno).
- TODO: Confirm whether `net8.0`/`net9.0` retirement happens within v1 or after
  (currently: keep all three).
