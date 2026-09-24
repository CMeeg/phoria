# Deferred Issues — Phase 1 (Dependency & Platform Updates)

Compiled at Phase 1 close-out (Task 12 of
[`docs/plans/2026-07-27-phase-1-dependency-platform-updates.md`](plans/2026-07-27-phase-1-dependency-platform-updates.md)).
`gh` was not available in the execution environment, so these were never filed
as real GitHub issues. Rather than wait for `gh` access, every entry except
the one below has since been fixed directly and migrated out of this file
(Phase 2's five items → the Phase 2 capture doc; everything else → Phase 1.5).
The one remaining entry is deliberately deferred to Phase 7, not blocked on
`gh`.

This file remains until the deferred Phase 2 and Phase 7 items are either
resolved or moved into their owning phase documentation. The Phase 1.5 items
were completed directly and are documented in the Phase 1.5 close-out plan.

---

## Phase 2 — Server Robustness

The five items originally recorded here (process-tree kill, `StartServer`/
`StopServer` semaphore race, undisposed `StreamPool`s, unconditional
`DangerousAcceptAnyServerCertificateValidator`, `IMemoryPoolFactory<byte>`
adoption) have moved to the "Known deferred issues" section of
[`docs/superpowers/plans/2026-08-01-phase-2-server-robustness.md`](superpowers/plans/2026-08-01-phase-2-server-robustness.md),
which is now the single Phase 2 scope reference. See that document instead.

---

## Phase 1.5 — Remaining Phase 1 close-out

The external-dependency item (`@meeg/vite-plugin-inspect-config@0.3.0`) and
all seven "Minor code-quality follow-ups" that were previously listed here
(dedupe `LoadGoldenManifest()`, `StubUrlHelper.ActionContext` fail-fast,
the `PhoriaIslandEntryTagHelper.cs` indentation regression, the redundant
`serverEntry !== false` guard, the pending changeset's `applyToEnvironment`
wording, the `phoria-island.test.ts` isolation-pattern alignment, the
HMR/dev-cert manual verification, and the Dockerfile `node:22-slim` pin)
have moved to
[`docs/superpowers/plans/2026-08-01-phase-1.5-close-out-deferred-issues.md`](superpowers/plans/2026-08-01-phase-1.5-close-out-deferred-issues.md).
See that document instead.

---

### Add `ViteChunk.Name` property

**Body:** Vite (since at least v7/v8/Rolldown) emits a `"name"` field in
manifest chunks that the .NET `ViteChunk` POCO does not currently model.
Harmless to omit today, but Phase 7 ("Vite bundling of .NET-referenced
static assets") will likely want name-based chunk lookup — add
`public string? Name { get; init; }` when that work starts. Flagged as
Task 7's hazard #2 in the original plan; deliberately deferred, not an
oversight.

**Labels:** `phase-7`, `enhancement`
**Milestone:** v1 (or re-scope to Phase 7 milestone if one exists)
