# Canary Dist-Tag Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Do not use subagent-driven-development: the registry half of this plan is a maintainer action, and a subagent cannot observe it land.

**Goal:** Make every prerelease publish land on the npm `canary` dist-tag instead of `beta`, retire the `beta` tag, and repair `@phoria/opentelemetry`, whose publishes have been routed to `latest` instead of the prerelease tag since its first release.

**Architecture:** The prerelease dist-tag lives in exactly one place — the `tag` field of `.changeset/pre.json` — and it cannot be set any other way, because Changesets rejects a `--tag` flag in pre mode. Renaming that field also renames the version family, because Changesets composes prerelease versions as `-<tag>.<n>`. So this is one field, four documents that name it, and a registry repair. The repair needs npm credentials that do not exist in the agent's environment, so it lands as a document the maintainer follows rather than as executed commands — and because it is correct only until it has been run, that document lives under the gitignored `.superpowers/` and the durable half (the policy) is what goes into `CONTRIBUTING.md`.

**Tech Stack:** Changesets (pre mode); the npm registry; Markdown. One JSON field changes. No product code.

**Spec:** None. This slice answers a maintainer instruction given in conversation — prereleases should publish under `@canary`, stables under `@latest` — and is argued from the Changesets source and the live registry rather than from a spec document. It is deliberately **not** part of Phase 6: it changes what the registry serves, not what a document says.

## Why the tag is the only lever

Three facts from the Changesets source, each re-derivable from upstream. File paths are from `changesets/changesets`.

**1. `--tag` is rejected in pre mode.** `packages/cli/src/commands/publish/index.ts` raises `Releasing under custom tag is not allowed in pre mode!`. The publish script is `changeset publish` (`package.json`), with no tag argument. So the field is the only place the tag can be chosen.

**2. The field is also the version family.** `packages/assemble-release-plan/src/increment.ts`:

```ts
if (preInfo != null && preInfo.state.mode !== "exit") {
  const preVersion = mapGetOrThrowInternal(preInfo.preVersions, release.name, /* ... */);
  // why are we adding this ourselves rather than passing 'pre' + versionType to semver.inc?
  // because semver.inc with prereleases is confusing and this seems easier
  version += `-${preInfo.state.tag}.${preVersion}`;
}
```

So `tag: "canary"` makes the next version PR produce `0.5.0-canary.2` (the pre counter parses `1` from `0.5.0-beta.1` and increments to `2`). There is no Changesets configuration that yields a `canary` dist-tag with `-beta.N` versions. **The tag rename and the version-family rename are one action.** The maintainer has accepted this.

**3. The tag name decides which dist-tag an only-prerelease package gets.** `packages/cli/src/commands/publish-plan/getPublishPlan.ts` classifies a package as `only-pre` when it is in pre mode, already has a `latest` dist-tag, and *every* published version's prerelease identifier equals the pre tag:

```ts
if (response.published) {
  publishedState = "published";
  publishedVersions = response.info.versions;
  if (preState != null &&
      response.info["dist-tags"].latest &&
      response.info.versions.every((version) => semverParse(version)!.prerelease[0] === preState.tag)) {
    publishedState = "only-pre";
  }
}
```

and `getReleaseTag` then returns `latest` — not the pre tag — for such a package:

```ts
if (tag) return tag;
if (preState != null && publishedState !== "only-pre") return preState.tag;
return "latest";
```

## The defect this repairs

`@phoria/opentelemetry` is the only `only-pre` package. All three of its published versions are `-beta.N`, and it holds a `latest` dist-tag, so every publish after its first was routed to `latest` and its `beta` tag never moved again.

| Package | `beta` | `latest` | Newest | only-pre |
|---|---|---|---|---|
| `@phoria/phoria` | `0.5.0-beta.1` | `0.4.2` | `0.5.0-beta.1` | no |
| `@phoria/phoria-react` | `0.5.0-beta.1` | `0.4.2` | `0.5.0-beta.1` | no |
| `@phoria/phoria-vue` | `0.4.0-beta.1` | `0.3.2` | `0.4.0-beta.1` | no |
| `@phoria/phoria-svelte` | `0.4.0-beta.1` | `0.3.2` | `0.4.0-beta.1` | no |
| `@phoria/opentelemetry` | **`0.2.0-beta.0`** | **`0.2.0-beta.2`** | `0.2.0-beta.2` | **yes** |
| `@phoria/vite-plugin-dotnet-dev-certs` | `0.3.0-beta.0` | `0.2.1` | `0.3.0-beta.0` | no |

`@phoria/opentelemetry`'s `beta` tag has been frozen at `0.2.0-beta.0`, its **first** publish (2026-08-16T00:28Z), while `latest` tracks `0.2.0-beta.2` (2026-09-24T20:58Z). An `npm i @phoria/opentelemetry@beta` install is therefore a silent two-version downgrade.

