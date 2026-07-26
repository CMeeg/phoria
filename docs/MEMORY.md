# Memory

Dated log of decisions made while shaping the project. One line each, with the why.

## 2026-07-26 — v1 milestone scoping

- v1 = "stability + a few key features", not a full feature-complete vision — because the priority is a release the author can confidently talk about, not shipping every idea.
- Tests are Phase 0 (first) — for long-term health, to de-risk dep updates, and to re-familiarize with the codebase.
- Test stack chosen: Vitest (JS), xUnit (.NET), Playwright (e2e) — mainstream, well-supported fits for each layer.
- Dependency/platform updates (Phase 1) reordered *before* server robustness (Phase 2) — because new deps/.NET 10 APIs may provide cleaner primitives for the robustness fixes, avoiding double work.
- Target .NET 8/9/10 for now (not 10-only) — 8/9 reach end of support Nov 2026, retire later; keeping them widens adoption.
- Platform stance: bleeding edge (Vite 7, latest React/Svelte/Vue, .NET 10 incl. memory pools).
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
