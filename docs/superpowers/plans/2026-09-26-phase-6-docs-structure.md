# Phase 6 Docs Structure Implementation Plan (Plan A)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn nine empty placeholder guides into real content by extracting the prose that already exists in `getting-started.md`, give a trialist a three-command Docker path that is actually executed, and give the documentation-review convention a trigger so the rot that produced those placeholders does not recur.

**Architecture:** This is an extraction, not an invention. `docs/guides/getting-started.md` (598 lines) is already an ordered reader journey, and the eight empty guides are named after the concepts that journey walks through — with **seven live links already pointing into them** from inside instructions. The prose for most of those guides therefore already exists and is being *moved*, governed by a mechanical seam test (does this paragraph explain *this concept*, or *this step in this order*?). The slice also adds what genuinely does not exist yet: a trialist path requiring no local toolchain, a concept guide for islands, a framework-extension recipe, and the trigger that makes the next change local.

**Tech Stack:** pnpm 11 workspaces + Turborepo; Vite 8/Rolldown islands; Aspire AppHosts; Markdown; Docker Compose (examples on port 8080); Node.js v24.16.0, pnpm 11.17.0, Docker 29.8.0.

**Spec:** [`docs/superpowers/specs/2026-09-26-phase-6-docs-structure-design.md`](docs/superpowers/specs/2026-09-26-phase-6-docs-structure-design.md) — the plan argues from the spec, so the spec travels with it; executors read both. Its problem-space companion is [`2026-09-26-phase-6-docs-journey-design.md`](docs/superpowers/specs/2026-09-26-phase-6-docs-journey-design.md).

## Global Constraints

- **Prose and commit messages in British English** — `centralise`, not `centralize`. Historical `docs/MEMORY.md` entries that predate this are exempt and must not be rewritten.
- **Do not hard-wrap prose.** Every paragraph is one unbroken line; block elements (headings, tables, fences) and each list item — including continuation prose — get their own line. This is `AGENTS.md`'s `### Markdown & prose` rule and applies to every file this plan touches.
- **This slice writes no product code.** No `.ts`, `.cs`, `.csproj`, or `package.json` file is modified. All nine examples already carry a `docker-compose.yml`, a `Dockerfile` and a `.dockerignore` on port 8080, and `with-workspace` is not an exception — its Dockerfile is at `apps/WebApp/Dockerfile`, which is depth 3, and a `-maxdepth 2` check reports a false negative. When checking Docker parity across the two example layouts, do not bound the search depth.
- **A retained placeholder must name what the guide will cover, point to the nearest working alternative, and promise no date.** The current generic warning ("work in progress… please raise an issue") names none of those and is replaced everywhere it appears.
- **At most three commands, as one contiguous block, may be restated in `README.md`.** Anything longer is owned by exactly one guide. The trialist block qualifies at three commands including the teardown line.
- **The `#canary` aside appears in exactly two places** — the root `README.md` trialist block and the `## Catalog` of `examples/README.md`. Not in the nine per-example READMEs.
- **A package's documentation lives in its README, or in a `docs` folder inside the package** — one file per topic, linked from that README, not shipped in the published artefact. Never a single page placed ad hoc beside a source file.
- **Docs are written assuming the current branch is `main`.** An unqualified `giget` ref resolves the repository's default branch, and `examples/` is not on `main` until the `canary` → `main` cut — which is the final step of the overall task, not of this plan.
- **`gh` is unavailable in this environment.** The ten `giget` references cannot be verified against `main` from an agent session; that is step 6 of the stable-cut runbook, run by the maintainer performing the cut.

## Review Focus

The failure modes this spec implies but no automated check exercises, most likely to bite a reader first:

1. **Prose silently lost in a move.** Extraction under a mechanical test is exactly the operation where a paragraph gets dropped and nobody notices, because it is then absent from both the source and the destination. Expected: every non-empty line of a moved range appears verbatim in its destination — pinned mechanically by the `LOST` detector in Task 0 (Review Focus 3 says how, and every extraction task runs it).
2. **A link that points at a file whose content is still a placeholder.** Eight links from inside live instructions currently do this. Expected: after this plan, every relative link from `getting-started.md` and `README.md` resolves to a file containing no `work in progress` warning — pinned by the link check in Task 0 and re-run per task.
3. **A command that looks correct and is not.** The class of defect this slice exists to fix, and reading cannot catch it. Expected: the trialist path is *executed* end to end (Task 8), not read; every other document's commands are checked against the target `package.json` scripts.
4. **A guide index that advertises content which does not exist.** The current index is honest — it links only the five real guides — and that property is easy to lose while making it journey-shaped. Expected: the index links only guides that are non-placeholder when the slice lands, so `configuration.md`, `workspaces.md` and `supported-ui-frameworks.md` are absent from it — pinned by the Review Focus 2 check run against the index in Task 9.
5. **The declared overlap "fixed" by deleting one side.** `getting-started.md` keeps a minimal component and registration inline while `creating-phoria-island-components.md` keeps the full treatment including `PhoriaIslandComponentFactory`. This is intentional. Expected: both sides still present at the end — pinned by the explicit existence check in Task 5.

## Sequencing Decisions

Recorded here rather than appended to `docs/MEMORY.md`, because this planning session is not permitted to modify files outside `/home/meeg/.opencode/plan/`. **Task 12 appends these to `docs/MEMORY.md` as its first step.**

- **The trigger (Task 1) lands before everything else**, so the remaining ten tasks are performed under the mechanism this slice introduces. This is the spec's explicit instruction and it is cheap: the trigger is a convention, and the convention only bites if it exists while the work that could rot documentation is happening.
- **Tasks 2, 3 and 4 are sequential, not parallel.** They edit disjoint line ranges of `getting-started.md` but the *same file*, so they cannot be delegated concurrently without merge conflicts on every hunk. Their independence is real; their parallelism is not.
- **Task 7 depends on Task 4.** The `configuration` note at `getting-started.md:455` sits inside the 432-493 range that Task 4 extracts. Task 4 deliberately leaves that one note in place, and Task 7 moves it. If Task 4 swallowed it, Task 7 would have nothing to move and `configuration.md` would lose the only real content extraction can give it.
- **Task 9 depends on Tasks 2-6.** The journey index may only link guides that are non-placeholder when the slice lands, so it cannot be written until the extraction has decided which those are. Writing it earlier would mean either guessing or advertising dead ends — the exact failure the slice is fixing.
- **Task 11 depends on Task 10.** The framework recipe's link goes into `ARCHITECTURE.md`, so the recipe must exist before the link does.
- **The nine per-example `giget` references move from this plan to Plan B.** The spec assigns all ten references to Plan A, but nine of them live one-per-file in `examples/*/README.md`, which Plan B rewrites wholesale to add the uniform `## Try it` section — which is where the `giget` command belongs. Repairing them here would mean editing a line that Plan B then moves, which is the "two homes for one explanation" failure the seam rule exists to prevent. Net effect is identical: all ten references are repaired across the two plans. **This is a deliberate deviation from the spec's plan decomposition and is the one place this plan does not follow the spec literally.**

## File Structure

**Created — 10 concept guides, 1 package doc.** Each is a single-responsibility document in the register named by its title; the title is preserved from the existing placeholder file so no incoming link changes target.

| File | Register | Content source |
| --- | --- | --- |
| `docs/guides/phoria-server.md` | concept | moved from `getting-started.md:206-386` |
| `docs/guides/client-entry.md` | concept | moved from `getting-started.md:387-408` |
| `docs/guides/server-entry.md` | concept | moved from `getting-started.md:409-431` |
| `docs/guides/phoria-web-app.md` | concept | moved from `getting-started.md:432-493` |
| `docs/guides/component-register.md` | reference | de-duplicated from `getting-started.md:187` + `creating-phoria-island-components.md:53-88` |
| `docs/guides/phoria-islands.md` | concept | **new prose** (Task 6) |
| `docs/guides/configuration.md` | reference (incomplete) | `getting-started.md:455` + the incomplete-guide notice |
| `docs/guides/workspaces.md` | concept (incomplete) | `getting-started.md:204` + the `with-workspace` example |
| `docs/guides/supported-ui-frameworks.md` | reference (incomplete) | the 3 framework packages and their examples |
| `packages/phoria-islands/docs/framework-plugin.md` | contributor reference | **new prose** (Task 10) |

**Modified — 6 files.** `docs/guides/getting-started.md` (loses five ranges, gains the trialist route, keeps a documented overlap), `README.md` (trialist block replaces the inline pnpm commands; flat index becomes journey-shaped), `docs/guides/creating-phoria-island-components.md` (its register section yields to the canonical guide), `CONTRIBUTING.md` (trigger + runbook step 6), `AGENTS.md` (trigger + a phase-number fix), `docs/ARCHITECTURE.md` (recipe link, runbook step 6, drift pass).

**Modified outside the repository — 1 file.** The `writing-plans` skill at `/home/meeg/.local/share/opencode/packages/superpowers/skills/writing-plans/SKILL.md`. This is the record that actually makes the step appear in new plans, but it is a global tool installation: it is not in this repository, is not version-controlled with it, and will not appear in any commit. Task 1 Step 3 handles it and says so.

**Not modified.** The nine `examples/*/README.md` (Plan B). The 18 existing plan documents (historical records; the spec rules them out of scope). `docs/PROJECT.md`.

---

### Task 0: Verification helpers

Establishes the three mechanical checks every later task runs. Without these, the slice's largest risk — prose silently lost in a move — has no detector, and the plan would be asking for a read-through to catch something a read-through reliably misses.

**Files:**
- Create: `/tmp/opencode/ph6-loss-check.sh` (outside the repository; a helper, not a deliverable)

**Interfaces:**
- Consumes: nothing.
- Produces: two shell functions available to every later task by sourcing `/tmp/opencode/ph6-lib.sh`:
  - `loss_check <source-file> <dest-file> <first-line> <last-line>` — prints `LOST: <line>` for every non-empty line of the source range that does not appear verbatim in the destination. Exit 0 when nothing was lost.
  - `no_placeholder <file>...` — exits non-zero if any named file still contains `work in progress`. Exit 0 otherwise.

- [ ] **Step 1: Create the helper library**

