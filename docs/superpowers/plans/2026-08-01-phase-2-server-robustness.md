# Phase 2 — Server Robustness: Observations & Initial Scope

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Seed the Phase 2 ("Server robustness & production-readiness", per `docs/PROJECT.md`) implementation plan with the observations gathered while running the `e2e/framework-multiple` preview (a build-time CA1873 warning and a non-graceful Ctrl+C shutdown), and consolidate them with the already-tracked Phase 2 deferred issues so this document is the single scope reference for Phase 2.

**Architecture:** Two distinct shutdown paths are involved. (1) **e2e preview orchestration** — `preview` runs `run-p preview:* -c` (npm-run-all), which spawns the .NET web app and the Phoria Server (h3/node) side-by-side as siblings; neither owns the other, so graceful shutdown is an orchestration concern. (2) **Sidecar process ownership** — in Production (`appsettings.Production.json`) the .NET app *spawns* the Phoria Server via `PhoriaServerProcess`, so graceful shutdown of the node process is a .NET hosting concern. Both paths need hardening.

**Tech Stack:** C#/.NET 10 (`PhoriaServerProcess`, `PhoriaServerProcessService`, CliWrap), TypeScript (`e2e` server entries, h3/listhen), Node process orchestration (currently `npm-run-all@4.1.5`), OpenTelemetry logging (library choice TBD on both runtimes — see Task 7).

## Global Constraints