Renaming the tag to `canary` makes the `only-pre` test false for that package — its published versions say `beta`, the pre tag says `canary` — so it publishes to `canary` like every other package. The policy fix and the bug repair are the same one-word change.

### A prior plan documented the trigger and missed the consequence

`docs/superpowers/plans/2026-08-15-canary-release-workflow.md` records npm's first-publish auto-assign of `latest` for a never-published package and marks it "accepted and documented". That is accurate about the *first* publish and wrong about what follows: the `latest` it assigns is what makes the package `only-pre`, and `only-pre` routes every *subsequent* publish to `latest` as well. Plan documents are historical records and are not retrofitted; the correction is recorded in `docs/MEMORY.md` by Task 5.

## What does not change

- **Stable releases already publish to `latest`.** Step 1 of the stable-cut runbook exits pre mode, and outside pre mode `getReleaseTag` falls through to `latest`. No change.
- **NuGet follows automatically.** `scripts/dotnet/publish.js` packs with `-p:Version=${version}` from the package version, so NuGet prerelease labels become `-canary.N` with no edit.
- **The peer and example ranges stay as they are.** Verified in Task 4: the projected `0.5.0-canary.2` satisfies all thirteen registry ranges in the repository, and the four `workspace:*` protocol entries are not ranges at all. Lowering a floor to the new family would exclude the versions already in the wild, so the rename stays permissive in one direction only.
- **No document in the repository has ever shipped an `@beta` install line.** Plan B is the first slice to write install commands, and it has not run. Deleting the `beta` tag therefore breaks no documented consumer; the maintainer accepted the deletion.

## Global Constraints

- **Prose and commit messages in British English.** Historical documents that predate this are exempt.
- **Do not hard-wrap prose.** Every paragraph is one unbroken line; block elements and each list item get their own line. `AGENTS.md`'s `### Markdown & prose` rule.
- **The stable-cut runbook is maintained in two files** — `CONTRIBUTING.md` and `docs/ARCHITECTURE.md` — and they are byte-identical across all six steps today. Steps 1 and 4 change in this slice; they change in **both** files in the same commit, or the copies fork. Step 6 must not change at all.
- **Assert structure, not line numbers.** This plan's line references are the state at authoring time. Every check anchors on content.
- **Every check is self-validating, control-able, and carries a negative control.** A check that no mutation can turn red proves the fixture, not the check.
- **A check that reads a file must assert it read something.** A filter that greps a *path list* for content words produces an empty scan and a suite that reports passes over nothing.
- **Never trust a hand-rolled survey of what a document says, and never trust a hand-rolled list of phrases.** Authoring this plan, a manual `grep` for beta-tag wording found six sites; the first authored check found **twelve**, including `CONTRIBUTING.md:50` and `CONTRIBUTING.md:60`, which the survey had missed. Then that check was itself found to be a hand-rolled phrase list, and it missed two more: `CONTRIBUTING.md:96` (a heading reading `### Beta stream (canary)`, which its case-sensitive `beta stream` alternative could not match) and the `Before each later beta cycle` sentence in `docs/ARCHITECTURE.md:523`, where the enumerated instruction read `before a later beta cycle` in the *other* file. `docs_name_no_beta_dist_tag` now asserts **zero** case-insensitive occurrences of `beta` in either document, so there is no list to be incomplete. **The check is the authority on which sites exist; a count written in a plan is not.**
- **The policy is durable; the repair is not.** The dist-tag policy belongs in `CONTRIBUTING.md`, because it outlives this change and would otherwise rot. The `npm dist-tag` commands do not, because they are correct only until the repair is done, and a reader who finds them later cannot tell whether they are still needed. They live in a gitignored document under `.superpowers/`, and `no_one_off_commands_in_committed_docs` keeps them out of the repository.
- **The repair runs after the `pre.json` change has merged, never before.** A publish that happens while the repository still says `beta` re-creates the `beta` tag and undoes the repair. The tag is read from `pre.json` at publish time, so the field must read `canary` on the default branch in a merged commit first. This constraint surfaced while writing the repair document rather than before it, which is why the document states it as a precondition with a command that checks it.
- **No credentialed registry action is performed by the agent.** `npm whoami` returns 401 here. Registry mutation is a maintainer step, recorded in a gitignored document, exactly like the existing step 6.

## Review Focus

Failure modes this slice implies that no check exercises directly:

1. **A `pre.json` edit that drops state.** The file is generated and carries `mode`, `initialVersions` and a sixteen-entry `changesets` array. A careless reformat or a re-serialisation can silently discard the pre counters, which would make the next version PR restart the family. Expected: the file equals `HEAD`'s version with only `tag` differing — pinned by `pre_json_only_tag_changed`, which compares against `HEAD` with `tag` normalised on both sides so the invariant survives the commit that carries the change.
2. **One runbook copy edited and not the other.** The maintainer steps are duplicated prose in two files with no link between them. The failure is silent: both files read correctly, and they disagree. Expected: byte-identical — pinned by `runbook_copies_identical`, with `runbook_step_count_is_six` as its anti-vacuity floor.
3. **A document left naming `beta`.** Thirteen lines across two files, carrying more distinct claims than that. The consequence is a reader installing the wrong stream, or a maintainer following a runbook that names a tag which no longer exists. Expected: the word `beta` appears **zero** times, case-insensitively, in either document — pinned by `docs_name_no_beta_dist_tag`, with `docs_still_describe_the_stream` proving the sections were corrected rather than deleted and a 50-line floor in the first check so a stubbed file cannot pass by containing nothing.
4. **The peer ranges tightened to the new family.** The obvious tidy-up after a rename is to move every floor from `-beta.1` to `-canary.2`. That would exclude every version already installed in the wild. Expected: ranges are untouched and still admit the projected version — pinned by `ranges_admit_projected_canary`, which discovers ranges from the manifests and carries a lower-bound negative control.
5. **The tag deleted before it is created.** Deleting `beta` before `canary` exists leaves a window with no prerelease tag at all, and `npm i @phoria/phoria@canary` 404s for the whole of it. Expected: the repair document orders every `dist-tag add` before every `dist-tag rm`, asserted structurally by `repair_doc_adds_precede_removes` rather than trusted to the prose.
6. **The repair run against an unmerged repository.** Deleting `beta` while `.changeset/pre.json` still says `beta` works, and is silently undone by the next publish, which re-creates the tag from the still-unmerged field. Every check passes at the moment it is run, so nothing reports the problem; the symptom appears weeks later as a `beta` tag that reappeared. Expected: the repair document names the merged state as a precondition and gives a command that checks it — pinned by `repair_doc_is_contained` for the document's placement, and by the precondition section itself, which is the only place the constraint is stated.
7. **The one-off commands hardening into `CONTRIBUTING.md`.** The commands are correct today and wrong forever after. Written into a committed document they read as a standing instruction, and a future maintainer either re-runs a repair that is already done or believes the tags are still `beta`. Expected: absent from every committed document — pinned by `no_one_off_commands_in_committed_docs`, with `policy_is_documented` proving the durable policy is present rather than merely not contradicted.

## Verification helpers

Four files, each authored and control-proven before this plan was saved. They are scratch, not repository artifacts.

- `/tmp/opencode/dtag-harness.sh` — `pass`, `fail`, `check_eq`, `summary`. `summary` fails a suite that ran zero checks.
- `/tmp/opencode/dtag-facts.sh` — read-only registry facts. `dtag_table` prints the table above; `dtag_onlypre <pkg> <tag>` evaluates the `only-pre` test against an arbitrary candidate tag, which is how the flip is asserted without publishing.
- `/tmp/opencode/dtag-lib.sh` — `pre_json_tag_is`, `pre_json_only_tag_changed`, `pre_json_state_is_intact`, `docs_name_no_beta_dist_tag`, `docs_still_describe_the_stream`, `runbook_copies_identical`, `runbook_step_count_is_six`, `extract_runbook`.
- `/tmp/opencode/dtag-ranges.sh` — `ranges_admit_projected_canary_suite`, `project_canary`, `dtag_semver_path`.
- `/tmp/opencode/dtag-repair.sh` — `repair_doc_is_contained`, `repair_doc_content`, `no_one_off_commands_in_committed_docs`, `policy_is_documented`. Shared by Task 4 and Task 6 so there is one copy of the repair-document assertions.

Controls already demonstrated at authoring time: `pre_json_only_tag_changed` red on a mutated `initialVersions`; `pre_json_state_is_intact` red on a dropped `changesets` array; `docs_name_no_beta_dist_tag` — now a zero-occurrence assertion — red on the uncorrected documents (listing all thirteen lines), red on either of the two claims its original phrase list missed, red on a wording nobody had listed, red when a file is stubbed below its 50-line floor, and red when a file is missing; `docs_still_describe_the_stream` red on a gutted section; `runbook_copies_identical` red on a one-sided edit; `runbook_step_count_is_six` red at five steps; `ranges_admit_projected_canary_suite` red on a range of `>=0.6.0-canary.1`; and `dtag_onlypre` reporting one `only-pre` package under `beta` and zero under `canary`.

The eighteen repair-document checks were run against the real document, which passes all eighteen, and each was turned red in turn: the document moved into a tracked path, a package's commands omitted, the `rm` commands placed before the `add`s, opentelemetry's add pointed at its stale `0.2.0-beta.0`, a stable package's `latest` also removed, one `rm` dropped, and an `npm dist-tag add` line added to `docs/ARCHITECTURE.md`.

### Eight defects these checks carried while being authored

Recorded because each is a way of writing a check that reports a pass it has not earned, and each was found by running the check rather than by reading it.