```bash
mkdir -p /tmp/opencode
cat > /tmp/opencode/ph6-lib.sh <<'LIB'
# Phoria Phase 6 docs helpers. Source this: . /tmp/opencode/ph6-lib.sh

# loss_check <source-file> <dest-file> <first-line> <last-line>
# Prints "LOST: <line>" for every non-empty line of the source range absent
# from the destination. This is the detector for prose lost during extraction.
loss_check() {
  local src="$1" dest="$2" first="$3" last="$4" lost=0 line
  while IFS= read -r line; do
    [ -z "$line" ] && continue
    if ! grep -Fqx -- "$line" "$dest"; then
      printf 'LOST: %s\n' "$line"
      lost=1
    fi
  done < <(sed -n "${first},${last}p" "$src")
  return $lost
}

# no_placeholder <file>...  — non-zero if any file is still a placeholder.
no_placeholder() {
  local f rc=0
  for f in "$@"; do
    if grep -q "work in progress" "$f"; then
      printf 'STILL PLACEHOLDER: %s\n' "$f"
      rc=1
    fi
  done
  return $rc
}
LIB
```

- [ ] **Step 2: Prove the loss detector actually detects a loss**

A detector that has never fired is not a detector. Drop a known line and confirm the check reports it:

```bash
. /tmp/opencode/ph6-lib.sh
cp docs/guides/getting-started.md /tmp/opencode/gs-probe.md
sed -n '523,527p' docs/guides/getting-started.md   # the three Tag Helper bullets
```

Expected output — five lines, the three bullets and two blank-adjacent text lines:

```
* Serialising data passed inline or from the view model as `props` for the component
* Requesting an SSR response from the Phoria Server (if required, Islands can be CSR only)
* Hydrating the component on the client (if required, Islands can be SSR only)
```

Now build a destination missing the middle bullet and confirm the detector reports exactly that one line:

```bash
printf '# Probe\n\n* Serialising data passed inline or from the view model as `props` for the component\n* Hydrating the component on the client (if required, Islands can be SSR only)\n' > /tmp/opencode/gs-probe-dest.md
loss_check docs/guides/getting-started.md /tmp/opencode/gs-probe-dest.md 523 527
echo "exit: $?"
```

Expected output:

```
LOST: * Requesting an SSR response from the Phoria Server (if required, Islands can be CSR only)
exit: 1
```

- [ ] **Step 3: Prove the placeholder detector fires on a real placeholder**

```bash
. /tmp/opencode/ph6-lib.sh
no_placeholder docs/guides/phoria-server.md; echo "exit: $?"
```

Expected: `STILL PLACEHOLDER: docs/guides/phoria-server.md` and `exit: 1`.

- [ ] **Step 4: Commit**

Nothing to commit — `/tmp/opencode/` is outside the repository. Record instead that the library exists, so a fresh session can recreate it:

```bash
git status --short
```

Expected: clean. The three modified/untracked spec files from the scoping sessions are expected to appear (`docs/MEMORY.md` modified; the two spec files untracked) — leave them alone; they are the design record for this plan and Task 12 commits them.

---

### Task 1: The documentation-review trigger

Lands first, so every task after it is performed under the mechanism the slice introduces. One rule, recorded in three places.

**Files:**
- Modify: `CONTRIBUTING.md:88-90` (the `## Documentation` section)
- Modify: `AGENTS.md` (new `## Documentation review` section between `### Markdown & prose` and `## Gotchas`), and `AGENTS.md:144`
- Modify (outside the repository): `/home/meeg/.local/share/opencode/packages/superpowers/skills/writing-plans/SKILL.md`

**Interfaces:**
- Consumes: nothing.
- Produces: the phrase "documentation review" as the name of a mandatory, named plan task. Later plans — including Plan B — inherit it by being written under this convention.

- [ ] **Step 1: Rewrite `CONTRIBUTING.md`'s `## Documentation` section**

Replace the passive wording ("should review whether…") with the requirement. Current text at `:88-90`:

```markdown
Contributions that change how Phoria works should review whether the README files or [`docs/guides/`](docs/guides/) need to reflect the change, and update them as part of the contribution. Substantial changes should also keep [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) consistent with the code.
```

Replace the whole section with:

```markdown
## Documentation

Contributions that change how Phoria works must update the documentation that describes them, in the same contribution. Review the README files and [`docs/guides/`](docs/guides/); where a change alters how Phoria's pieces cooperate, [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) must match the code.

**Every plan carries a documentation review that names the documents it checked.** State which documents were read and what was confirmed about each — the guide documenting a changed export, the package README whose install command or version range moved, the example README whose commands changed. Where a change altered no public surface, say so and give the reason; the entry still exists. A plan without a named documentation review is incomplete.
```

- [ ] **Step 2: Add the requirement to `AGENTS.md`, and fix the phase number**

Insert between the end of `### Markdown & prose` (`:135`) and `## Gotchas` (`:137`):

```markdown
## Documentation review

- **Every plan must contain a task named "Documentation review" that lists the documents it checked.** A plan without one is incomplete, and the set of plans can be searched for it.
- **Name the documents, and say what was confirmed about each** — not "docs updated". The guide that documents a changed export, the package README whose install command or version range moved, the example README whose commands changed.
- **Where a change altered no public surface, the task still exists** and states that, with the reason. There is no "not applicable" exemption; the entry is what makes "names the documents" coherent when the list is empty.
- **A document lives in the guides or with its package.** Prose explaining how Phoria's pieces cooperate stays whole in [`docs/guides/`](docs/guides/); a package's own public surface is documented in that package's README or its `docs` folder, never as a page placed beside a source file.
```

Then fix a known drift in the same file. `AGENTS.md:144` reads:

```markdown
... reconcile to `^1.0.0` when all packages reach `1.0.0` (Phase 10).
```

`docs/PROJECT.md:206` is `11. **Release prep** — version reconciliation to `1.0.0`, docs pass, changesets,` — the `1.0.0` reconciliation is Phase 11, not Phase 10 (Phase 10 is "Examples"). Change `(Phase 10)` to `(Phase 11)`.

- [ ] **Step 3: Update the `writing-plans` skill so the step appears in new plans**

This is the record that actually makes the requirement visible to plan authors. **It lives in a global tool installation, not in this repository** — it is not version-controlled here, will not appear in any commit, and must be reported to the user as a local-only change.

In `/home/meeg/.local/share/opencode/packages/superpowers/skills/writing-plans/SKILL.md`, add to the `## Plan Document Header` template, after the `**Spec:**` block and before `## Global Constraints`:

```markdown
## Documentation Review

[The documents this plan read and what was confirmed about each — the guide
documenting a changed export, the README whose command or version range moved.
Where no public surface changed, state that and give the reason. This section
is mandatory; a plan without it is incomplete. Required by CONTRIBUTING.md
`## Documentation`.]
```

And add a matching step to the `## Self-Review` checklist as item 0:

```markdown
**0. Documentation review:** does the plan carry a `## Documentation Review`
section naming the documents it checked? If the work changed no public
surface, does the section say so and give the reason?
```

If that file does not exist on this machine, skip the step and record in the task's commit message that the global convention could not be updated — the two in-repository records (`CONTRIBUTING.md`, `AGENTS.md`) still stand on their own.

- [ ] **Step 4: Verify both repository records name the requirement**

```bash
grep -ni "documentation review" CONTRIBUTING.md AGENTS.md
```

The search is case-insensitive because the phrase appears in both casings — `## Documentation review` as a heading and "documentation review" in prose. Expected: `CONTRIBUTING.md` has **2** hits (the bolded requirement and the closing "A plan without a named documentation review is incomplete"), and `AGENTS.md` has **2** (the `## Documentation review` heading and the first bullet's `"Documentation review"`). A zero count means the edit did not land in the file you think it did — check which.

- [ ] **Step 5: Confirm the phase fix landed and the file is otherwise intact**

```bash
grep -n "Phase 1[01]" AGENTS.md
git diff --stat AGENTS.md CONTRIBUTING.md
```

Expected: the peer-dependency bullet now reads `(Phase 11)`, the `docs/PROJECT.md Phase 4` reference at `:152` is untouched, and `git diff --stat` shows two files changed with no deletions beyond the replaced `## Documentation` paragraph.

- [ ] **Step 6: Commit**

```bash
git add CONTRIBUTING.md AGENTS.md
git commit -m "docs: make the documentation review a named requirement in every plan"
```

---

### Task 2: Extract the Phoria Server concept

The largest single move: 180 lines out of the journey and into `phoria-server.md`. It is first among the extractions because it is the biggest and therefore the one most likely to be done badly.

**Files:**
- Modify: `docs/guides/getting-started.md:206-386` (the `### Add Phoria Server` section, 181 lines including its heading)
- Modify: `docs/guides/phoria-server.md` (4 lines, currently a title and a placeholder warning)

**Interfaces:**
- Consumes: `loss_check` and `no_placeholder` from Task 0.
- Produces: `docs/guides/phoria-server.md` as a concept guide. The journey's `### Add Phoria Server` heading survives, its prose does not; a link replaces it. No later task links to this file except Task 9's index.

- [ ] **Step 1: Record the source range before touching anything**

```bash
. /tmp/opencode/ph6-lib.sh
sed -n '206,386p' docs/guides/getting-started.md > /tmp/opencode/ph6-phoria-server.md
grep -c '' /tmp/opencode/ph6-phoria-server.md
```

Expected: `181`. That number is the loss detector's denominator; if it changes during this task, the source range moved and the line numbers in later steps are stale.

- [ ] **Step 2: Read the range and apply the seam test to each paragraph**

Read `/tmp/opencode/ph6-phoria-server.md` end to end. For each paragraph ask the mechanical question: **does this explain *the Phoria Server concept*, or *this step in this order*?**

- Concept → `phoria-server.md`.
- Step-in-order (the commands to paste, in sequence) → stays in the journey.

The section is expected to be almost entirely concept — what the server is, that it is supervised, how health is reported, how it degrades — with the shell commands to add and run it remaining in the journey. Judge each paragraph; do not assume a split point at a heading.

- [ ] **Step 3: Write `docs/guides/phoria-server.md`**

Keep the existing `# Phoria Server` title so the incoming link target is unchanged. Replace the placeholder warning with the paragraphs Step 2 classified as concept, moved across **verbatim and in their original order** — do not rewrite, re-word or reorder them, because Step 4's loss detector matches whole lines and any edit to a moved line shows up as a false loss. Then close with:

```markdown
## Related

- [Client Entry](./client-entry.md) and [Server Entry](./server-entry.md) — the two entry points the server renders through.
- [Phoria Web App](./phoria-web-app.md) — the .NET app that starts and proxies to the server.
- [Phoria Islands](./phoria-islands.md) — what the server renders.
```

The file is a cut range plus those three links. The guide needs no `##` sections of its own beyond `## Related` unless Step 2's classification produced a natural break — if it did not, do not invent one.

If Tasks 3, 4 or 6 have not yet run, those three targets are still placeholders; that is expected and does not block this task. Task 9's check confirms every one is real before the slice lands.

- [ ] **Step 4: Prove nothing was lost**

```bash
. /tmp/opencode/ph6-lib.sh
loss_check docs/guides/getting-started.md docs/guides/phoria-server.md 206 386
echo "exit: $?"
```