- This is a **capture/scoping document**, not the full Phase 2 implementation. The fixes below execute as Phase 2 work (after Phase 1 / CI tasks are complete); nothing here should be smuggled into dependency-upgrade or CI tasks.
- The known deferred Phase 2 issues (`Process.Kill()` process-tree bug,
  `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s,
  unconditional `DangerousAcceptAnyServerCertificateValidator`,
  `IMemoryPoolFactory<byte>` adoption) now live in this document's own
  "Known deferred issues" section (Task 5) — they were migrated out of
  `docs/deferred-issues-phase-1.md`, which is trimmed accordingly and retains
  only the entries unrelated to Phase 2 (dependency tracker, phase-1/phase-3
  code-quality follow-ups).
- `run-p` replacement is a **review** task: prefer a maintained library over a hand-rolled script; candidates must be evaluated, not assumed. `concurrently` is one candidate, not a decision.
- Repo style: Biome for TS/JSON/markdown (tabs, line width 120); tabs + 4-space indent for C#. `dotnet` build order: `build` → `lint` → `check` → `test`.
- CA1873 is enabled because `packages/Phoria/Phoria.csproj` sets `<AnalysisLevel>latest-recommended</AnalysisLevel>` (recommended rules, .NET 10).

---

### Task 1: Fix the CA1873 warning in `PhoriaServerProcess`

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs` (line 91)

**Interfaces:**
- Produces: Release builds of `packages/Phoria` compile without the CA1873 warning `PhoriaServerProcess.cs(91,62)`.

- [ ] **Step 1: Guard the expensive logging argument with `IsEnabled`**

`string.Join(" ", processOptions.Arguments ?? [])` is evaluated eagerly at the call site of the source-generated `[LoggerMessage]` method even when `Information` logging is disabled, which is what CA1873 flags (see <https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873>). The documented fix is to guard the call:

```csharp
if (logger.IsEnabled(LogLevel.Information))
{
    logger.LogServerProcessIsStarting(processOptions.Command, string.Join(" ", processOptions.Arguments ?? []));
}
```

- [ ] **Step 2: Verify the warning is gone**

Run: `dotnet build packages/Phoria/Phoria.csproj --configuration Release`
Expected: exit 0, and no `CA1873` warning in the output. Also confirm no other CA1873 warnings surface elsewhere in the package once this one is fixed.

---

### Task 2: Harden the Phoria Server (node) shutdown handler

**Files:**
- Modify: `e2e/framework-multiple/WebApp/ui/src/server.ts` (shutdown handler, lines 108-129)
- Modify: `e2e/with-workspace/WebApp/ui/src/server.ts` (same snippet)
- Modify: `docs/guides/getting-started.md` (the server script snippet, lines ~360-381)

**Interfaces:**
- Produces: `listener.close()` resolves promptly on SIGINT/SIGTERM instead of waiting up to the keep-alive timeout for the .NET app's pooled HTTP connection to close; the 5s force-exit fallback remains as a safety net.

- [ ] **Step 1: Close idle keep-alive connections before/while closing the listener**

listhen's `Listener` exposes the underlying Node server (`listener.server`, confirmed in `listhen/dist/shared/*.d.ts`), and Node ≥18.2 provides `closeIdleConnections()`. In `shutdown()`, drop idle connections so `close()` does not stall:

```ts
function shutdown(signal: NodeJS.Signals) {
  console.log(`Received signal ${signal}. Shutting down server.`)

  void listener.close().then(() => {
    console.log("Server listener closed.")
    process.exit(0)
  })

  // Drop idle keep-alive connections so close() doesn't wait for them
  listener.server.closeIdleConnections()

  // Force shutdown after 5 seconds
  setTimeout(() => {
    console.error("Could not shutdown gracefully. Forcefully shutting down server.")
    process.exit(1)
  }, 5000)
}
```

- [ ] **Step 2: Mirror the change in the second e2e app and the guide**

Apply the identical change to `e2e/with-workspace/WebApp/ui/src/server.ts` and to the server script in `docs/guides/getting-started.md` so end users get the hardened snippet.

- [ ] **Step 3: Verify**

Run: `pnpm --filter framework-multiple check` and `pnpm --filter with-workspace check` (or the equivalent `tsc`), plus `pnpm biome check --write` on the changed files. Expected: no type errors, no lint errors. Live signal behavior is verified in Task 6.

---

### Task 3: Review and replace `run-p` across the repo with a signal-forwarding library

**Background (root cause of the non-graceful preview shutdown):**

`npm-run-all@4.1.5` (pinned via `pnpm-workspace.yaml` catalog) registers **zero signal handlers** — a source grep for `signal`/`SIGINT`/`SIGTERM` across its entire `lib/` and `bin/` is empty. On Ctrl+C the kernel SIGINTs the whole foreground process group; `run-p` and pnpm die instantly *by signal*, so pnpm reports the intentional stop as `[ERR_PNPM_RECURSIVE_RUN_FIRST_FAIL] ... Command failed with signal "SIGINT"` + `[ELIFECYCLE] Command failed.` The two children do shut down gracefully themselves (`cross-env@10.1.0` forwards SIGINT to `dotnet` and exits 0 on SIGINT; the node server has SIGTERM/SIGINT handlers), but nothing remains to wait for them — the node shutdown log is lost on the broken stdout pipe, and the terminal hangs until the orphaned children exit (hence the second `^C^C`).

**Files:**
- Modify: `e2e/framework-multiple/package.json` (`build`, `preview` scripts)
- Modify: `e2e/with-workspace/WebApp/package.json` (`build`, `preview` scripts)
- Modify: `docs/guides/building-for-production.md` (documented `build`/`preview` script pattern)
- Modify: `pnpm-workspace.yaml` (catalog entry) — swap or co-exist depending on the chosen library
- Possibly modify: `docs/guides/getting-started.md` if it references the pattern

**Interfaces:**
- Produces: Ctrl+C on `pnpm --filter framework-multiple preview` (and the `build` scripts) forwards the signal to all children, waits for their graceful exit, and exits 0 — no `[ELIFECYCLE]` spam, no lingering processes.

- [ ] **Step 1: Evaluate candidates against the requirements**

Candidates (evaluate, don't assume): `concurrently` (`-k`/`--success` flags, well-tested signal forwarding), `npm-run-all` 6.x/forks, `lil-js/run`, `execa`-based orchestration. Requirements: (a) forwards SIGINT/SIGTERM to all children and waits for graceful exit; (b) exits 0 when the user interrupts; (c) platform-agnostic (Windows-safe, per the repo's `cross-env`/`npm-run-all` rationale); (d) maintained; (e) supports the `build:*` / `preview:*` glob patterns currently used. Record the chosen library and the rejected alternatives (with reasons) in this task's notes.

- [ ] **Step 2: Apply the replacement repo-wide**

Update both e2e `package.json` files' `build` and `preview` scripts, the `pnpm-workspace.yaml` catalog, and `docs/guides/building-for-production.md` (both the `build` and `preview` sections) to the chosen library. Run `pnpm install` to update the lockfile.

- [ ] **Step 3: Verify graceful shutdown live**

Run: `pnpm --filter framework-multiple preview`, wait for both servers ("Local: http://localhost:5173/" and the dotnet app running), press Ctrl+C.
Expected: both processes log a clean shutdown, the command exits 0, no `[ERR_PNPM_RECURSIVE_RUN_FIRST_FAIL]` / `[ELIFECYCLE]` output, and no second Ctrl+C is needed. Repeat for `pnpm --filter with-workspace ...` (run from its WebApp dir).

---

### Task 4: Library-level graceful process stop in `PhoriaServerProcess`

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs` (`StopServer()`, lines 138-165)
- Reference: `packages/Phoria/Server/PhoriaServerProcessService.cs` (line 12 TODO)
- Reference: `docs/deferred-issues-phase-1.md` → "`Process.Kill()` does not kill the entire process tree"

**Interfaces:**
- Produces: when the .NET host owns the Phoria Server process (Production sidecar model), shutdown sends a graceful signal first, waits a grace period, then force-kills the process tree — so the node server runs its SIGTERM handler instead of being SIGKILLed.

- [ ] **Step 1: Design the termination sequence**

`Process.Kill()` sends SIGKILL on Unix; the node server's graceful handlers never run. Proposed sequence for `StopServer()`:
1. Send SIGTERM to the process (via `kill -TERM <pid>` using CliWrap — already a dependency — or P/Invoke `kill(2)`). On Windows, `Process.Kill()` (TerminateProcess) is the only option and is acceptable.
2. Wait a grace period (align with the node server's 5s force-exit window, e.g. 5-6s) for the process to exit on its own.
3. Fallback: `Process.Kill(entireProcessTree: true)` so any children (e.g. esbuild in dev-mode vite) are also terminated — this resolves the deferred process-tree bug.

Record the design decision (kill command vs P/Invoke; grace-period value) and any .NET 10-specific API that supersedes it before implementing.

- [ ] **Step 2: Implement and unit-test**

Implement the sequence in `StopServer()` and add unit tests covering: process not running, process exits within grace period, process must be force-killed. `packages/Phoria.Tests` currently has no `PhoriaServerProcess` coverage — this is the first.

- [ ] **Step 3: Address the `PhoriaServerProcessService` debugger-stop TODO**

The TODO at `PhoriaServerProcessService.cs:12` ("Process is not being killed when stoppingToken is triggered — seems only a problem when stopping the debugger") should be revisited: stopping the debugger kills the host without running graceful shutdown, orphaning the node process. If still reproducible after Step 2, record a dedicated follow-up issue with repro steps.

---

### Task 5: Consolidate the known Phase 2 deferred issues into this doc

**Files:**
- Modify: this document (new "Known deferred issues" section)
- Reference: `docs/deferred-issues-phase-1.md`

- [x] **Step 1: Import the five deferred entries and trim the source**

Copy the five Phase 2 entries from `docs/deferred-issues-phase-1.md` (process-tree kill, `StartServer`/`StopServer` semaphore race, undisposed `StreamPool`s, unconditional `DangerousAcceptAnyServerCertificateValidator`, `IMemoryPoolFactory<byte>` adoption) into a "Known deferred issues" section here, with their labels/milestones, so this document is the single Phase 2 scope reference. Remove the "Phase 2 — Server Robustness" section from `docs/deferred-issues-phase-1.md` (superseded by this section, not duplicated) and replace it with a one-line pointer to this document. Leave the rest of that file untouched — the dependency-tracker entry and the phase-1/phase-3 code-quality follow-ups are unrelated to Phase 2 and stay there until filed as their own issues.

Done 2026-08-01 — see "Known deferred issues" section below; `docs/deferred-issues-phase-1.md` trimmed accordingly.

- [x] **Step 2: Cross-link this document**

Add a reference to this plan from `docs/PROJECT.md`'s Phase 2 bullet and from `docs/deferred-issues-phase-1.md` so both point here.

Done 2026-08-01 — both files updated.

## Known deferred issues

Migrated from `docs/deferred-issues-phase-1.md` (2026-08-01), where they were
recorded at Phase 1 close-out as explicitly out-of-scope for a
dependency-upgrade phase. `gh` was not available in that environment (or this
one), so none have been filed as real GitHub issues yet — each entry below is
meant to become one issue when `gh` access (or the GitHub UI) is available.

### `Process.Kill()` does not kill the entire process tree

**Body:** `PhoriaServerProcess` (or wherever the Node server process is
stopped) calls `Process.Kill()` without `entireProcessTree: true`. This is
suspected to be the root cause of the known server-process shutdown bug
described in `docs/PROJECT.md` (reproduces mainly when stopping the
debugger). Fix: pass `entireProcessTree: true` (or the .NET 10 equivalent)
so child processes spawned by the Node server are also terminated. See
Task 4 above, which implements this fix.

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

### Task 6: Live verification of the shutdown paths

**Files:**
- None (verification only)

- [ ] **Step 1: Reproduce the current shutdown behavior (baseline)**

Run: `pnpm --filter framework-multiple preview`; exercise the app; press Ctrl+C.
Expected (current, known-ugly): dotnet logs "Application is shutting down...", pnpm prints `[ERR_PNPM_RECURSIVE_RUN_FIRST_FAIL]` + `[ELIFECYCLE] Command failed`, and a second `^C^C` is required. Capture the node server's output — confirm whether `Received signal SIGINT. Shutting down server.` appears at all (it may be lost on the broken stdout pipe).

- [ ] **Step 2: Confirm the keep-alive `close()` hypothesis**

While the baseline is running, note whether node's shutdown message appears and how long the process lingers. This validates whether Task 2's `closeIdleConnections()` is the fix for the lingering, or whether the linger is purely the dead parent's broken pipe (in which case Task 3 is the fix). If the linger persists after Tasks 2+3, investigate the .NET → node HTTP keep-alive connection lifecycle.

- [ ] **Step 3: Re-verify after Tasks 2 and 3**

After each of Tasks 2 and 3, re-run the baseline reproduction and confirm the shutdown is clean (single Ctrl+C, clean exit, no error spam).

---

### Task 7: Add OpenTelemetry logging (.NET host + Node/Vite sidecar)

**Background:** Added to Phase 2 scope per `docs/PROJECT.md`'s "health/observability" bullet, now made concrete as OpenTelemetry logging. Confirmed starting point (author, 2026-08-01): the .NET side has **no existing logging configuration at all** — it just runs on whatever the default `ILogger` setup provides, with `ILogger<T>` injected and used wherever appropriate; there is nothing to migrate away from, only DI-time setup to add. The Node/h3 side similarly has nothing beyond framework built-ins and a handful of `console.log` calls (e.g. `e2e/*/WebApp/ui/src/server.ts`'s shutdown handler, see Task 2). Logging only for this task — traces/metrics are explicitly out of scope for now (see `docs/PROJECT.md` Open questions); no cross-runtime log correlation is attempted either, since that would require a tracing decision this milestone is deferring.

**Files:**
- TBD — the .NET composition-root/DI entry point(s) where `AddOpenTelemetry()`/`Extensions.Logging` would be wired have not yet been located; identify during Step 1.
- TBD — the Node/h3 entry point(s) (`e2e/*/WebApp/ui/src/server.ts` at minimum; possibly a shared helper if the pattern should live in `@phoria/phoria` itself rather than per-e2e-app).

**Interfaces:**
- Produces: server-process lifecycle events and errors, in both the .NET host and the Node/Vite sidecar, are emitted as structured OpenTelemetry logs.

- [ ] **Step 1: Design — evaluate library and exporter choices (do not assume)**

.NET candidates: `OpenTelemetry.Extensions.Logging` (bridges `ILogger`/`ILoggerProvider` into the OTel logging pipeline — likely the natural fit given `ILogger<T>` is already the call pattern everywhere) vs. hand-rolled OTel SDK usage. Node candidates: `@opentelemetry/api-logs` + `@opentelemetry/sdk-logs` (+ an appropriate exporter package), vs. a lighter pino/winston-with-OTel-transport approach. Exporter target is undecided — options include an OTLP endpoint (gRPC or HTTP) for real deployments and a console exporter for local/dev — record which is the default and whether it's configurable. Also decide: does this belong in `@phoria/phoria`/`Phoria` (the published packages) so consumers get it for free, or only in the `e2e` apps as a demonstration? Record the decision and rejected alternatives here before implementing.

- [ ] **Step 2: Implement .NET-side OpenTelemetry logging**

Wire up the chosen library at the DI root so existing `ILogger<T>` call sites (e.g. `PhoriaServerProcess`, `PhoriaServerProcessService`) start emitting via OpenTelemetry with no call-site changes required.

- [ ] **Step 3: Implement Node/h3-side OpenTelemetry logging**

Wire up the chosen library in the Node sidecar entry point(s), replacing the ad-hoc `console.log` calls identified above with structured OTel log emission.

- [ ] **Step 4: Verify**

Confirm logs are emitted and visible via the chosen exporter (e.g. console output, or a local OTLP collector) for: normal server start, normal graceful stop, and a forced/crash stop. Run `pnpm --filter <e2e-app> check` / `dotnet build` to confirm no regressions from the new dependency.