1. **A command form without a version breaks a version-shaped pattern.** `npm dist-tag add <pkg>@<version> canary` and `npm dist-tag rm <pkg> beta` are different shapes. A pattern written for the first (`$p@`) silently matched only the adds, so a runbook missing every `rm` scored 1 instead of 2. Match the shape each form actually has.
2. **Prose that mentions a command is not a command.** The runbook's own sentence — "Every `dist-tag add` runs before any `dist-tag rm`" — matched both greps and made a correctly ordered block look inverted. Anchor command greps at the start of the line.
3. **A uniform expectation is wrong when one case is special.** Expecting two commands per package failed on `@phoria/opentelemetry`, which has three: it also loses its `latest`. Assert each package's shape and give the special case its own named check, rather than a count that is only true for the easy ones.
4. **A truthiness test compared against a value.** `pre_json_state_is_intact` printed `j.mode ? 1 : 0` and then compared it to the string `pre`, so a healthy file failed. Emit the value and compare values.
5. **A negative control must probe the bound that actually binds.** `>=0.5.0-beta.1` is open-ended, so a control version above it (`9.9.9`) satisfies it and the control fails on a correct file. The control has to probe the floor. Separately, pnpm's `workspace:*` is a protocol, not a range; passing it to `semver.satisfies` is meaningless, so it is counted and reported rather than dropped silently.
6. **Revising a document does not re-verify the checks that document carried.** Splitting the repair out of `CONTRIBUTING.md` left both the old inlined check block *and* a new shared `dtag-repair.sh` in the slice — two copies of the same assertions, in the one place that already has a check for two copies of the same prose forking. Every check still passed; the defect was invisible to all of them, because a check asserts against a file, not against a plan. Assert the plan's shape too: a structural sweep for duplicated assertion blocks and for `---` separators between tasks found both this and a separator the splice had dropped.
7. **A check that is correct and red is a signal, not a nuisance.** `policy_is_documented` fails right now, because `CONTRIBUTING.md` has no policy subsection until Task 3. That is the check working. A suite that must be green before the plan has run is a suite that cannot be written before the plan has run.
8. **A hand-rolled phrase list is a survey wearing a check's clothes.** `docs_name_no_beta_dist_tag` was an alternation of every beta wording its author could think of, and it was wrong twice: `### Beta stream (canary)` has a capital `B` and `grep -E` is case-sensitive, and `Before each later beta cycle` in `docs/ARCHITECTURE.md` is not the `before a later beta cycle` that the same author enumerated for `CONTRIBUTING.md`. A list of known-bad strings can only ever contain the strings someone thought of, and it looks exhaustive while it is not. **Assert the property, not the instances**: after the rename the word `beta` appears *zero* times, so the check is `grep -ic beta` against a floor, not an alternation. Same class as `no_placeholder` in Plan A, which was made case-insensitive for the same reason — and same lesson as the hand-rolled survey that found six sites where the check found twelve, one layer further out.

---

## Task 0 — Record the registry baseline

**Files:** none created. `/tmp/opencode/dtag-facts.sh` is already written; this task pins its output as the plan's baseline and confirms the fault is still present.

- [ ] **Step 1: Print the baseline table.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-facts.sh
  printf 'package\tnewest\tbeta\tlatest\tonlyPre\tnvers\n'
  dtag_table
  ```

  Expect six rows. The `onlyPre` column must be `yes` for `@phoria/opentelemetry` and `no` for the other five.

- [ ] **Step 2: Assert the fault, and the fix, against the live registry.**

  ```bash
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-facts.sh
  under_beta=0; under_canary=0
  for p in "${DTAG_PACKAGES[@]}"; do
    [ "$(dtag_onlypre "$p" beta)"   = yes ] && under_beta=$((under_beta+1))
    [ "$(dtag_onlypre "$p" canary)" = yes ] && under_canary=$((under_canary+1))
  done
  check_eq "only_pre_count_under_beta"   "$under_beta"   1
  check_eq "only_pre_count_under_canary" "$under_canary" 0
  summary
  ```

  This is the load-bearing assertion of the whole slice: it evaluates the real `only-pre` test from the real registry against both candidate tags, so the claim that renaming repairs the routing is measured, not argued. The control is inherent — the same loop with the expectations swapped must fail, and it does, because the counts are 1 and 0 rather than equal.

- [ ] **Step 3: Record the ruling.**

  Append to `.superpowers/sdd/2026-09-29-canary-dist-tag/progress.md`: the six-row baseline, the `only-pre` mechanism with its upstream file path, and the decision that the `beta` tag is deleted rather than left to go stale.

---

## Task 1 — Rename the tag in `pre.json`

**Files:** `.changeset/pre.json`

- [ ] **Step 1: Change the `tag` field and nothing else.**

  Use the `edit` tool. Replace `"tag": "beta",` with `"tag": "canary",`. Do not re-serialise the file, do not reorder keys, do not touch `mode`, `initialVersions` or `changesets`.

- [ ] **Step 2: Verify the change is surgical.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-lib.sh
  pre_json_tag_is canary
  pre_json_only_tag_changed
  pre_json_state_is_intact
  summary
  ```

  All three pass. `pre_json_only_tag_changed` compares the working tree against `git show HEAD:.changeset/pre.json` with `tag` normalised on both sides, so it is meaningful both before and after this change is committed.