Expected: no output, `exit: 0`. Any `LOST:` line is content that was neither kept in the journey nor moved to the guide — restore it to one of the two, then re-run.

- [ ] **Step 5: Replace the journey section with a link**

In `docs/guides/getting-started.md`, keep the `### Add Phoria Server` heading and its runnable commands; replace the conceptual prose with a pointer in the same shape as the existing entry-point steps at `:389` and `:411`:

```markdown
### Add Phoria Server

The [Phoria Server](./phoria-server.md) is the Node.js sidecar that renders your Phoria Island components on the server. Add it and wire it up as follows.
```

- [ ] **Step 6: Verify the guide is no longer a placeholder and its links resolve**

```bash
. /tmp/opencode/ph6-lib.sh
no_placeholder docs/guides/phoria-server.md; echo "exit: $?"
```

Expected: no output, `exit: 0`.

- [ ] **Step 7: Commit**

```bash
git add docs/guides/getting-started.md docs/guides/phoria-server.md
git commit -m "docs: extract the Phoria Server concept from the getting-started journey"
```

---

### Task 3: Extract the Client Entry and Server Entry concepts

Two adjacent ranges, 387-408 and 409-431, producing two guides. The journey already summarises each in one sentence and then links to an empty file — the summary is the concept, so the move is mostly a relocation of what follows.

**Files:**
- Modify: `docs/guides/getting-started.md:387-431` (the `### Add Client Entry` and `### Add Server Entry` sections)
- Modify: `docs/guides/client-entry.md` (4 lines, placeholder)
- Modify: `docs/guides/server-entry.md` (4 lines, placeholder)

**Interfaces:**
- Consumes: `loss_check`, `no_placeholder` from Task 0.
- Produces: `docs/guides/client-entry.md` and `docs/guides/server-entry.md` as concept guides. Task 9's index links both. `phoria-server.md`'s `## Related` section (Task 2) links both.

- [ ] **Step 1: Record both source ranges**

```bash
. /tmp/opencode/ph6-lib.sh
sed -n '387,408p' docs/guides/getting-started.md > /tmp/opencode/ph6-client-entry.md
sed -n '409,431p' docs/guides/getting-started.md > /tmp/opencode/ph6-server-entry.md
grep -c '' /tmp/opencode/ph6-client-entry.md /tmp/opencode/ph6-server-entry.md
```

Expected: `22` and `23`. Task 2 has already changed the line count *after* 386 only if it edited anything at or before 386 — it does not, so these ranges are still valid. If Task 2's diff touched lines before 387, re-derive both ranges from the `### Add Client Entry` and `### Add Server Entry` headings before continuing.

- [ ] **Step 2: Write `docs/guides/client-entry.md`**

Keep the `# Client Entry` title. The journey's own one-sentence summary at `:389` is the concept and belongs here; the code the reader pastes stays in the journey. The file should end:

```markdown
## Related

- [Server Entry](./server-entry.md) — the other half; together they are the two entry points Phoria renders through.
- [Phoria Island directives](./directives.md) — what the client does once an island hydrates.
- [Phoria Islands](./phoria-islands.md) — what a Client Entry hydrates.
```

- [ ] **Step 3: Write `docs/guides/server-entry.md`**

Keep the `# Server Entry` title. The summary at `:411` is the concept; the code stays in the journey. End with:

```markdown
## Related

- [Client Entry](./client-entry.md) — the other half.
- [Phoria Server](./phoria-server.md) — the process this entry runs inside.
- [Phoria Islands](./phoria-islands.md) — what a Server Entry renders.
```

- [ ] **Step 4: Prove nothing was lost, in both directions**

```bash
. /tmp/opencode/ph6-lib.sh
loss_check docs/guides/getting-started.md docs/guides/client-entry.md 387 408; echo "client-entry exit: $?"
loss_check docs/guides/getting-started.md docs/guides/server-entry.md 409 431; echo "server-entry exit: $?"
```

Expected: no output from either, both `exit: 0`.

- [ ] **Step 5: Reduce both journey sections to their summary plus a live link**

The journey keeps the `### Add Client Entry` and `### Add Server Entry` headings and their code. Replace each conceptual block with the existing one-sentence summary, which already links correctly — `:389` reads `The [Client Entry](./client-entry.md) is the entrypoint for the browser…` and that link now resolves to real content. The only edit needed is the loss detector's complement: if a concept paragraph was moved out, the summary must now carry the whole concept at the journey level.

- [ ] **Step 6: Verify both guides and their incoming links**

```bash
. /tmp/opencode/ph6-lib.sh
no_placeholder docs/guides/client-entry.md docs/guides/server-entry.md; echo "exit: $?"
grep -n "](\./client-entry.md)\|](\./server-entry.md)" docs/guides/getting-started.md
```

Expected: `exit: 0`, and hits at the `### Add Client Entry` and `### Add Server Entry` sections.

- [ ] **Step 7: Commit**

```bash
git add docs/guides/getting-started.md docs/guides/client-entry.md docs/guides/server-entry.md
git commit -m "docs: extract the Client Entry and Server Entry concepts from the journey"
```

---

### Task 4: Extract the Phoria Web App concept

Fixes the sharpest defect in the slice: `getting-started.md:434` currently *defines* the term "Phoria Web App" by linking to an empty file. The definition moves into the guide; the journey links to it.

**Files:**
- Modify: `docs/guides/getting-started.md:432-493` (the `### Add Phoria to the dotnet web app` section)
- Modify: `docs/guides/phoria-web-app.md` (4 lines, placeholder)

**Interfaces:**
- Consumes: `loss_check`, `no_placeholder` from Task 0.
- Produces: `docs/guides/phoria-web-app.md` as a concept guide. **One line is deliberately left behind** — the configuration note at `:455` — and Task 7 moves it into `configuration.md`.
- **Consumed by:** Task 7, which depends on this task leaving `:455` intact.

- [ ] **Step 1: Record the source range and isolate the line that stays**

```bash
. /tmp/opencode/ph6-lib.sh
sed -n '432,493p' docs/guides/getting-started.md > /tmp/opencode/ph6-phoria-web-app.md
grep -c '' /tmp/opencode/ph6-phoria-web-app.md
sed -n '455p' docs/guides/getting-started.md
```

Expected: `62`, and:

```markdown
> Any [configuration](./configuration.md) option that is not set explicitly will fallback to a default value except for `entry` and `ssrEntry` because there are no sensible defaults for these options.
```

That note explains a **configuration** concept, so under the seam test it does not belong in the Phoria Web App guide. It stays in the journey for now; Task 7 moves it.

- [ ] **Step 2: Write `docs/guides/phoria-web-app.md`**

Keep the `# Phoria Web App` title. Carry the definition from `:434` — that a Phoria Web App is "just a way of saying a dotnet web app with the `Phoria` NuGet package installed and configured" — plus the conceptual material from the range. **Do not carry the `:455` configuration note.** End with:

```markdown
## Related

- [Phoria Server](./phoria-server.md) — the sidecar the Phoria Web App starts and proxies to.
- [Configuration](./configuration.md) — the options this app is configured with. Incomplete at the time of writing.
- [Phoria Islands](./phoria-islands.md) — what the Tag Helpers in this app render.
```

- [ ] **Step 3: Prove nothing was lost, allowing for the one deliberate exception**

```bash
. /tmp/opencode/ph6-lib.sh
loss_check docs/guides/getting-started.md docs/guides/phoria-web-app.md 432 493
echo "exit: $?"
```

Expected: exactly one `LOST:` line, the `:455` configuration note. Confirm that is the only one:

```bash
. /tmp/opencode/ph6-lib.sh
loss_check docs/guides/getting-started.md docs/guides/phoria-web-app.md 432 493 2>&1 | grep -c '^LOST:'
```

Expected: `1`. Any other count means a paragraph was dropped by accident — restore it, then re-run. After Task 7 this check is re-run and must report `0`, because by then the note has moved into `configuration.md`.

- [ ] **Step 4: Reduce the journey section to a live link**

`getting-started.md:434` currently reads:

```markdown
Now you can add Phoria to your dotnet web app. From this point on we will refer to it as the [Phoria Web App](./phoria-web-app.md), which is just a way of saying a dotnet web app with the `Phoria` NuGet package installed and configured.
```

Reduce it to the ordered step, keeping the commands:

```markdown
Now you can add Phoria to your dotnet web app. From this point on we will refer to it as the [Phoria Web App](./phoria-web-app.md).
```

The link now resolves, and the definition has one home.

- [ ] **Step 5: Verify the guide and confirm the `:455` note is still in the journey**

```bash
. /tmp/opencode/ph6-lib.sh
no_placeholder docs/guides/phoria-web-app.md; echo "exit: $?"
grep -n "fallback to a default value except for" docs/guides/getting-started.md
```

Expected: `exit: 0`, and one hit — the note is still there for Task 7 to move.

- [ ] **Step 6: Commit**

```bash
git add docs/guides/getting-started.md docs/guides/phoria-web-app.md
git commit -m "docs: extract the Phoria Web App concept and resolve the term's dead link"
```

---

### Task 5: Canonicalise the component register

The one case where two copies of the same explanation already exist. Unlike Tasks 2-4 this is a de-duplication, not a move: one copy becomes canonical and the other two link to it.

**Files:**
- Modify: `docs/guides/component-register.md` (4 lines, placeholder) — becomes canonical
- Modify: `docs/guides/creating-phoria-island-components.md:53-88` (the `## Register the UI component` section) — yields to the canonical guide
- Modify: `docs/guides/getting-started.md:187` (one sentence plus a code block) — keeps a minimal inline registration, links onward

**Interfaces:**
- Consumes: `loss_check`, `no_placeholder` from Task 0.
- Produces: `docs/guides/component-register.md` as the reference for registration. The `## Related` sections written in Tasks 2-4 may link to it; Task 9's index links it.

- [ ] **Step 1: Read both existing copies**

```bash
sed -n '183,196p' docs/guides/getting-started.md
echo "======================================"
sed -n '53,88p' docs/guides/creating-phoria-island-components.md
```

Read `creating-phoria-island-components.md` in full first. Its later sections (`## Render the UI component with the PhoriaIslandTagHelper` at `:89` and `## Render the UI component with the PhoriaIslandComponentFactory` at `:108`) are **not** registration and must not move — see Step 5.

- [ ] **Step 2: Write the canonical `docs/guides/component-register.md`**

Keep the `# Component Register` title. It must cover, in this order:

1. What the register is — a module Phoria imports at startup to learn which components exist and how to render each one.
2. The shape of the file — the existing code block from `getting-started.md:189` onwards, verbatim.
3. The key/renderer pairing, and that the key is what the `component` attribute of `<phoria-island>` matches.
4. Multi-framework registers — what a register looks like when more than one framework is installed, since `creating-phoria-island-components.md:53-88` covers the React case and `examples/framework-multiple` covers the mixed case.

Ground every claim in the two source ranges. Do not add a registration option the code does not have; if a source range is ambiguous about what a field does, read the code (`packages/phoria-islands/src/client/` and the framework packages' CSR/SSR services) before writing the sentence.

Close with:

```markdown
## Related

- [Creating Phoria Island components](./creating-phoria-island-components.md) — the full walkthrough, including the Tag Helper and component factory.
- [Phoria Islands](./phoria-islands.md) — what you are registering.
```

- [ ] **Step 3: Prove the React-case content survived**

```bash
. /tmp/opencode/ph6-lib.sh
loss_check docs/guides/creating-phoria-island-components.md docs/guides/component-register.md 53 88
echo "exit: $?"
```

Expected: no output, `exit: 0`. Lines legitimately not carried across — the section's own heading and any pointer prose Task 5 Step 5 replaces — must be moved rather than deleted, so the detector should report nothing. If a specific line is intentionally not carried, delete it from `creating-phoria-island-components.md` in the same step that explains why, and re-run to confirm the count matches that one line.

- [ ] **Step 4: Replace the `creating-phoria-island-components.md` section with a link**

`## Register the UI component` at `:53` becomes:

```markdown
## Register the UI component

Register the component so Phoria knows where to find it and how to render it. The register's shape, its key/renderer pairing, and multi-framework registers are covered in the [component register guide](./component-register.md); the code for this guide follows.
```

Keep the code block that follows, so the walkthrough stays runnable. Sections `:89` and `:108` are untouched.

- [ ] **Step 5: Preserve the declared overlap in `getting-started.md`**

The journey keeps a minimal inline registration — the reader must be able to complete the ordered path without leaving it. `getting-started.md:187` reads:

```markdown
You will also need to [register the component](./component-register.md) so that Phoria knows where to find it and how to render it. Create the file `WebApp/ui/src/components/register.ts`:
```

That sentence and the code block after it **stay**. Only append a pointer to the full treatment:

```markdown
You will also need to [register the component](./component-register.md) so that Phoria knows where to find it and how to render it. Create the file `WebApp/ui/src/components/register.ts`:
```

followed by the existing code block, then:

```markdown
> The [component register guide](./component-register.md) covers multi-framework registers and the full shape of this file.
```

**This overlap is intentional.** The journey needs runnable code at the point of use; the guide needs to be complete. It is declared in the spec so it does not later read as an accident and get "fixed" by deleting one side.

- [ ] **Step 6: Verify both sides of the declared overlap survive**

```bash
. /tmp/opencode/ph6-lib.sh
no_placeholder docs/guides/component-register.md; echo "exit: $?"
grep -c "register.ts" docs/guides/getting-started.md docs/guides/creating-phoria-island-components.md
grep -n "PhoriaIslandComponentFactory" docs/guides/creating-phoria-island-components.md | head -2
```

Expected: `exit: 0`; at least one `register.ts` hit in each file; and `PhoriaIslandComponentFactory` still present in `creating-phoria-island-components.md` (Review Focus 5 — its deletion would be the "fix" this overlap must not receive).

- [ ] **Step 7: Commit**

```bash
git add docs/guides/component-register.md docs/guides/creating-phoria-island-components.md docs/guides/getting-started.md
git commit -m "docs: make the component register guide canonical and link the two existing copies to it"
```

---

### Task 6: Write the Phoria Islands concept

The only guide with no source prose anywhere. Everything else is moved; this is written.

**Files:**
- Modify: `docs/guides/phoria-islands.md` (4 lines, placeholder)

**Interfaces:**
- Consumes: nothing. Every other extraction task already links to this file; it does not depend on them.
- Produces: `docs/guides/phoria-islands.md` as the framework-agnostic concept register. Task 9's index links it.

- [ ] **Step 1: Read the three sources that define the concept**

```bash
sed -n '521,543p' docs/guides/getting-started.md
echo "======================================"
sed -n '458,470p' docs/ARCHITECTURE.md
echo "======================================"
sed -n '1,20p' docs/guides/directives.md
```

- [ ] **Step 2: Confirm the CSR-only and SSR-only claims against the code before writing them**

`getting-started.md:525-527` asserts an island "can be CSR only" and can be "SSR only". That is the load-bearing claim in the new prose, and it comes from a guide, not from code:

```bash
grep -rn "client" packages/Phoria/Islands/PhoriaIslandHtmlContent.cs | head -20
```

Confirm what happens when the `client` attribute is absent, and what happens when SSR is declined. If the code does not support one of the two, write only what it supports.

- [ ] **Step 3: Write the guide**

Keep the `# Phoria Islands` title. Write it at the **concept** register — framework-agnostic, no React sample code, no framework API. Content:

```markdown
# Phoria Islands

A Phoria Island is a UI component that your .NET application hands to Phoria to render. Phoria renders it on the server through the Phoria Server, returns the markup inside the HTTP response, and then hydrates that same markup in the browser so the component becomes interactive without a page reload.

## What the Tag Helper does for you

The `Phoria` NuGet package ships a Tag Helper that renders an island. Given a component name, some props and a client directive, it handles the three things that would otherwise be repeated at every render site:

* **Serialising props.** Data passed inline or from the view model is serialised and handed to the component as `props`. It can come from any source in your .NET application.
* **Requesting server-rendered markup.** The Phoria Server renders the component using the SSR strategy of the framework that registered it.
* **Hydrating in the browser.** The client entry hydrates the rendered markup using the CSR strategy of that framework, when the island is configured to.

## Server-rendering, client-rendering, or both

The two halves are independent, and an island may use either or both.

* **Both** is the default shape: markup arrives in the response and the component hydrates in the browser. This is what the getting-started path builds.
* **Server-rendering only** — the component renders its markup and never hydrates. Use it for content that is displayed but not interactive, so no JavaScript is shipped for it.
* **Client-rendering only** — the component ships no server markup and mounts itself in the browser. Use it for a component that depends on browser APIs unavailable at render time.

Which half runs is a property of the island's configuration: the [client directives](./directives.md) choose when a client-rendering island hydrates, and the framework's SSR service chooses how server markup is produced.

## What an island is not

An island is not a page and not a separate application. It is a component rendered inside your existing .NET page, and everything around it — layout, navigation, forms — stays ordinary .NET. Phoria's job is the boundary between the .NET application and the component, not replacing either.

## Related

- [Creating Phoria Island components](./creating-phoria-island-components.md) — the walkthrough, in React, for writing one and rendering it from .NET.
- [Phoria Island directives](./directives.md) — the reference for the client directives that control hydration.
- [Component register](./component-register.md) — how a component becomes something Phoria can render.
- [Client Entry](./client-entry.md) and [Server Entry](./server-entry.md) — the two entry points the two halves run through.
```

- [ ] **Step 4: Verify every link in the new guide resolves to a real file**

```bash
. /tmp/opencode/ph6-lib.sh
for f in creating-phoria-island-components directives component-register client-entry server-entry; do
  printf '%-36s ' "$f"
  if [ -f "docs/guides/$f.md" ] && ! grep -q "work in progress" "docs/guides/$f.md"; then echo OK; else echo "NOT READY"; fi
done
```

Expected: `OK` for all five. `directives.md` is real today; the other four are ready only if Tasks 3 and 5 have run. If any reports `NOT READY`, that is ordering, not a defect — Task 9's check is the gate that all are real before the slice lands.

- [ ] **Step 5: Verify the guide is no longer a placeholder**

```bash
. /tmp/opencode/ph6-lib.sh
no_placeholder docs/guides/phoria-islands.md; echo "exit: $?"
```

Expected: `exit: 0`.

- [ ] **Step 6: Commit**

```bash
git add docs/guides/phoria-islands.md
git commit -m "docs: write the Phoria Islands concept guide"
```

---

### Task 7: The three guides that stay incomplete

`configuration.md`, `workspaces.md` and `supported-ui-frameworks.md` are **not** completed by this slice — a complete configuration reference is a lookup table, and extraction only carries what the journey happens to touch. What changes is what they say: each names what it will cover and points to the nearest working alternative, and none promises a date.

**Files:**
- Modify: `docs/guides/getting-started.md:204` (the aside naming `configuration.md` and `workspaces.md`) and `:455` (the configuration note Task 4 left behind)
- Modify: `docs/guides/configuration.md` (4 lines, placeholder)
- Modify: `docs/guides/workspaces.md` (4 lines, placeholder)
- Modify: `docs/guides/supported-ui-frameworks.md` (25 lines: a title, a three-item list, a note, and three per-framework warnings)

**Interfaces:**
- Consumes: Task 4 (which left `:455` in place). `no_placeholder` from Task 0 — but note `no_placeholder` cannot be used as a pass condition for these three files, because they legitimately keep an "incomplete" notice. Their check is that the notice has the required *shape*, below.
- Produces: the incomplete-guide notice as a reusable three-part form. Task 9 uses knowledge of which guides are incomplete to decide what the index may link.

- [ ] **Step 1: Move the `:455` configuration note into `configuration.md`**

The note is a configuration concept that `getting-started.md:455` currently carries as an aside. Move it, and cut it from the journey:

```bash
. /tmp/opencode/ph6-lib.sh
sed -n '455p' docs/guides/getting-started.md
```

In `docs/guides/getting-started.md`, delete that `> [!NOTE]` block. In `docs/guides/configuration.md`, keep the `# Configuration` title, add the moved note, and add the notice:

```markdown
# Configuration

> [!NOTE]
> This guide is incomplete. It will cover every `PhoriaOptions` setting and its default value. For now, [Getting started](./getting-started.md) covers the options the first-run path uses, and the defaults for the rest are documented at the declaration site in `packages/Phoria/PhoriaOptions.cs`.

Any configuration option that is not set explicitly falls back to a default value, except for `entry` and `ssrEntry`, because there are no sensible defaults for those options.
```

- [ ] **Step 2: Re-run Task 4's loss check — it must now report zero**

```bash
. /tmp/opencode/ph6-lib.sh
loss_check docs/guides/getting-started.md docs/guides/phoria-web-app.md 432 493
echo "exit: $?"
```

Expected: no output, `exit: 0`. The one line Task 4 deliberately left behind is now accounted for. **If this reports a `LOST:` line, Task 4 was run against different line numbers than the file now has** — re-derive the range from the `### Add Phoria to the dotnet web app` heading before concluding anything is lost.

- [ ] **Step 3: Write `docs/guides/workspaces.md`**

`getting-started.md:204` mentions workspaces in an aside and links here. That aside stays in the journey — it is step-in-order context about the `WebApp/ui` folder — and the guide carries the concept:

```markdown
# Workspaces

> [!NOTE]
> This guide is incomplete. It will cover monorepo and pnpm workspace layouts, and how Phoria resolves island components that live in a workspace package. For now, the [`with-workspace`](../../examples/with-workspace) example is a complete workspace layout that installs, builds and runs standalone.

Phoria supports pnpm workspaces, which can simplify configuration and may be a better fit for a typical .NET solution or project directory structure than putting all UI code under the web app.

When an island component is imported directly from a workspace package rather than from an ordinary dependency, the framework plugin needs to know: that package's modules must be transformed the same way application-owned components are. Pass those package names as `workspacePackages` when configuring the framework plugin. Ordinary dependencies are excluded and need no configuration.
```

- [ ] **Step 4: Give `supported-ui-frameworks.md` real per-framework sections**

The file already has a title, a React/Svelte/Vue list and a note about future support — keep all three. Replace the three placeholder warnings with a short section each. Each section answers *"what does this package do"* and links the example for *"show me it working"*, per the placement rule:

```markdown
## React

`@phoria/phoria-react` provides the React CSR and SSR services and the React Vite plugin. Wrap `react()` from `@vitejs/plugin-react`, configure `createPhoriaFrameworkPlugin` with React's component filter, and register the framework in both your client and server entries.

See the [`framework-react`](../../examples/framework-react) example for a React-only island, or [`framework-multiple`](../../examples/framework-multiple) for React alongside Svelte and Vue.

## Svelte

`@phoria/phoria-svelte` provides the Svelte CSR and SSR services and the Svelte Vite plugin. It wraps `svelte()` from `@sveltejs/vite-plugin-svelte` and configures the shared factory with Svelte's component filter.

Svelte renders with `renderComponentToString` only, and its plugin additionally externalises `svelte` itself so the Vite-transformed component and the renderer share one `svelte/internal/server` instance.

See the [`framework-svelte`](../../examples/framework-svelte) example, or [`framework-multiple`](../../examples/framework-multiple) for all three frameworks together.

## Vue

`@phoria/phoria-vue` provides the Vue CSR and SSR services and the Vue Vite plugin. It wraps `vue()` from `@vitejs/plugin-vue` with `vue: false` so the framework's own JSX handling does not conflict, and configures the shared factory with Vue's component filter.

Vue mounts with `createApp().mount` — it has no explicit hydrate/render distinction.

See the [`framework-vue`](../../examples/framework-vue) example, or [`framework-multiple`](../../examples/framework-multiple) for all three frameworks together.
```

Add an incomplete notice under the title, since the per-package detail is thin until the package READMEs exist:

```markdown
> [!NOTE]
> This guide is incomplete. It will cover the full API of each framework package. For now, each section links the package's example, which is the fastest way to see a framework working end to end.
```

- [ ] **Step 5: Verify all three notices have the required shape**

```bash
. /tmp/opencode/ph6-lib.sh
for f in configuration workspaces supported-ui-frameworks; do
  printf '=== %s ===\n' "$f"
  grep -c "This guide is incomplete" "docs/guides/$f.md"
  grep -o "](\.\./\.\./examples/[a-z-]*)" "docs/guides/$f.md" | head -2
done
```

Expected: `configuration.md` reports `1` for the notice and needs no example link (it points at `getting-started.md` and `PhoriaOptions.cs` instead — confirm both are present). `workspaces.md` reports `1` and links `../../examples/with-workspace`. `supported-ui-frameworks.md` reports `1` and links examples.

- [ ] **Step 6: Confirm no date was promised and no generic warning survives**

```bash
grep -rn "work in progress" docs/guides/configuration.md docs/guides/workspaces.md docs/guides/supported-ui-frameworks.md
grep -rniE "coming soon|will be (available|released|published)|next release|by [A-Z][a-z]+ 20[0-9][0-9]" docs/guides/configuration.md docs/guides/workspaces.md docs/guides/supported-ui-frameworks.md
```

Expected: both commands produce no output. The first is the old generic warning; the second catches a date or vague ETA sneaking in, which the notice rule forbids.

- [ ] **Step 7: Commit**

```bash
git add docs/guides/configuration.md docs/guides/workspaces.md docs/guides/supported-ui-frameworks.md docs/guides/getting-started.md
git commit -m "docs: give the incomplete guides a notice that names what they will cover"
```

---

### Task 8: The trialist path, executed

The only task in the plan whose deliverable is verified by *running* it. Everything else is a read-through, and the spec is explicit that this one is not — a command that looks correct and is not cannot be caught by reading.

**Files:**
- Modify: `README.md:21-37` (the `## Getting started` section)
- Modify: `docs/guides/getting-started.md:8-14` (the `## Clone an example project` section)
- Modify: `examples/README.md:5-19` (the `## Catalog` table)
- Modify: `examples/README.md:33` (the contributor-contract `giget` reference, for casing consistency)

**Interfaces:**
- Consumes: the fact that all nine examples carry a `docker-compose.yml`, a `Dockerfile` and a `.dockerignore` on port 8080, and that `giget` fetch plus a Production container reaching `Healthy` were each verified 9/9 in the examples-parity phase.
- Produces: the three-command trialist block, the `## Clone an example project` section that expands it, and the `#canary` aside in its two permitted locations. Task 9's index links `getting-started.md`, whose trialist route this task creates.

- [ ] **Step 1: Replace `README.md`'s inline pnpm commands with the trialist path**

`README.md:21-37` currently opens with a two-example list followed by a three-command `pnpm` block. Global Constraint: at most three commands as one contiguous block may live in `README.md`, so the `pnpm` block is replaced, not appended to. Replace the whole `## Getting started` section with:

````markdown
## Getting started

Run the example with Docker and no local toolchain — no .NET SDK, Node, pnpm or Aspire CLI required:

```shell
pnpx giget gh:CMeeg/phoria/examples/getting-started getting-started
cd getting-started && docker compose up --build -d
```

Then open <http://localhost:8080>, and stop with `docker compose down`.

The first build pulls and compiles a multi-stage .NET and Node image, so expect it to take a few minutes.

> To target the canary branch instead, append `#canary` to the ref — `gh:CMeeg/phoria/examples/getting-started#canary`. The separator is `#`, not `@`; `@` is npm dist-tag syntax.

To develop against it, or to add Phoria to an existing .NET project, see the [Getting started guide](./docs/guides/getting-started.md).
````

Keep the two links to `examples/getting-started` and `examples/framework-multiple` — they are links, not commands, and they are the fastest route to the source. The dev-loop pointer replaces the removed `pnpm` block.

- [ ] **Step 2: Expand `## Clone an example project` in the journey**

`getting-started.md:8-14` gains the fuller version with the explanation the README cannot carry:

````markdown
## Clone an example project

Every example under [`examples/`](../../examples) is a standalone application outside this repository's workspace. Fetch one with `giget` and run it in Docker, without installing the .NET SDK, Node, pnpm or the Aspire CLI:

```shell
pnpx giget gh:CMeeg/phoria/examples/getting-started getting-started
cd getting-started && docker compose up --build -d
```

Then open <http://localhost:8080>. The first build pulls and compiles a multi-stage .NET and Node image, so expect a few minutes. Stop with `docker compose down`.

> To target the canary branch instead, append `#canary` to the ref — the separator is `#`, not `@`; `@` is npm dist-tag syntax.

To run the example from source instead, use the development loop below.
````

- [ ] **Step 3: Add the `#canary` aside to `examples/README.md`'s catalog, and normalise the casing there**

`examples/README.md:33` currently reads `pnpx giget gh:cmeeg/phoria/examples/<name> <name>` — lowercase owner. The canonical owner casing is `CMeeg`. Add the aside beneath the `## Catalog` table and fix the casing:

```markdown
> To fetch from the canary branch, append `#canary` to the ref — `gh:CMeeg/phoria/examples/<name>#canary`. The separator is `#`, not `@`; `@` is npm dist-tag syntax.
```

- [ ] **Step 4: Execute the trialist path end to end**

This is the verification. Run it in a temporary directory, using `#canary` — the unqualified form resolves `main`, where `examples/` does not exist yet, so it would 404 for a reason that has nothing to do with whether the path works.

```bash
rm -rf /tmp/opencode/ph6-trialist && mkdir -p /tmp/opencode/ph6-trialist
cd /tmp/opencode/ph6-trialist
pnpx giget@latest gh:CMeeg/phoria/examples/getting-started#canary getting-started
cd getting-started
ls docker-compose.yml Dockerfile .dockerignore
```

Expected: the fetch succeeds and all three files are listed. `ls` failing on any of them means the example is not at Docker parity and the trialist path is not shippable — stop and report.

- [ ] **Step 5: Build and start the container, and wait for health**

```bash
cd /tmp/opencode/ph6-trialist/getting-started
docker compose up --build -d
```

```bash
docker compose ps
```

Expected: the container reaches `Up`. The health status may read `Unhealthy` for the first few seconds while the Node child process binds — **the monitor reports `Unhealthy` until the node child binds, so two consecutive healthy readings are required before concluding anything is wrong.** Poll twice:

```bash
sleep 20 && docker compose ps
sleep 20 && docker compose ps
```

Expected: `healthy` in both. If the first is `healthy` and the second is not, that is a real failure and the trialist path does not work.

- [ ] **Step 6: Confirm the page actually serves**

```bash
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8080
curl -s http://localhost:8080/health
```

Expected: `200`, and a healthy payload. The first build compiles a multi-stage .NET and Node image, so allow several minutes on the first `up --build`; the timeout on a cold build is not a failure of the path.

- [ ] **Step 7: Tear down and confirm the documented stop command works**

```bash
docker compose down
docker compose ps
```

Expected: no containers running. This is the third command of the documented block, so it is part of what Step 1 verified — the teardown line is not decorative.

- [ ] **Step 8: Verify the aside appears in exactly two places**

```bash
grep -rn "canary" README.md examples/README.md examples/*/README.md
```

Expected: hits in `README.md` and `examples/README.md` only. **Zero hits in the nine `examples/*/README.md`** — those document the released state, and a pre-1.0 branch target repeated nine times is noise that would outlive the beta stream. Their `giget` references are repaired in Plan B, where the uniform `## Try it` section that contains them is written.

- [ ] **Step 9: Verify `README.md` holds exactly one command block**

```bash
awk '/^```shell/,/^```$/' README.md
```

Expected: exactly one shell block, containing exactly the three documented commands. This is the Global Constraint that stops the trialist path and the guide from becoming two homes.

- [ ] **Step 10: Commit**

```bash
git add README.md docs/guides/getting-started.md examples/README.md
git commit -m "docs: add a three-command Docker trialist path, verified by running it"
```

---

### Task 9: The journey-shaped guide index

Depends on Tasks 2-6. The index may only link guides that are non-placeholder when the slice lands, so it cannot be written until the extraction has decided which those are.

**Files:**
- Modify: `README.md:38-47` (the `## Usage` section)

**Interfaces:**
- Consumes: the output of Tasks 2-7.
- Produces: the guide index, which is the only place in `README.md` that points at `docs/guides/`.

- [ ] **Step 1: Determine which guides are real, from the files rather than from intent**

```bash
. /tmp/opencode/ph6-lib.sh
for f in docs/guides/*.md; do
  if grep -q "work in progress\|This guide is incomplete" "$f"; then
    printf 'INCOMPLETE  %s\n' "$f"
  else
    printf 'real        %s\n' "$f"
  fi
done
```

Expected `real`: `getting-started.md`, `creating-phoria-island-components.md`, `directives.md`, `building-for-production.md`, `deployment.md`, and — after Tasks 2-6 — `phoria-server.md`, `client-entry.md`, `server-entry.md`, `phoria-web-app.md`, `component-register.md`, `phoria-islands.md`. Expected `INCOMPLETE`: `configuration.md`, `workspaces.md`, `supported-ui-frameworks.md`. Anything else is a finding — resolve it before writing the index.

- [ ] **Step 2: Replace the flat five-item list with a journey-shaped index**

Replace the five bullets under `## Usage` with:

```markdown
## Usage

Run it, then build on it, then go deeper:

1. [Getting started](./docs/guides/getting-started.md) — run the example with Docker, then add Phoria to an existing .NET project.
2. [Creating Phoria Island components](./docs/guides/creating-phoria-island-components.md) — write a component and render it from .NET, with the Tag Helper and the component factory.
3. [Phoria Islands](./docs/guides/phoria-islands.md) — what an island is, what the Tag Helper does for you, and when to render on the server, in the browser, or both.
4. [Component register](./docs/guides/component-register.md) — how Phoria finds your components and knows how to render each one.
5. [Client Entry](./docs/guides/client-entry.md) and [Server Entry](./docs/guides/server-entry.md) — the two entry points the two halves of an island run through.
6. [Phoria Server](./docs/guides/phoria-server.md) — the Node.js sidecar: what it does, how it is supervised, how it reports health, and how it degrades.
7. [Phoria Web App](./docs/guides/phoria-web-app.md) — the .NET web app with the `Phoria` NuGet package installed and configured.
8. [Phoria Island directives](./docs/guides/directives.md) — the client directives that choose when an island hydrates.
9. [Building for production](./docs/guides/building-for-production.md)
10. [Deployment](./docs/guides/deployment.md)
```

Keep the `> [!NOTE]` block that follows the current list, unchanged. Its honesty property is preserved by construction: the index advertises only real content, and the three incomplete guides are absent from it.

- [ ] **Step 3: Verify every link in the index resolves to a non-placeholder guide**

```bash
. /tmp/opencode/ph6-lib.sh
links=$(grep -o '](\./docs/guides/[a-z-]*\.md)' README.md | sed 's/](\.\///; s/)$//' | sort -u)
for l in $links; do
  printf '%-52s ' "$l"
  if [ -f "$l" ] && ! grep -q "work in progress\|This guide is incomplete" "$l"; then echo OK; else echo "BROKEN OR INCOMPLETE"; fi
done
```

Expected: every line `OK`. This is Review Focus 4 — the current index is honest, and that property is what this check protects.

- [ ] **Step 4: Verify the three incomplete guides are absent**

```bash
grep -c "guides/configuration.md\|guides/workspaces.md\|guides/supported-ui-frameworks.md" README.md
```

Expected: `0`. Linking an incomplete guide from the index would advertise content the slice deliberately did not write.

- [ ] **Step 5: Commit**

```bash
git add README.md
git commit -m "docs: shape the guide index as the reader's path, linking only real content"
```

---

### Task 10: The framework-extension recipe

`createPhoriaFrameworkPlugin` is exported from `@phoria/phoria/vite` (`packages/phoria-islands/src/vite/framework.ts:154-155`) and consumed by all three framework packages as thin composers. `docs/ARCHITECTURE.md:263-288` describes what the three existing adapters do. Nothing describes how to write a fourth, and the open `TODO.md` item to add `phoria-preact` depends on it.

**Files:**
- Create: `packages/phoria-islands/docs/framework-plugin.md`
- Modify: `docs/ARCHITECTURE.md:263-288` (add a link to the recipe; content otherwise unchanged)
- Modify: `packages/phoria-islands/README.md` (link the new `docs` folder from **learn more**)

**Interfaces:**
- Consumes: nothing from earlier tasks. Independent of Tasks 2-9.
- Produces: the canonical framework recipe, in the package's first `docs` folder. Task 11 completes the `ARCHITECTURE.md` drift pass. Plan B adds links to this recipe from the three framework package READMEs.

- [ ] **Step 1: Read the factory and the options type**

```bash
sed -n '1,60p' packages/phoria-islands/src/vite/framework.ts
sed -n '93,160p' packages/phoria-islands/src/vite/framework.ts
```

The options type has exactly seven fields — `name`, `include`, `exclude`, `cwd`, `workspacePackages`, `optimizeDeps`, `ssrExternal` — and the factory returns a Vite `Plugin` with six hooks: `name`, `config`, `configEnvironment`, `configResolved`, `applyToEnvironment`, `transform`. Document all seven and all six.

- [ ] **Step 2: Read one composer end to end as the worked example**

```bash
cat packages/phoria-react/src/vite/plugin.ts
```

Read the Svelte and Vue equivalents too — Svelte's extra `ssrExternal` entry and Vue's `vue: false` opt-out are the framework-specific differences the recipe has to show how to make:

```bash
cat packages/phoria-svelte/src/vite/plugin.ts
cat packages/phoria-vue/src/vite/plugin.ts
```

- [ ] **Step 3: Write `packages/phoria-islands/docs/framework-plugin.md`**

The content is inlined below. It is transcribed from the files in Steps 1-2, so the implementer writes rather than reconstructs. Where the code disagrees with this draft, **the code wins** — and the difference is worth reporting, because it means a fourth framework is not a pure copy.

````markdown
# Adding a Phoria framework package

Phoria ships React, Svelte and Vue adapters. Each is a thin composer over one shared factory, so adding a fourth is mostly configuration plus two small service modules — not a new Vite plugin.

## The shape of a framework package

Four entry points, one directory per purpose. Every JS package in the repository has these four, and changing one does not affect the others.

| Entry | Source | Contents |
| --- | --- | --- |
| `.` | `src/main.ts` | `const framework = { name: "react" } as const`, exported |
| `./client` | `src/client/main.ts` | `registerCsrService(framework.name, service)` |
| `./server` | `src/server/main.ts` | `registerSsrService(framework.name, service)`, plus re-exports of the render helpers and an island type guard |
| `./vite` | `src/vite/plugin.ts` | the framework Vite plugin |

`src/main.ts` is exactly this, and is the whole of the `.` entry:

```ts
const framework = {
	name: "react"
} as const

export { framework }
```

The service entries import it through the package's `~` path alias, as `~/main`:

```ts
import { registerSsrService } from "@phoria/phoria"
import { framework } from "~/main"
import { isReactIsland, renderComponentToStream, renderComponentToString, service } from "./ssr"

registerSsrService(framework.name, service)
```

## What the shared factory owns, and what you supply

`createPhoriaFrameworkPlugin` and its `PhoriaFrameworkPluginOptions` type are exported from `@phoria/phoria/vite`. The factory owns the `__phoriaComponentPath` transform, the `applyToEnvironment` guard limiting that transform to the `client` and `ssr` environments, the `ssr` environment registration, and the merge of your `optimizeDeps` and `ssrExternal` entries. It returns a plain Vite `Plugin`.

`PhoriaFrameworkPluginOptions` has seven fields. **Only four of them are yours to expose**, and the split is deliberate — it is enforced in every framework package's option type by omitting the other three:

```ts
interface PhoriaReactPluginOptions extends Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal"> {
	react?: ReactOptions | false
}
```

| Option | Who sets it | What it is |
| --- | --- | --- |
| `name` | package | the plugin name; use the framework package's name, e.g. `phoria-react` |
| `include` | **caller** | glob of modules to transform; defaults to the framework's own component extensions, e.g. `["**/*.jsx", "**/*.tsx"]` |
| `exclude` | **caller** | glob to leave alone; defaults to `"node_modules/**"` |
| `cwd` | **caller** | the Vite project root; defaults to `process.cwd()` |
| `workspacePackages` | **caller** | workspace packages whose modules are application-owned and must be transformed like your own; defaults to `[]` |
| `optimizeDeps` | package | pre-bundled runtime entries, e.g. `["react", "react-dom/client"]` |
| `ssrExternal` | package | modules externalised from the `ssr` environment |

The three package-set fields are not caller-configurable because getting them wrong breaks rendering rather than producing a useful error: a missing `optimizeDeps` entry makes the SSR environment resolve the framework's unbundled ESM, and a wrong `ssrExternal` can put two copies of a module-level singleton in the same process. If your framework needs an `ssrExternal` entry beyond its own server entry, add it in the package, as Svelte does.

### Why `workspacePackages` exists

The transform skips anything under `node_modules`, because dependencies are not application-owned. A workspace package is symlinked into `node_modules`, so by path alone it is indistinguishable from a registry dependency — a component imported from one would be skipped, its island would render blank, and nothing would say why. Naming the package in `workspacePackages` puts its modules back in scope. Leave it empty for a single-package app.

## The Vite plugin

Wrap the framework's own Vite plugin, compose the shared factory, and return both. This is `packages/phoria-react/src/vite/plugin.ts` in full, and it is the template to copy:

```ts
import { createPhoriaFrameworkPlugin, type PhoriaFrameworkPluginOptions } from "@phoria/phoria/vite"
import react, { type Options as ViteReactPluginOptions } from "@vitejs/plugin-react"
import type { PluginOption } from "vite"

const pluginName = "phoria-react"

export type ReactOptions = Pick<ViteReactPluginOptions, "include" | "exclude">

interface PhoriaReactPluginOptions extends Omit<PhoriaFrameworkPluginOptions, "name" | "optimizeDeps" | "ssrExternal"> {
	react?: ReactOptions | false
}

const defaultOptions: PhoriaReactPluginOptions = {
	include: ["**/*.jsx", "**/*.tsx"],
	exclude: "node_modules/**",
	cwd: process.cwd(),
	workspacePackages: []
}

function phoriaReact(options?: Partial<PhoriaReactPluginOptions>): PluginOption {
	const opts = { ...defaultOptions, ...options }
	const plugins: PluginOption = options?.react !== false ? [...react(options?.react)] : []

	plugins.push(
		createPhoriaFrameworkPlugin({
			name: pluginName,
			include: opts.include,
			exclude: opts.exclude,
			cwd: opts.cwd,
			workspacePackages: opts.workspacePackages,
			optimizeDeps: ["react", "react-dom/client"],
			ssrExternal: ["@phoria/phoria-react/server"]
		})
	)

	return plugins
}

export type { PhoriaReactPluginOptions }
export { phoriaReact }
```

Four things to get right, each of which has bitten a framework that got it wrong:

1. **`false` is the opt-out, and it is per-plugin.** `options?.react !== false` decides whether the framework's own Vite plugin is included at all; `options?.react` then forwards the framework plugin's own options. All three packages follow this, and it is what lets an application that already configures `react()` itself avoid a double transform.
2. **Spread the framework plugin's return value.** React and Svelte plugins return arrays (`[...react(...)]`, `[...svelte(...)]`); Vue's returns a single plugin (`vue(...)`, unspread). Copy whichever matches the framework you are adding — the return type differs per plugin, and `PluginOption` accepts both.
3. **`optimizeDeps` names the runtime entries the framework needs pre-bundled.** `["react", "react-dom/client"]` for React, `["svelte"]` for Svelte, `["vue"]` for Vue.
4. **`ssrExternal` names the framework's own server entry — and sometimes the framework itself.** All three externalise `@phoria/phoria-<framework>/server`. Svelte additionally externalises `svelte`, so the Vite-transformed component and the renderer share one `svelte/internal/server` instance; a second copy means two copies of module-level `ssr_context`, and `push_element` crashes reading `null`. That is a Svelte-specific hazard, not a general rule, but it is the reason to read your framework's SSR path before fixing this list.

## The two service modules

**`./server`** exports an SSR service — a single `render` function. The type is `PhoriaIslandComponentSsrService<F, T>`, and the shape is fixed across all three frameworks:

```ts
const service: PhoriaIslandComponentSsrService<typeof framework.name, FunctionComponent> = {
	render: async (component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		const island = await importComponent<typeof framework.name, FunctionComponent>(component)

		const renderComponent = options?.renderComponent ?? renderComponentToStream

		const html = await renderComponent(island, props)

		return {
			framework: framework.name,
			componentPath: island.componentPath,
			html
		}
	}
}
```

Four obligations, in order: **reject** a component registered for another framework; **import** it via `importComponent`, which resolves the loader and the component; **render** it with the props spread onto it, honouring a per-call `options.renderComponent` override; and **return** `{ framework, componentPath, html }`, where `html` is a string or a `ReadableStream`. `componentPath` is what the preload chain depends on — returning the island's own `componentPath` unchanged is what makes preload links resolve.

**`./client`** exports a CSR service — a single `mount` function, typed `PhoriaIslandComponentCsrService<F, T>`:

```ts
mount: async (island, component, props, options) => {
	// …verify framework, then…
	const mode = options?.mode ?? csrMountMode.hydrate
	// dynamically import the framework runtime, then hydrate or mount
}
```

Two rules. **Import the framework's runtime dynamically** (`await import("react")`, `await import("react-dom/client")`), so a server-rendered-only island does not pull the framework into the client's first load. And **default to hydrate**, not mount — `options?.mode ?? csrMountMode.hydrate` — because the usual case is markup that is already in the document.

### Per-framework rendering differences

| | SSR | CSR |
|---|---|---|
| **React** | `renderComponentToStream` (default, via `renderToReadableStream` from `react-dom/server.edge`) or `renderComponentToString`, both wrapping in `<StrictMode>` | `hydrateRoot` / `createRoot`, both in `<React.StrictMode>`; React and react-dom are dynamically imported; in dev a shim defines `window.$RefreshReg$`/`$RefreshSig$` because the Vite react-refresh preamble never runs |
| **Svelte** | `renderComponentToString` only — `render()` from `svelte/server`, returns `html.body` | `Svelte.hydrate` / `Svelte.mount` (Svelte 5); props passed only when non-null |
| **Vue** | `renderComponentToStream` (default, `renderToWebStream`) or `renderComponentToString`, via `createSSRApp` | `createApp().mount` — Vue has no explicit hydrate/render distinction |

Vue's row is the one to note: it has no hydrate/mount distinction at all, so its CSR service mounts unconditionally.

### The loader, and when the object form is required

A component's `loader` in the register is one of two shapes, and picking the wrong one is the most common registration mistake:

```ts
type PhoriaIslandComponentLoader<M, T> =
	| PhoriaIslandComponentModuleLoader<M, T>      // { module, component }
	| PhoriaIslandComponentDefaultModuleLoader<T>  // () => import("./x")
```

The **function** form imports the module and takes its **default export**. The **object** form names the export explicitly, and is required whenever the component is a named export. React islands are almost always named exports, which is why the multi-framework register below uses the object form for React and the function form for Svelte and Vue.

## Registering the framework in the entries

Both entries must import their framework's service, and **the import order is load-bearing**. `registerComponent` looks up the framework at registration time and throws if it is not there yet:

```
Cannot register component "ReactCounter" because the "react" framework has not been registered.
```

So the framework services come first, then the register, then the call. This is `examples/framework-multiple/WebApp/ui/src/entry-client.ts` and `entry-server.ts`, with all three frameworks installed:

```ts
import "@phoria/phoria-react/client"
import "@phoria/phoria-svelte/client"
import "@phoria/phoria-vue/client"
import "./components/register"
import { PhoriaIsland } from "@phoria/phoria/client"

PhoriaIsland.register()
```

```ts
import "@phoria/phoria-react/server"
import "@phoria/phoria-svelte/server"
import "@phoria/phoria-vue/server"
import "./components/register"
import type { PhoriaIsland } from "@phoria/phoria/server"

async function renderPhoriaIsland(island: PhoriaIsland) {
  return await island.render()
}

export { renderPhoriaIsland }
```

The matching multi-framework register, showing both loader forms:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  ReactCounter: {
    loader: {
      module: () => import("./counter/counter.tsx"),
      component: (module) => module.Counter,
    },
    framework: "react",
  },
  VueCounter: {
    loader: () => import("./counter/counter-button.vue"),
    framework: "vue",
  },
  SvelteCounter: {
    loader: () => import("./counter/counter.svelte"),
    framework: "svelte",
  },
})
```

Framework names are matched case-insensitively — `registerComponent` lowercases both the component name and the framework — so `framework: "react"` and `"React"` are the same key.

## Checklist

- [ ] Four entry points exported, each a single constant or a side-effect registration as above
- [ ] `registerCsrService` and `registerSsrService` called with `framework.name`
- [ ] The option type omits `name`, `optimizeDeps` and `ssrExternal`, and re-adds only the framework plugin's own options with a `false` opt-out
- [ ] `createPhoriaFrameworkPlugin` configured with all seven values, `include` and `exclude` defaulted to the framework's own component extensions
- [ ] Framework's own Vite plugin wrapped, spread or not according to what it returns
- [ ] `optimizeDeps` includes the framework runtime
- [ ] `ssrExternal` includes the framework's own server entry, plus the framework itself if its SSR path holds module-level state
- [ ] `workspacePackages` settable, defaulting to `[]`
- [ ] `render` rejects foreign frameworks, imports via `importComponent`, honours `options.renderComponent`, and returns `componentPath` unchanged
- [ ] `mount` imports the runtime dynamically and defaults to hydrate
- [ ] An example added under `examples/` reaching `Healthy` in a Production container
````

Three points in that draft are corrections to facts the sources contradict, and each is the kind of thing that would have made the recipe actively misleading:

- **The seven options are not all caller-settable.** All three packages' option types `Omit` `name`, `optimizeDeps` and `ssrExternal`. My earlier draft's table implied all seven were the caller's, which would have invited a fourth package to expose the three that must stay package-determined.
- **Registration order fails at registration, not at render.** `registerComponent` throws `Cannot register component "X" because the "Y" framework has not been registered.` My earlier draft said it "fails at render with a framework-mismatch error", which is a different error from a different module and would have sent an implementer looking in the wrong place.
- **Vue's plugin is not spread; React's and Svelte's are.** `[...react(...)]` and `[...svelte(...)]` against a bare `vue(...)`. A copy-paste of the React composer into the Vue package would produce a subtly wrong plugin list.

- [ ] **Step 4: Link the recipe from `ARCHITECTURE.md`**

`docs/ARCHITECTURE.md:263-288` is correct as it stands and is where agents arrive, so it keeps its summary. Add one line at the end of the `### Framework adapters` section:

```markdown
To add a fourth framework, see [`packages/phoria-islands/docs/framework-plugin.md`](../packages/phoria-islands/docs/framework-plugin.md).
```

Duplication is a permitted fallback, not a defect: if a later agent reads `ARCHITECTURE.md` and does not follow this link, the fix is to duplicate the recipe here, decided by that observation rather than in advance.

- [ ] **Step 5: Link the new `docs` folder from the package README**

`packages/phoria-islands/README.md` gains a **learn more** entry. The first `docs` folder inside a package establishes a convention, and the link is what makes it discoverable:

```markdown
- [Adding a Phoria framework package](./docs/framework-plugin.md)
```

- [ ] **Step 6: Verify every path named in the recipe exists**

```bash
for p in packages/phoria-islands/src/vite/framework.ts packages/phoria-react/src/vite/plugin.ts packages/phoria-svelte/src/vite/plugin.ts packages/phoria-vue/src/vite/plugin.ts examples/framework-multiple/WebApp; do
  printf '%-56s ' "$p"
  [ -e "$p" ] && echo OK || echo MISSING
done
```

Expected: `OK` for all five. Also confirm the recipe's two exports are real, and that the option types really do omit the three package-set fields — the recipe's central claim:

```bash
grep -n "^export { createPhoriaFrameworkPlugin\|^export type { PhoriaFrameworkPluginOptions" packages/phoria-islands/src/vite/framework.ts
grep -n 'Omit<PhoriaFrameworkPluginOptions' packages/phoria-{react,svelte,vue}/src/vite/plugin.ts
grep -n 'Cannot register component' packages/phoria-islands/src/register.ts
```

