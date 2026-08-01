# Deferred Issues — Phase 1 (Dependency & Platform Updates)

Compiled at Phase 1 close-out (Task 12 of
[`docs/plans/2026-07-27-phase-1-dependency-platform-updates.md`](plans/2026-07-27-phase-1-dependency-platform-updates.md)).
`gh` was not available in the execution environment, so these were never filed
as real GitHub issues — they are recorded here, in enough detail to file
directly, so the list survives until someone with `gh` access (or the GitHub
UI) can create them. Each entry below is meant to become one issue; the
subheading is the suggested title.

**Once filed:** delete this file and let the GitHub issues (added to the v1
milestone, per the plan's Task 12 step 8) be the source of truth.

---

## Phase 2 — Server Robustness

The five items originally recorded here (process-tree kill, `StartServer`/
`StopServer` semaphore race, undisposed `StreamPool`s, unconditional
`DangerousAcceptAnyServerCertificateValidator`, `IMemoryPoolFactory<byte>`
adoption) have moved to the "Known deferred issues" section of
[`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`](superpowers/plans/2026-08-01-phase-2-server-robustness.md),
which is now the single Phase 2 scope reference. See that document instead.

---

## External dependency tracker

### Release `@meeg/vite-plugin-inspect-config@0.3.0` with `vite: ^8.0.0`

**Body:** `@meeg/vite-plugin-inspect-config` (author-owned) currently caps
its `vite` peer dependency at `^6.0.0`. Phase 1 (Task 7) worked around this
via a `peerDependencyRules.allowedVersions` entry in `pnpm-workspace.yaml`:

```yaml
peerDependencyRules:
  allowedVersions:
    "vite-plugin-externalize-deps>vite": "8"
    "@meeg/vite-plugin-inspect-config>vite": "8"
```

The plugin was kept (not removed) because it produces the resolved-config
snapshots (`.vite-config/vite.config.json`) used as the Vite 6→8 diff
baseline during the upgrade. Once `0.3.0` ships with `vite: ^8.0.0`, remove
the `@meeg/vite-plugin-inspect-config>vite` line from
`peerDependencyRules.allowedVersions` in `pnpm-workspace.yaml`.

Note: `vite-plugin-externalize-deps>vite` may still need its own entry
depending on that plugin's release cadence — check independently.

**Labels:** `dependencies`, `follow-up`
**Milestone:** v1

---

## Minor code-quality follow-ups (from Phase 1 task reviews)

Each of these was flagged as a Minor finding during a task-scoped or final
whole-branch code review and explicitly triaged as safe to defer — none are
functional defects, all are cheap, low-risk cleanups.

### Deduplicate `LoadGoldenManifest()` test helper

**Body:** `LoadGoldenManifest()` is duplicated identically in both
`packages/Phoria.Tests/Vite/ViteSsrManifestTests.cs` and
`packages/Phoria.Tests/Islands/PhoriaIslandPreloadTagHelperTests.cs` (added
in Task 1). Extract to a shared test utility/base class if the test surface
grows further.

**Labels:** `phase-1`, `tech-debt`, `good-first-issue`
**Milestone:** v1

---

### `StubUrlHelper.ActionContext` returns `null!` instead of throwing

**Body:** In `PhoriaIslandPreloadTagHelperTests.cs` (Task 1), the test
stub's `ActionContext` property returns `null!`. Safe today because the
code under test never accesses it, but a `throw new
NotSupportedException()` would make any future accidental use fail fast
and loud instead of silently propagating a null.

**Labels:** `phase-1`, `tech-debt`, `good-first-issue`
**Milestone:** v1

---

### Indentation regression in `PhoriaIslandEntryTagHelper.cs`

**Body:** Around lines 250–255, a `Task.Factory.StartNew` → `Task.FromResult`
rewrite in Task 6 left the `var linkOutput` block dedented by one tab
relative to its enclosing `foreach` body. Whitespace-only — compiles fine,
all tests pass — but should be reformatted for readability.

**Labels:** `phase-1`, `tech-debt`, `good-first-issue`
**Milestone:** v1

---

### Add `ViteChunk.Name` property

**Body:** Vite (since at least v7/v8/Rolldown) emits a `"name"` field in
manifest chunks that the .NET `ViteChunk` POCO does not currently model.
Harmless to omit today, but Phase 3 ("Vite bundling of .NET-referenced
static assets") will likely want name-based chunk lookup — add
`public string? Name { get; init; }` when that work starts. Flagged as
Task 7's hazard #2 in the original plan; deliberately deferred, not an
oversight.

**Labels:** `phase-3`, `enhancement`
**Milestone:** v1 (or re-scope to Phase 3 milestone if one exists)

---

### Manually verify React HMR preamble and HTTPS dev-cert loading under Vite 8

**Body:** `PhoriaIslandEntryTagHelper.cs` hard-codes
`@vitejs/plugin-react`'s HMR preamble strings
(`window.__vite_plugin_react_preamble_installed__`, `/@react-refresh`).
During Phase 1 (Task 7), `@vitejs/plugin-react@6.0.4`'s `refresh-runtime.js`
was inspected directly and still exports `injectIntoGlobalHook`
compatibly, so this is believed low-risk — but there is no automated test
for HMR behavior, and the HTTPS dev-cert path
(`@phoria/vite-plugin-dotnet-dev-certs` assigning `{ cert, key }` file
paths) was not fully exercised live in the Phase 1 sandbox (no code in that
path changed, so risk is believed low but unconfirmed). Do a manual
`pnpm --filter framework-multiple dev` check over HTTP and HTTPS, confirm
HMR fires in the browser, and close this out.

**Labels:** `phase-1`, `needs-manual-verification`
**Milestone:** v1

---

### Remove redundant `serverEntry !== false` guard in `configEnvironment`

**Body:** In `packages/phoria-islands/src/vite/plugin.ts` (Task 9), the
`configEnvironment` switch's `case environment.server:` re-checks
`serverEntry !== false` even though `configEnvironment` can only ever be
invoked for environments actually present in `config.environments`, and the
`server` key is only registered there under the same condition. The extra
check is dead code — harmless, but could be removed for clarity.

**Labels:** `phase-1`, `tech-debt`, `good-first-issue`
**Milestone:** v1

---

### Tighten Task 9's changeset wording for `applyToEnvironment` scope

**Body:** The changeset added in Task 9 says "Framework plugins now scope
their transform to the client and ssr environments." This is accurate
today (currently `transform` is the only per-environment hook the three
framework plugins define) but `applyToEnvironment` actually scopes the
*entire* per-environment plugin instance, not just `transform` — a future
reader who adds a second per-environment hook to one of these plugins might
be misled into thinking only `transform` is covered. Consider a doc-comment
or changelog clarification rather than relying on the changeset text alone.

**Labels:** `phase-1`, `documentation`, `good-first-issue`
**Milestone:** v1

---

### Align `phoria-island.test.ts`'s setup pattern with `register.test.ts`

**Body:** `packages/phoria-islands/src/server/phoria-island.test.ts` (Task
10) uses `beforeAll` to register a stub "Counter" component/framework/SSR
service shared across its three tests, whereas
`packages/phoria-islands/src/register.test.ts` uses
`beforeEach(() => vi.resetModules())` with a fresh import per test. No
functional risk (Vitest isolates module state per test file by default,
and no test in `phoria-island.test.ts` mutates registry state in a
conflicting way), but for consistency the file could be updated to follow
the repo's established pattern.

**Labels:** `phase-1`, `tech-debt`, `good-first-issue`
**Milestone:** v1

---

### Both e2e Dockerfiles' `uibuild` stage floats on `node:22-slim`

**Body:** `e2e/framework-multiple/WebApp/Dockerfile` and
`e2e/with-workspace/WebApp/Dockerfile` (and the matching example in
`docs/guides/deployment.md`) fixed their runtime-stage `NODE_VERSION` env
var to `24.18.0` during Phase 1's final review fix wave, but the `uibuild`
stage's base image is still the floating tag `FROM node:22-slim`, which is
inconsistent with `.nvmrc`'s pinned `24.18.0`. Confirmed **not currently
broken** — `node:22-slim` resolves to a recent Node 22.x patch that still
satisfies the `engines.node` floor (`^20.19.0 || ^22.12.0 || >=24.0.0`), and
the repo has no `engine-strict`/`engines-strict` enforcement anywhere
(`.npmrc`, `pnpm-workspace.yaml`, or root `package.json`) that would fail
the build even if it didn't. Low-priority consistency cleanup: pin the
`uibuild` stage to `node:24-slim` (or a `24.18.x`-pinned tag) to match the
rest of the toolchain.

**Labels:** `phase-1`, `tech-debt`, `good-first-issue`
**Milestone:** v1
