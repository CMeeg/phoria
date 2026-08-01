# Deferred Issues — Phase 1 (Dependency & Platform Updates)

Compiled at Phase 1 close-out (Task 12 of
[`docs/plans/2026-07-27-phase-1-dependency-platform-updates.md`](plans/2026-07-27-phase-1-dependency-platform-updates.md)).
`gh` was not available in the execution environment, so these were never filed
as real GitHub issues. Rather than wait for `gh` access, every entry except
the one below has since been fixed directly and migrated out of this file
(Phase 2's five items → the Phase 2 capture doc; everything else → Phase 1.5).
The one remaining entry is deliberately deferred to Phase 3, not blocked on
`gh`.

This file remains until the deferred Phase 2 and Phase 3 items are either
resolved or moved into their owning phase documentation. The Phase 1.5 items
were completed directly and are documented in the Phase 1.5 close-out plan.

---

## Phase 2 — Server Robustness (explicitly scoped out of Phase 1)

These five items are named in the plan itself
([Task 6, Step 8](plans/2026-07-27-phase-1-dependency-platform-updates.md))
as deliberately out of scope for a dependency-upgrade phase. They belong to
Phase 2 ("Server robustness & production-readiness") per
[`docs/PROJECT.md`](PROJECT.md).

### `Process.Kill()` does not kill the entire process tree

**Body:** `PhoriaServerProcess` (or wherever the Node server process is
stopped) calls `Process.Kill()` without `entireProcessTree: true`. This is
suspected to be the root cause of the known server-process shutdown bug
described in `docs/PROJECT.md` (reproduces mainly when stopping the
debugger). Fix: pass `entireProcessTree: true` (or the .NET 10 equivalent)
so child processes spawned by the Node server are also terminated.

**Labels:** `phase-2`, `server-robustness`, `bug`
**Milestone:** v1

---

### `StartServer`/`StopServer` has a semaphore race

**Body:** The server process lifecycle's `StartServer`/`StopServer` methods
have a race condition around the semaphore guarding concurrent
start/stop calls. Needs a lifecycle-hardening pass as part of Phase 2's
"in-process start, monitor/reconnect, graceful degradation" work.

**Labels:** `phase-2`, `server-robustness`, `bug`
**Milestone:** v1

---

### Undisposed `StreamPool`s

**Body:** One or more `StreamPool` instances (wrapping
`RecyclableMemoryStream`) are created but never disposed, per the plan's
Task 6 self-review notes. Needs an audit of `Phoria.IO.StreamPool`
lifetimes and proper `IDisposable` cleanup wired into DI/service lifetimes.

**Labels:** `phase-2`, `server-robustness`, `bug`
**Milestone:** v1

---

### Unconditional `DangerousAcceptAnyServerCertificateValidator`

**Body:** The server process's HTTP client (or equivalent) unconditionally
accepts any server certificate via
`DangerousAcceptAnyServerCertificateValidator`, with no environment gating.
This should be restricted to development/preview scenarios (e.g. paired
with `@phoria/vite-plugin-dotnet-dev-certs`) and never active in
production.

**Labels:** `phase-2`, `server-robustness`, `security`
**Milestone:** v1

---

### Adopt `IMemoryPoolFactory<byte>` for `Phoria.IO.StreamPool`

**Body:** .NET 10 introduces `IMemoryPoolFactory<byte>` as a more modern
alternative to hand-rolled `RecyclableMemoryStream` pooling. `StreamPool`
currently exposes `RecyclableMemoryStream` publicly, and consumers rely on
`GetReadOnlySequence()`, an `IBufferWriter<byte>` cast, and `Stream`
semantics — none of which `MemoryPool<byte>` provides directly. This is a
**public-API refactor** of `Phoria.IO`, not a drop-in dependency bump;
scope it as a deliberate design task in Phase 2, alongside the other
`Phoria.IO`/server-robustness fixes above.

**Labels:** `phase-2`, `server-robustness`, `enhancement`
**Milestone:** v1

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
Harmless to omit today, but Phase 3 ("Vite bundling of .NET-referenced
static assets") will likely want name-based chunk lookup — add
`public string? Name { get; init; }` when that work starts. Flagged as
Task 7's hazard #2 in the original plan; deliberately deferred, not an
oversight.

**Labels:** `phase-3`, `enhancement`
**Milestone:** v1 (or re-scope to Phase 3 milestone if one exists)