- [ ] **Step 3: Record the ruling.**

  Note that the version family moves with the tag, that this is not separable through Changesets, and that the maintainer accepted `-canary.N`.

---

## Task 2 — Correct the documents that name the tag

**Files:** `CONTRIBUTING.md`, `docs/ARCHITECTURE.md`

Thirteen lines name the prerelease stream as `beta`. Correct claims in place; do not restructure either document.

The prose below is **guidance, not the work list**. It was written before the check was rewritten, and it already missed two real claims: a heading at `CONTRIBUTING.md:96` reading `### Beta stream (canary)`, and the `Before each later beta cycle` sentence in `docs/ARCHITECTURE.md:523` — the instruction below corrects "before a later beta cycle" for the *other* file, and this file says "each later". Step 1's list is the work list; the bullets are there to say what each correction is *for*, so a claim is fixed correctly rather than merely scrubbed of the word.

- [ ] **Step 1: Enumerate the sites with the check, not by hand.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-lib.sh
  docs_name_no_beta_dist_tag
  ```

  It fails and lists every line carrying the word, case-insensitively. That list is the work list. When the run is green the word appears zero times in either document, and the check cannot be satisfied by removing a phrase from an enumeration — it has no enumeration.

- [ ] **Step 2: Correct `CONTRIBUTING.md`.**

  Use the `edit` tool, one claim at a time. The corrections in substance:

  - The branch-model paragraph that says `canary` "produces prerelease `beta` builds" now produces prerelease `canary`-tagged builds. Keep the sentence's job — naming which branch publishes prereleases — and change only the stream name.
  - The review-and-merge list item that says an approved change "ships as a `beta` prerelease" says `canary` prerelease.
  - The release-workflow paragraph: the parenthetical "(npm `beta` dist-tag)" becomes "(npm `canary` dist-tag)"; "its natural 0.x beta version" becomes its natural 0.x prerelease version; "a matching beta to NuGet" becomes a matching prerelease to NuGet; "the released beta versions" becomes the released versions; "before a later beta cycle" becomes before a later prerelease cycle; and the closing "Betas are safe to consume for integration and production testing of work in progress" becomes "Prereleases are safe to consume…". That sentence is a judgement about prereleases, not about a tag, and it is worth keeping.
  - Runbook step 1: "exit beta mode" becomes "exit pre mode". Step 4: "enter beta mode" becomes "enter pre mode". The pre-mode *tag* is the subject of Task 1; these two steps describe entering and leaving the mode, and Changesets calls that mode "pre" whatever tag it carries.
  - The paragraph after the runbook: "the beta stream is quiescent" becomes "the canary stream is quiescent"; "until pre mode is re-entered" is already correct and stays.
  - The `### Beta stream (canary)` heading becomes `### The canary stream`, matching the corrected `docs/ARCHITECTURE.md` heading so the two documents name the section the same way. **This site was missed by the original check** because it capitalises `Beta` and the check's `beta stream` alternative was case-sensitive. Check for links to the old anchor across the repository rather than assuming there are none — the same anchor exists in both files, so a link written against either would break.

- [ ] **Step 3: Correct `docs/ARCHITECTURE.md`, including the runbook copy.**

  The same corrections, including the heading, plus:

  - The `### The beta stream` heading becomes `### The canary stream`. Anything linking to that anchor must be checked — `CONTRIBUTING.md` links to `#release-workflow`, not to the subsection, so no link breaks, but confirm it rather than assume.
  - The `tag: "beta"` inside the `pre.json` description becomes `tag: "canary"`. This is the one place a document states the field's value, so it must agree with Task 1 exactly.
  - "publishes each bumped package as a `beta` prerelease on npm and a matching beta on NuGet" becomes `canary` prerelease and a matching prerelease.
  - "so the beta remains in the natural 0.x version family" becomes "so the prerelease remains in the natural 0.x version family".
  - "Before each later beta cycle, update that lower-bound tuple" becomes "Before each later prerelease cycle…". **This site was missed by the original check** because the instruction above corrected "before a later beta cycle" — the wording in *this* file — and the sentence here reads "each later", which the enumeration never contained.
  - "pre.json presence/exit state determines beta versus stable behavior" becomes "prerelease versus stable behavior".
  - Runbook steps 1 and 4: the **same** edits as `CONTRIBUTING.md`, character for character.