Expected: two export hits at `:154` and `:155`; three `Omit<...>` hits, one per framework package, each naming `"name" | "optimizeDeps" | "ssrExternal"`; and the registration error message present. If any framework package does *not* omit those three, the recipe's options table is wrong — fix the recipe, not the package, and say so in the commit.

- [ ] **Step 7: Commit**

```bash
git add packages/phoria-islands/docs/framework-plugin.md packages/phoria-islands/README.md docs/ARCHITECTURE.md
git commit -m "docs: document how to add a Phoria framework package"
```

---

### Task 11: `ARCHITECTURE.md` drift pass and runbook step 6

**Files:**
- Modify: `docs/ARCHITECTURE.md` — the `## Stable-cut runbook` section (`:531-539`), and any claim found to disagree with the code

**Interfaces:**
- Consumes: Task 10 (the recipe link is already in place; this task does not add it).
- Produces: `ARCHITECTURE.md` in agreement with the shipped code, and a stable-cut runbook whose step 6 is the post-cut verification of the ten `giget` references.

- [ ] **Step 1: Add step 6 to the `ARCHITECTURE.md` runbook, matching `CONTRIBUTING.md`**

`docs/ARCHITECTURE.md:531-539` and `CONTRIBUTING.md:72-77` currently carry the same five numbered steps. `CONTRIBUTING.md` is the canonical, human-facing copy; `ARCHITECTURE.md` is agent-first and must not present an incomplete runbook to an agent. Add to both:

```markdown
6. Verify the documentation `giget` references now resolve on `main` — fetch one example unqualified and confirm it builds, since an unqualified ref resolves the default branch and `examples/` is absent until this cut lands.
```

**These two runbooks duplicate by design.** They already did before this task; the documentation review required by Task 1 is what keeps them in sync.

- [ ] **Step 2: Verify the two runbooks agree**

```bash
diff <(sed -n '/^A maintainer cuts a stable release using this sequence:/,/^$/p' docs/ARCHITECTURE.md) <(sed -n '/^Stable releases are coordinated cuts, run by a maintainer:/,/^$/p' CONTRIBUTING.md)
```

Expected: differences only in the introductory sentence and the surrounding prose. The six numbered steps must match exactly.

- [ ] **Step 3: Verify the claims in the `## Agent cheat-sheet` resolve**

Every path the cheat-sheet names was checked while writing this plan; all resolve. Re-verify, because a file that moved since is exactly the drift this task exists to catch:

```bash
for p in \
  packages/Phoria/Islands/PhoriaIslandHtmlContent.cs \
  packages/phoria-react/src/server/ssr.tsx \
  packages/phoria-svelte/src/server/ssr.ts \
  packages/phoria-vue/src/server/ssr.ts \
  packages/phoria-islands/src/client/directives.ts \
  packages/phoria-islands/src/server/routing.ts \
  packages/Phoria/Islands/PhoriaIslandSsr.cs \
  packages/Phoria/Islands/PhoriaIslandPreloadTagHelper.cs \
  packages/Phoria/Islands/PhoriaIslandEntryTagHelper.cs \
  packages/Phoria/Server/ViteDevServerHmrProxy.cs \
  packages/Phoria/Server/PhoriaServerMonitor.cs \
  packages/Phoria/Server/PhoriaServerProcess.cs \
  packages/Phoria/Vite/ViteManifestReader.cs ; do
  printf '%-58s ' "$p"
  [ -e "$p" ] && echo OK || echo DRIFT
done
```

