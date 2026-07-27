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
- **Rolldown did NOT break the ssr-manifest key contract (Task 7).** The plan's hazard #1 — the single most likely place for unplanned work — did not materialize: Rolldown emitted ssr-manifest keys compatible with the existing `__phoriaComponentPath` → preload-tag mapping, with no `?commonjs-*` suffix changes observed. Task 1's preload assertion (added specifically to catch this) passed unmodified; no `ssr-manifest.rolldown.json` fallback or golden-file update was needed.
- **TypeScript 6 silently required `paths` values to be relative (Task 4).** The plan assumed `tsconfig.json` `paths` entries were unaffected by dropping `baseUrl`, but TS 6 requires `./`-prefixed relative paths once `baseUrl` is absent — all 9 tsconfig files needed `"src/*"` → `"./src/*"`. This also exposed that `phoria-react`, `phoria-svelte`, and `phoria-vue` were missing an explicit `@types/node` devDependency, previously masked by `baseUrl` auto-resolution.
- **Most catalog version bumps had already landed by Task 7, ahead of Task 11 (the dedicated dependency-sweep task).** Task 7's Vite 8 catalog rewrite incidentally brought react/react-dom, svelte, vue, magic-string, empathic, cross-env, postcss-preset-env, destr, and several other transitive deps to their Task-11 target versions as part of getting Vite 8 to build cleanly. Task 11 correctly scoped itself down to only the remaining direct (non-catalog) deps: `defu`, `mime`, `tinyexec`, `std-env`, and `CliWrap`.
- **`net9.0` dropped from the TFM list (Task 6), against `PROJECT.md`'s original "keep all three."** net9 is STS and reaches EOL the same day as net8 (2026-11-10), so carrying it cost a TFM leg and conditioned `PackageVersion` entries for zero additional coverage. `PROJECT.md` is amended in Task 12 to reflect `net8.0;net10.0`.
- **`.NET 10` memory pools stayed out of Phase 1 as planned, and `PROJECT.md`'s Phase 1 goals bullet is corrected in Task 12** to stop implying they shipped this phase — they move to Phase 2 alongside the other `Phoria.IO`/server-robustness fixes (`Process.Kill()` process-tree bug, `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, unconditional `DangerousAcceptAnyServerCertificateValidator`).
- **CI required more than an action-version bump.** The .NET 10 SDK ships only the .NET 10 runtime, but `Phoria.Tests` still targets `net8.0`, so every `setup-dotnet` step in `ci.yml` now also installs `8.0.x` explicitly. `global.json`'s `test.runner: Microsoft.Testing.Platform` also changed the `dotnet test` invocation shape: `dotnet test Phoria.sln` (VSTest-style) must become `dotnet test --solution Phoria.sln` in MTP mode, or the solution path is silently misinterpreted.
- No task's verification run (`build` → `lint` → `check` → `test` → `dotnet test` → browser tests → e2e smoke) needed more than a same-task fix round to go clean — the two fix rounds that did occur (Task 3's Svelte/Vue Counter variable-name regression from a lint autofix, Task 7's overly-strict `@sveltejs/vite-plugin-svelte` peer range) were code-review findings, not CI failures. That is a stronger-than-expected result for a twelve-task, bundler-major-version phase.