- [ ] **Step 4: Verify.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-lib.sh
  docs_name_no_beta_dist_tag
  docs_still_describe_the_stream
  runbook_copies_identical
  runbook_step_count_is_six
  summary
  ```

  All four pass. `runbook_copies_identical` is the one that catches a one-sided edit; `runbook_step_count_is_six` proves the comparison is over six real steps and not an empty extraction.

- [ ] **Step 5: Confirm no heading link broke.**

  ```bash
  grep -rn "beta-stream\|#the-beta" --include="*.md" . | grep -v node_modules || echo "  no references to the old anchor"
  ```

- [ ] **Step 6: Record the ruling.**

  Note the site count the check reports and not the count this plan predicted — the plan said twelve twice and was wrong, because its check was a phrase list and the prose was a survey. Note specifically that the original `docs_name_no_beta_dist_tag` missed two real claims (`CONTRIBUTING.md:96`'s capitalised heading, and `docs/ARCHITECTURE.md:523`'s "each later beta cycle") and was replaced with a zero-occurrence assertion as a result, and that the two runbook copies were edited in lockstep.

---

## Task 3 — State the policy durably, without the commands

**Files:** `CONTRIBUTING.md` (a short subsection under `## Release workflow`)

The policy outlives this change and must be written down where a future maintainer will read it. The *commands* that implement it do not: they are a one-off repair, they are correct only until the repair is done, and a reader who finds them later cannot tell whether they are still needed. So the two are separated — this task writes the policy, Task 4 writes the commands.

- [ ] **Step 1: Add the policy subsection.**

  A short subsection in `## Release workflow` of `CONTRIBUTING.md`, placed with the existing stream descriptions. It states, and nothing beyond:

  - Prereleases publish under the npm **`canary`** dist-tag; stable releases publish under **`latest`**. Stable needs no mechanism of its own — leaving pre mode is what produces it.
  - The tag is the `tag` field of `.changeset/pre.json`. `--tag` is rejected in pre mode, so that field is the only place it can be set.
  - The tag is also the version family, so prerelease versions read `0.x.y-canary.N`. The two are one action and cannot be separated through Changesets.
  - A bare `npm i <name>` resolves to `latest`, which for these packages is a stable release older than the prerelease stream. Documentation that means the prerelease stream must name the tag.
  - NuGet has no dist-tags, so the `Phoria` package is pinned by version in documentation for that reason.

  **No `npm dist-tag` command appears in this subsection, or anywhere else in the repository's committed documents.** A one-off repair does not belong in a document that outlives it. Step 2 enforces that.

  This is a maintainer document, so it may state requirements and prohibitions. It is not a guide, and it does not belong in `docs/guides/`.