Expected: `OK` for all thirteen. Any `DRIFT` line is a real finding — correct the cheat-sheet to the path that exists.

- [ ] **Step 4: Verify the package and entry-point claims**

```bash
ls packages/
grep -c '"\./client"\|"\./server"\|"\./vite"' packages/phoria-islands/package.json
grep -rn "Phase 1[01]" AGENTS.md docs/ARCHITECTURE.md
```

Expected: the eight package directories (`Phoria`, `phoria-islands`, `phoria-opentelemetry`, `phoria-react`, `phoria-svelte`, `phoria-vue`, `Phoria.Tests`, `vite-plugin-dotnet-dev-certs`); the entry-point count matching the four-entry pattern the cheat-sheet and `AGENTS.md` describe; and no stale phase reference. `AGENTS.md:144` was corrected to Phase 11 in Task 1 — if it reads `Phase 10` here, Task 1 Step 2 did not land.

- [ ] **Step 5: Read the remaining sections against the code**

Sections not covered by a mechanical check above, read against the code they describe: `### The .NET host` (`:69`), `### The core JS runtime` (`:171`), `### The dev-certs plugin` (`:292`), `### Examples` (`:305`), `## Build & runtime pipeline` (`:330`), `## Request lifecycle` (`:377`), `## Contracts & invariants` (`:441`), `## Testing strategy` (`:483`), `## Release workflow` (`:515`).

For each, check the named files exist, the named commands match the root `package.json` scripts, and the described sequence matches the implementation. Record any disagreement as an edit to `ARCHITECTURE.md` in this task — the spec's non-goal forbids re-litigating this document's structure and depth, so corrections are made in place and nothing is restructured.

- [ ] **Step 6: Verify `## Related docs` still resolves**

```bash
for p in docs/guides docs/PROJECT.md docs/MEMORY.md CONTRIBUTING.md; do
  printf '%-24s ' "$p"
  [ -e "$p" ] && echo OK || echo BROKEN
done
```

Expected: all four `OK`.

- [ ] **Step 7: Commit**

```bash
git add docs/ARCHITECTURE.md CONTRIBUTING.md
git commit -m "docs: bring ARCHITECTURE.md back into agreement with the shipped code"
```

---

### Task 12: Slice close-out

**Files:**
- Modify: `docs/MEMORY.md` (append the sequencing decisions)
- Verify: every file this plan created or modified

**Interfaces:**
- Consumes: all eleven prior tasks.
- Produces: the slice's record in `MEMORY.md`, and the whole-branch check.

- [ ] **Step 1: Confirm the sequencing decisions are recorded in `docs/MEMORY.md`**

The sequencing decisions were appended to `docs/MEMORY.md` when this plan was authored, per the `/plan` command's process — they describe why the plan is shaped as it is, and are therefore part of its record rather than part of its execution. Confirm they are there and commit them with the slice:

```bash
grep -n "Phase 6 docs structure" docs/MEMORY.md
```

Expected: one hit. If it is missing, the entry was lost in a rebase — restore it from this plan's `## Sequencing Decisions` section plus the two findings below before continuing.

Record alongside it, if not already present: **the nine per-example `giget` references are Plan B's, not this plan's**, with the reasoning; and **the references are not syntactically broken** — they use lowercase `gh:cmeeg/phoria`, which GitHub resolves case-insensitively, and fail only because `examples/` is absent from `main` until the `canary` → `main` cut, which is why verification is step 6 of the runbook rather than something this plan can complete.

- [ ] **Step 2: Run the whole-slice check**

```bash
. /tmp/opencode/ph6-lib.sh
echo "=== guides still carrying a placeholder warning ==="
grep -rln "work in progress" docs/guides/ || echo "none"
echo "=== guides declaring themselves incomplete ==="
grep -rln "This guide is incomplete" docs/guides/
echo "=== every relative link from the journey resolves to a real file ==="
missing=0
for l in $(grep -o '](\./[a-z-]*\.md)' docs/guides/getting-started.md | sed 's/](\.\///; s/)$//' | sort -u); do
  [ -f "docs/guides/$l" ] || { echo "BROKEN: $l"; missing=1; }
done
[ $missing -eq 0 ] && echo "all resolve"
```

Expected: no placeholder warnings remain; exactly `configuration.md`, `workspaces.md` and `supported-ui-frameworks.md` declare themselves incomplete; all links resolve.

- [ ] **Step 3: Confirm the diff is documentation-only**

```bash
git diff --stat main...HEAD | tail -20
git diff --name-only main...HEAD | grep -vE '\.(md)$' || echo "no non-Markdown files changed"
```

Expected: `no non-Markdown files changed`. The slice writes no product code. If any non-Markdown file appears, something outside the plan ran — find it before committing.

- [ ] **Step 4: Commit**

```bash
git add docs/MEMORY.md
git commit -m "docs: record the Phase 6 docs structure sequencing decisions"
```

- [ ] **Step 5: Hand off to Plan B**

Plan B — `2026-09-26-phase-6-readme-sweep` — covers the seven package READMEs and the nine example READMEs. It depends on this plan's trialist path existing, because each example README's `## Try it` section is the per-file instance of what Task 8 built once in `README.md`. Plan B also carries the nine per-example `giget` reference repairs and the three framework package README links to the recipe from Task 10.

---

## Self-Review

**1. Spec coverage.** Every spec section maps to a task: the seam and its test → Tasks 2-7 (applied per paragraph, Step 2 of each); the concept guide mapping → Tasks 2-7, one guide per task; `phoria-islands` taking the concept register → Task 6; incomplete-guide notices → Task 7; the journey page after extraction → Tasks 2-5, 7, 8; the trialist path and its three-command rule → Task 8; the `#canary` aside in two places → Task 8; the runbook step 6 → Tasks 11 and 1; the journey-shaped index → Task 9; the trigger in three places → Task 1; package README shape → **Plan B** (the `phoria-islands` link is Task 10 Step 5); example README shape → **Plan B**; the framework recipe → Task 10; the placement rule → applied throughout, and stated in Task 10 Step 3; the nine placeholder warnings → Tasks 2-7; the verification table → Task 0's helpers, Task 8's execution, and the per-task read-throughs. The spec's Plan B boundary is respected: no package or example README is rewritten here.

**2. Placeholder scan.** No `TBD`, `TODO`, or "similar to Task N", and no angle-bracket markers. Task 10's recipe is inlined in full, transcribed from the sources its Steps 1-2 name, with the instruction that the code wins where they disagree. Task 11 Step 5 is a read-against-the-code instruction by nature: it names seven sections, and the spec's non-goal forbids restructuring `ARCHITECTURE.md`, so its output is corrections in place rather than new content.

**3. Type consistency.** `loss_check` and `no_placeholder` are defined once in Task 0 and called with the same arity throughout (`loss_check <src> <dest> <first> <last>`; `no_placeholder <file>...`). Task 4 establishes the one-line-exception form of `loss_check` and Task 7 Step 2 is the task that closes it — the check's expected output changes from `1` to `0` across those two tasks, and both state which.

**4. Review Focus.** All five lines have a pin: (1) the `LOST` detector, proved in Task 0 Step 2 before being relied on; (2) `no_placeholder` plus per-task link greps; (3) Task 8's executed path; (4) Task 9 Steps 3-4; (5) Task 5 Step 6.

**Two findings recorded during planning, not yet in the spec.** The spec's Context table lists seven dead links; there are **eight** — `getting-started.md:63` links to `supported-ui-frameworks.md` and is absent from the table. It does not change the work, since Task 7 covers that file, but the spec's count is wrong. And the ten `giget` references are **not** syntactically broken as the spec implies — they use lowercase owner casing, which GitHub resolves case-insensitively, and fail only because `examples/` is absent from `main`. Task 12 Step 1 records both in `MEMORY.md`.