- [ ] **Step 2: Verify the policy is stated and the commands are absent.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-repair.sh

  policy_is_documented
  no_one_off_commands_in_committed_docs
  summary
  ```

  Five checks, called from `dtag-repair.sh` rather than written out again here. Task 4 and Task 6 call the same two functions, so there is one copy of these assertions in the slice. Inlining them per task would reproduce the exact fork that `runbook_copies_identical` exists to catch — the plan would carry three sets of assertions about a single one-off document, and the drift would be invisible until one of them failed for a reason that had already been fixed elsewhere.

  `policy_is_documented` states the policy **positively**, so a `CONTRIBUTING.md` that merely stopped mentioning `beta` cannot pass it. `no_one_off_commands_in_committed_docs` deliberately excludes `.superpowers/` and `docs/superpowers/plans/`: the repair document is gitignored, and the plan discusses these commands as history. Neither is a committed instruction. It is control-able — adding an `npm dist-tag add` line to `CONTRIBUTING.md` turns it red, which was demonstrated.

- [ ] **Step 3: Record the ruling.**

  Note that the policy is durable documentation while the repair is not, why they are separated, and that the historical fact of the repair belongs in `docs/MEMORY.md` at Task 6 rather than in `CONTRIBUTING.md`.

---

## Task 4 — The one-off repair document

**Files:** `.superpowers/sdd/2026-09-29-canary-dist-tag/one-off-registry-repair.md` — **created, never committed.**

`.superpowers/` is gitignored (`.gitignore:3`), so a document there cannot be committed by accident, and it survives on the maintainer's machine rather than in `/tmp`. It sits beside the ledger for this slice, which is where the record of the repair belongs.

The agent cannot perform the repair: `npm whoami` returns 401. This document is what the maintainer follows instead.

- [ ] **Step 1: Write the document.**

  Content, in this order:

  1. **What this is.** A one-time repair of the npm dist-tags, run once with the maintainer's own credentials and then discarded. It is not a procedure in the repository; the policy it implements is in `CONTRIBUTING.md`.
  2. **Why.** One paragraph: prereleases will publish under `canary` because the `tag` field of `.changeset/pre.json` says so, and `--tag` is rejected in pre mode, so that field is the only lever. The tag is also the version family, so the next version PR publishes `0.x.y-canary.N`.
  3. **Ordering, and why it matters.** Every `dist-tag add` runs before any `dist-tag rm`. Deleting `beta` first leaves a window with no prerelease tag on any package, and `npm i @phoria/phoria@canary` fails for its duration. There is no reason to hurry the deletes.
  4. **Add `canary` for all six**, each pinned to the version in the Task 0 baseline:

     ```bash
     npm dist-tag add @phoria/phoria@0.5.0-beta.1 canary
     npm dist-tag add @phoria/phoria-react@0.5.0-beta.1 canary
     npm dist-tag add @phoria/phoria-vue@0.4.0-beta.1 canary
     npm dist-tag add @phoria/phoria-svelte@0.4.0-beta.1 canary
     npm dist-tag add @phoria/opentelemetry@0.2.0-beta.2 canary
     npm dist-tag add @phoria/vite-plugin-dotnet-dev-certs@0.3.0-beta.0 canary
     ```

     Point `canary` at each package's **newest** published version. For `@phoria/opentelemetry` that is `0.2.0-beta.2`, not the `0.2.0-beta.0` its stale `beta` tag names — using the stale value would recreate the bug under a new name.
  5. **Delete `beta` for all six**, after the adds:

     ```bash
     npm dist-tag rm @phoria/phoria beta
     npm dist-tag rm @phoria/phoria-react beta
     npm dist-tag rm @phoria/phoria-vue beta
     npm dist-tag rm @phoria/phoria-svelte beta
     npm dist-tag rm @phoria/opentelemetry beta
     npm dist-tag rm @phoria/vite-plugin-dotnet-dev-certs beta
     ```
  6. **Remove the bogus `latest` from `@phoria/opentelemetry` only:**

     ```bash
     npm dist-tag rm @phoria/opentelemetry latest
     ```

     That package has never had a stable release, so a `latest` pointing at `0.2.0-beta.2` is what let a bare `npm i @phoria/opentelemetry` install a prerelease. Removing it means a bare install fails with a resolution error rather than silently handing out a prerelease. **This is a deliberate consumer-visible change, and the document says so in those words.** The other five packages have real stable releases and their `latest` tags are correct — do not touch them. Changesets will not re-create this `latest`: the first-publish auto-assign is gated on the package never having been published, and this one has been.
  7. **Verify.** A `npm view` block for all six, with the expected output written out, so the maintainer compares against a record rather than forming a judgement. Expected: `canary` on the newest version; `beta` absent everywhere; `latest` present and stable on the five, absent on `@phoria/opentelemetry`.
  8. **When it is done.** The document can be deleted, the policy it implements stays in `CONTRIBUTING.md`, and the next version PR will publish `0.5.0-canary.2`.

- [ ] **Step 2: Verify the document is correct, current and uncommittable.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-facts.sh
  source /tmp/opencode/dtag-repair.sh

  repair_doc_is_contained
  repair_doc_content
  summary
  ```

  Seventeen checks, called from `dtag-repair.sh`. Task 3 and Task 6 call the same two functions, so the slice carries one copy of the repair-document assertions instead of three. Each was turned red at authoring time against the real document, on copies: move it into a tracked path, omit a package's commands, place the `rm` commands before the `add`s, point opentelemetry's add at its stale `0.2.0-beta.0`, also remove a stable package's `latest`, and drop one `rm`.

  The per-package pattern is anchored deliberately. `^npm dist-tag add @phoria/phoria(@[^ ]+)? +canary$` cannot match `@phoria/phoria-react`, because the alternation requires either an `@version` or a space immediately after the package name. A substring match would have counted the react package's commands against `phoria` and passed a document that had dropped one of them.

  `otel_canary_targets_newest` reads the live registry rather than trusting this plan's copy of the version. If a release lands between authoring and execution it goes red, which is the point: the pinned version is stale and the document must be re-pinned.

- [ ] **Step 3: Record the ruling, and state plainly what has not happened.**

---

## Task 5 — Gate the version-family change on the ranges

**Files:** none modified. This task decides a question and proves it.

The rename moves versions into the `-canary.N` family. The obvious follow-up is to retarget every floor. That would be wrong, and the reason needs a check rather than an opinion.

- [ ] **Step 1: Prove the existing ranges still admit the projected version.**

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-ranges.sh
  ranges_admit_projected_canary_suite
  summary
  ```

  Expect one pass reporting thirteen registry ranges and four `workspace:*` protocol entries, with a projected `0.5.0-canary.2`. The `workspace:*` entries are pnpm protocol declarations, not ranges; the check counts them separately rather than dropping them silently.

- [ ] **Step 2: Read the direction of the change.**

  `semver` was verified to accept `0.5.0-canary.2` against `>=0.5.0-beta.1`, `^0.5.0-beta.1` and `>=0.5.0-0 <2.0.0`, and to reject `0.5.0-beta.1` against `^0.5.0-canary.1`. The rename is therefore permissive in one direction only: existing consumers keep resolving, but a range that adopts the new family's floor stops admitting the old versions. That asymmetry is the argument for leaving the floors where they are.

- [ ] **Step 3: Prove the check can fail.**

  Temporarily change `packages/phoria-vue/package.json`'s peer range on `@phoria/phoria` from `>=0.5.0-beta.1` to `>=0.6.0-canary.1`, re-run Step 1, and confirm it fails naming that file and range. Restore with `git checkout -- packages/phoria-vue/package.json` and confirm it passes again. A check that cannot be turned red proves the fixture.

- [ ] **Step 4: Record the ruling.**

  The floors stay. The examples' pins are rewritten by the existing `pnpm examples:bump` flow at the next release, as they are for any version change; that is the normal path, not a consequence of the rename.

---

## Task 6 — Close out the slice

**Files:** `docs/MEMORY.md`

- [ ] **Step 1: Run the whole slice.**

  The repair-document assertions live in one shared function, not two copies, so this cannot drift from Task 4. That is deliberate: the slice contains a duplicated maintainer runbook, and duplicating its checks would reproduce the exact failure the runbook's own byte-identity check exists to catch.

  ```bash
  cd /home/meeg/projects/cmeeg/phoria
  source /tmp/opencode/dtag-harness.sh
  source /tmp/opencode/dtag-facts.sh
  source /tmp/opencode/dtag-lib.sh
  source /tmp/opencode/dtag-ranges.sh
  source /tmp/opencode/dtag-repair.sh

  under_beta=0; under_canary=0
  for p in "${DTAG_PACKAGES[@]}"; do
    [ "$(dtag_onlypre "$p" beta)"   = yes ] && under_beta=$((under_beta+1))
    [ "$(dtag_onlypre "$p" canary)" = yes ] && under_canary=$((under_canary+1))
  done
  check_eq "only_pre_count_under_beta"   "$under_beta"   1
  check_eq "only_pre_count_under_canary" "$under_canary" 0

  pre_json_tag_is canary
  pre_json_only_tag_changed
  pre_json_state_is_intact
  docs_name_no_beta_dist_tag
  docs_still_describe_the_stream
  runbook_copies_identical
  runbook_step_count_is_six
  ranges_admit_projected_canary_suite

  policy_is_documented
  no_one_off_commands_in_committed_docs
  repair_doc_is_contained
  repair_doc_content
  summary
  ```

- [ ] **Step 2: Confirm the change set is what it claims to be.**

  ```bash
  git status --porcelain
  git diff --check
  ```

  Expect exactly three tracked files modified — `.changeset/pre.json`, `CONTRIBUTING.md`, `docs/ARCHITECTURE.md` — plus this plan as untracked. **The repair document must not appear.** It lives under `.superpowers/`, so it is invisible to `git status`; if it ever shows up, `git check-ignore` has stopped covering it. Any `.ts`, `.cs`, `.csproj` or other `package.json` in the list means something outside the plan ran — find it before committing.

- [ ] **Step 3: Append the `docs/MEMORY.md` entry.**

  Append a new dated section; the `## 2026-09-29` entry must remain the last heading, so if this entry is dated 2026-09-29 it goes inside that entry's list rather than as a new heading, or a new heading is added only if the date has moved on. The entry records:

  - The dist-tag policy: prereleases on `canary`, stables on `latest`, set by the `tag` field of `.changeset/pre.json`; `--tag` is rejected in pre mode, so that field is the only lever; the tag is also the version family, so versions read `-canary.N`.
  - The `@phoria/opentelemetry` defect and its mechanism — `only-pre`, `getReleaseTag`, the upstream paths — and that `docs/superpowers/plans/2026-08-15-canary-release-workflow.md` recorded the first-publish trigger while missing the consequence that every later publish was routed to `latest` too. That plan is a historical record and is not retrofitted.
  - That the repair is a one-off document under `.superpowers/`, gitignored and never committed, and that its commands are deliberately absent from the repository's committed documents while the policy they implement stays in `CONTRIBUTING.md`.
  - That the repair must run **after** the `pre.json` change has merged. A publish that happens while the repository still says `beta` recreates the `beta` tag and undoes the repair. This ordering was found while writing the repair document, not before it.
  - That the registry half is outstanding and needs npm credentials the agent does not have, that `npm whoami` returns 401 there, and that `@phoria/opentelemetry`'s bare install will fail until its `latest` is removed.
  - That Plan B's install lines are gated on the `canary` tag existing, and that they are uniform `@canary` for all six packages once it does.

- [ ] **Step 4: Record the close-out in the ledger.**

  Note the suite result, the outstanding maintainer action and its ordering constraint, and that this slice does not complete Phase 6.
