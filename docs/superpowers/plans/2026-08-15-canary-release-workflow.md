# Canary & Release Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship Phase 4 — a `canary` branch producing changesets-prerelease `beta` builds published to npm (trusted publishing/OIDC) and NuGet, with examples synced to the published betas and the `develop`-ahead-of-`main` integration absorbed.

**Architecture:** `canary` replaces `develop` and commits `.changeset/pre.json` (`mode: "pre"`, `tag: "beta"`) + `baseBranch: "canary"` so every merged change opens a beta version PR that publishes the natural 0.x beta versions via a single shared `release.yml` (triggered on both `main` and `canary`; repo state, not branch, decides beta vs stable). npm publishing moves to OIDC trusted publishing (`id-token: write`, no `NPM_TOKEN`); `NUGET_API_KEY` is unchanged. The `examples:bump` sync lands as a PR merged by the maintainer because branch protection rejects the workflow's direct push (`GH006`).

**Tech Stack:** GitHub Actions (single `release.yml` + `ci.yml`), Changesets 2.31 prereleases, pnpm 11, npm trusted publishing (OIDC), NuGet (`scripts/dotnet/publish.js`), GitHub branch protection (web UI), docker-compose.

**Spec:** `docs/superpowers/specs/2026-08-15-canary-release-workflow-design.md` — the plan argues from the spec; the one deliberate deviation (examples-sync via PR instead of a direct `git push --follow-tags`) is the resolution of the spec's internal conflict between branch protection and the workflow's own push, and was approved by the maintainer.

## Global Constraints

- Single shared `release.yml` runs on `push` to **both** `main` and `canary`; pre.json present/absent decides beta vs stable, not the branch. npm allows exactly one trusted-publisher config per package, keyed to one workflow filename.
- `canary` replaces `develop`: feature PRs target `canary`; `main` receives only coordinated stable cuts. Nobody pushes to `main` or `canary` directly once protected.
- On `canary`: `.changeset/config.json` `baseBranch: "canary"` and committed `.changeset/pre.json` (`mode: "pre"`, `tag: "beta"`, `initialVersions` computed from current package state — never hand-written). On `main`: `baseBranch: "main"`, no pre.json.
- `release.yml` gains `permissions.id-token: write`; the `NPM_TOKEN` env var is **removed**; `NUGET_API_KEY` is unchanged. Never set `NODE_AUTH_TOKEN` — npm auto-detects GitHub OIDC during `changeset publish` and adds provenance.
- Trusted-publishing prereqs already satisfied: every JS package's `repository.url` is `git+https://github.com/CMeeg/phoria.git`; Node 24 runners bundle npm ≥ 11.5.1.
- The `examples:bump` commit lands via a **pull request** (`gh pr create`, base = the branch that published) merged by the maintainer — direct push is rejected by branch protection. Git tags are pushed directly (`--follow-tags`); branch protection covers `refs/heads/*`, not `refs/tags/*`.
- Branch protection (web UI): require a pull request, require status checks `build-and-test` + `test-browser`, block force pushes, restrict push to maintainers, admin bypass allowed ("Do not allow bypassing the above settings" left **unchecked**), **no** required approvals (GitHub never counts the PR author's own approval — would deadlock a sole maintainer).
- First beta run applies the 15 pending minor/patch changesets → natural 0.x beta versions. The `@phoria/phoria` peerers use `>=0.5.0-0 <2.0.0`, so the core's `0.5.0-beta.0` is in range and does not force framework major bumps. The `1.0.0` major changesets stay queued for Phase 10.
- npm quirk: a never-published package's first publish also takes the `latest` dist-tag — applies to `@phoria/opentelemetry`; accepted and documented.
- CI order stays `build` → `lint` → `check` → `test`, plus `dotnet test --solution Phoria.sln --configuration Release`; `pnpm examples:check` runs in `ci.yml`.
- Biome style: tabs, as-needed semicolons, no trailing commas, 120-col. C# comments are opt-in. Markdown prose: no hard wrapping.

---

### Task 1: Rehearsal of the changesets prerelease flow (throwaway worktree)

Validates the riskiest unknown (changesets prereleases) before anything is committed for real, and locks down the exact expected versions, peer rewrites, and dotnet sync that Tasks 3 and 6 assert against.

**Files:**
- Temp: git worktree at `/tmp/opencode/phoria-rehearsal` (discarded — never pushed)
- Modify: `docs/MEMORY.md` (dated entry recording findings)

**Interfaces:**
- Produces: the recorded expected values consumed by Task 6 verification (natural 0.x beta versions per package, unchanged `>=0.5.0-0 <2.0.0` peer ranges, and the dotnet `0.5.0-beta.0` sync).

- [ ] **Step 1: Create the throwaway worktree and install**

```bash
git worktree add /tmp/opencode/phoria-rehearsal HEAD
```

Run in `/tmp/opencode/phoria-rehearsal`:

```bash
pnpm install
```

Expected: install completes cleanly (shared repo-object store; fresh `node_modules` in the worktree).

- [ ] **Step 2: Enter beta prerelease mode**

```bash
pnpm changeset pre enter beta
```

Expected: `.changeset/pre.json` is created with `"mode": "pre"`, `"tag": "beta"`, the 15 pending changesets in `"changesets"`, and `"initialVersions"` for all 7 packages derived from current package state.

- [ ] **Step 3: Apply the version bumps**

```bash
pnpm run version
```

This runs `changeset version && pnpm install --no-frozen-lockfile`. Expected: versions bump to their natural beta values, changelogs are written, and the lockfile is refreshed.

- [ ] **Step 4: Inspect and record the results (beta)**

```bash
node -e 'for (const p of ["phoria-islands","phoria-react","phoria-svelte","phoria-vue","phoria-opentelemetry","vite-plugin-dotnet-dev-certs","Phoria"]) { const j=require(`./packages/${p}/package.json`); console.log(`${j.name} -> ${j.version}`) }'
node -e 'for (const p of ["phoria-react","phoria-svelte","phoria-vue","phoria-opentelemetry"]) { const j=require(`./packages/${p}/package.json`); console.log(`${j.name} peer @phoria/phoria:`, j.peerDependencies["@phoria/phoria"]) }'
git status --short
```

Expected: `@phoria/phoria` and `@phoria/phoria-react` → `0.5.0-beta.0`; `@phoria/phoria-svelte` and `@phoria/phoria-vue` → `0.4.0-beta.0`; `@phoria/opentelemetry` → `0.2.0-beta.0`; `@phoria/vite-plugin-dotnet-dev-certs` → `0.3.0-beta.0`; and `phoria-dotnet` → `0.5.0-beta.0`. All four peer ranges remain `>=0.5.0-0 <2.0.0` because the core beta is in range, so no `1.0.0-beta.0` cascade occurs. Record the exact observed values.

- [ ] **Step 5: Rehearse the stable-cut mechanics (runbook steps 1–2)**

Reset the worktree to HEAD (discard the beta run), then simulate the exit path with the original changesets still pending:

```bash
git checkout HEAD -- . && git clean -fd .changeset
pnpm changeset pre enter beta
pnpm changeset pre exit
pnpm run version
node -e 'for (const p of ["phoria-islands","phoria-react","phoria-vue"]) { const j=require(`./packages/${p}/package.json`); console.log(`${j.name} -> ${j.version}`) }'
```

Expected: `pre exit` removes `pre.json` cleanly, and `changeset version` now produces **stable** bumps (`0.5.0`, `0.4.0`, …) with peers unchanged at `>=0.5.0-0 <2.0.0` — validating that the canary→main cut produces stable releases and that the window between `pre exit` and `pre enter beta` is quiescent. Record the observed stable versions.

- [ ] **Step 6: Discard the worktree**

```bash
git worktree remove /tmp/opencode/phoria-rehearsal --force
git worktree prune
```

Expected: no worktree remains; the working repo is untouched (verify `git status` is clean at the repo root).

- [ ] **Step 7: Commit the rehearsal findings to `docs/MEMORY.md`**

Add a dated entry (`## 2026-08-15 — Phase 4 rehearsal (canary prerelease flow)`) capturing: the exact beta versions per package, the unchanged peer range, the dotnet version sync, the stable-cut mechanics result, and the pre.json shape. Then:

```bash
git add docs/MEMORY.md
git commit -m "docs: record canary prerelease rehearsal findings"
```

Commit lands on the current branch (`feature/canary-workflow`); Task 2 creates `canary` from this HEAD so it rides along.

### Task 2: Branch model — create `canary`, delete `develop`, update `ci.yml`

**Files:**
- Modify: `.github/workflows/ci.yml:6-12` (the two `- develop` trigger entries — one under `push.branches`, one under `pull_request.branches` — become `- canary`)

**Interfaces:**
- Consumes: Task 1's commit on `feature/canary-workflow` HEAD.
- Produces: `canary` branch on origin, `develop` gone, CI runs on `canary`.

- [ ] **Step 1: Create and switch to `canary`**

```bash
git branch canary
git checkout canary
```

`canary` is created at the current HEAD (`develop` + the committed Phase-4 docs + the Task 1 MEMORY entry), not at bare `develop` HEAD — the docs and rehearsal findings ride along. (Sequencing note: this deviation from the spec's "at develop HEAD" is recorded in MEMORY in Task 4.)

- [ ] **Step 2: Update `ci.yml` triggers**

In `.github/workflows/ci.yml`, change both `- develop` entries (push block and pull_request block) to `- canary`. Resulting trigger blocks:

```yaml
on:
  workflow_dispatch:
  push:
    branches:
      - main
      - canary
  pull_request:
    branches:
      - main
      - canary
```

- [ ] **Step 3: Verify, commit, and push**

```bash
git grep -n develop -- .github/workflows/ci.yml || echo "no develop refs"
git add .github/workflows/ci.yml
git commit -m "ci: run CI on canary instead of develop"
git push -u origin canary
```

Expected: grep finds no `develop` in `ci.yml`; push succeeds.

- [ ] **Step 4: Delete `develop` (local and remote) and the absorbed feature branch**

```bash
git branch -D develop
git push origin --delete develop
git remote prune origin
git branch -D feature/canary-workflow
```

- [ ] **Step 5: Verify the new branch model**

```bash
git branch -a
git log --oneline canary -5
```

Expected: only `main` and `canary` remain (`origin/HEAD -> origin/main`); `canary` history shows the Phase-4 docs, the MEMORY rehearsal entry, and the ci.yml commit. GitHub's default branch stays `main`.

### Task 3: Changesets prerelease setup on `canary`

**Files:**
- Modify: `.changeset/config.json:4` (`"baseBranch": "main"` → `"baseBranch": "canary"`)
- Create: `.changeset/pre.json` (generated by `pnpm changeset pre enter beta`)

**Interfaces:**
- Consumes: Task 2's `canary`.
- Produces: `.changeset/pre.json` consumed by `changesets/action` in `release.yml` (Task 4) to produce beta version PRs and beta publishes.

- [ ] **Step 1: Point the branch-resolved changesets config at `canary`**

In `.changeset/config.json`, change line 4 to:

```json
	"baseBranch": "canary",
```

`main`'s committed copy stays `"baseBranch": "main"` — each branch resolves its own config, which is how the stable and beta streams coexist in one workflow.

- [ ] **Step 2: Enter beta prerelease mode**

```bash
pnpm changeset pre enter beta
```

Expected: `.changeset/pre.json` created (no hand-written version list — changesets computes `initialVersions` from package state).

- [ ] **Step 3: Inspect the generated pre.json**

```bash
node -e 'const p=require("./.changeset/pre.json"); console.log("mode:", p.mode); console.log("tag:", p.tag); console.log("changesets:", p.changesets.length); console.log("initialVersions:", JSON.stringify(p.initialVersions, null, 1))'
```

Expected: `mode: pre`, `tag: beta`, 15 changesets, and `initialVersions` for all 7 packages. Any discrepancy (wrong count, wrong tag) means Task 1's rehearsal didn't reflect reality — stop and reconcile before committing.

- [ ] **Step 4: Commit and push**

```bash
git add .changeset/config.json .changeset/pre.json
git commit -m "chore: enter changesets beta prerelease mode on canary"
git push
```

### Task 4: Single shared `release.yml` with OIDC trusted publishing and PR-based examples sync

**Files:**
- Modify: `.github/workflows/release.yml` (full rewrite)
- Modify: `CONTRIBUTING.md:70` and `CONTRIBUTING.md:77`
- Modify: `docs/ARCHITECTURE.md` (the `### The beta stream` and `### Stable-cut runbook` paragraphs)
- Modify: `docs/MEMORY.md` (dated entry: GH006 finding, PR decision, single-workflow note, branch-creation deviation)

**Interfaces:**
- Consumes: `.changeset/pre.json` + `baseBranch: "canary"` (Task 3).
- Produces: the workflow whose run on the Task 4 push opens the first beta version PR (Task 6).

- [ ] **Step 1: Rewrite `release.yml`**

Replace the whole file with:

```yaml
name: Release

on:
  push:
    branches:
      - main
      - canary

concurrency: ${{ github.workflow }}-${{ github.ref }}

jobs:
  release:
    name: Release
    runs-on: ubuntu-latest
    permissions:
      contents: write       # to create releases and push the examples-sync branch (changesets/action)
      issues: write          # to post issue comments (changesets/action)
      pull-requests: write   # to create the version PR and the examples-sync PR
      id-token: write        # npm trusted publishing (OIDC) during changeset publish
    steps:
      - uses: actions/checkout@v7

      - name: Setup pnpm
        uses: pnpm/action-setup@v6

      - name: Setup Node
        uses: actions/setup-node@v7
        with:
          node-version-file: ".nvmrc"
          cache: "pnpm"

      - name: Setup dotnet
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: "./global.json"

      - name: Install dependencies
        run: pnpm install

      - name: Build Packages
        run: pnpm build

      - name: Create Release Pull Request or Publish
        id: changesets
        uses: changesets/action@v1.9.0
        with:
          version: pnpm run version
          commit: "chore: bump version"
          title: "chore: release"
          publish: pnpm run publish
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}

      - name: Push release tags
        if: steps.changesets.outputs.published == 'true'
        run: git push --follow-tags
        shell: bash

      - name: Open examples-sync pull request
        if: steps.changesets.outputs.published == 'true'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          git push origin --delete chore/examples-sync 2>/dev/null || true
          git config user.name "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
          pnpm examples:bump
          git checkout -b chore/examples-sync
          git add examples
          git commit -m "chore(examples): sync to latest phoria packages"
          git push -u origin chore/examples-sync
          gh pr create --base "${GITHUB_REF_NAME}" --head chore/examples-sync \
            --title "chore(examples): sync to latest phoria packages" \
            --body "Automated sync of the example dependencies to the just-released phoria versions."
```

Key points vs. the old file: `push.branches` now `[main, canary]`; `permissions` gains `id-token: write`; `NPM_TOKEN` env is gone (npm CLI auto-detects GitHub OIDC during `changeset publish` — provenance is added automatically; do not reintroduce any npm token); `NUGET_API_KEY` unchanged. The `Push release tags` step runs on both branches and now pushes **tags only** — the branch ref is already up to date at the version-PR merge (`changeset publish` makes no commits with `commit: false`), and tags aren't branch-protected. The examples sync is committed to a `chore/examples-sync` branch and opened as a PR (`gh` is preinstalled on GitHub runners) because branch protection rejects the workflow's direct push with `GH006`.

- [ ] **Step 2: Validate the workflow file**

```bash
npx --yes actionlint .github/workflows/release.yml
```

Expected: no diagnostics. (If offline, fall back to a manual read-through against the schema — `on.push.branches`, `permissions`, and the `gh` step.)

- [ ] **Step 3: Update `CONTRIBUTING.md`**

Edit the `### Beta stream (canary)` paragraph (line 70) to end: "...and a matching beta to NuGet, then opens a "Sync examples" pull request that updates the examples to the released beta versions — merge it too. Betas are safe to consume for integration and production testing of work in progress."

Edit the stable-release runbook step 2 (line 77) to: "2. Merge `canary` into `main`. The `main` release workflow opens a stable "Version Packages" pull request; merging it publishes the stable versions (npm `latest`, NuGet), pushes release tags, and opens a "Sync examples" pull request syncing the examples to stable refs — merge that too."

- [ ] **Step 4: Update `docs/ARCHITECTURE.md`**

In `### The beta stream`, change "then syncs the examples (`pnpm examples:bump`) to the released versions" to "then opens a "Sync examples" pull request updating the examples (`pnpm examples:bump`) to the released versions, which the maintainer merges — the branches are branch-protected, so the workflow cannot push directly". Adjust the `### Stable-cut runbook` paragraph ("merge it to publish, sync examples, and push tags") the same way.

- [ ] **Step 5: Record the decisions in `docs/MEMORY.md`**

Add a dated entry (`## 2026-08-15 — Phase 4 execution (canary release workflow)`) recording: the GH006 finding (branch protection rejects the workflow's direct `git push`; the classic "allow specified actors to bypass required pull requests" only skips the PR requirement, not required status checks) and the approved resolution (examples sync lands as a `gh`-created PR merged by the maintainer); the single-workflow/`id-token`/no-`NPM_TOKEN` shape; and the canary-created-at-HEAD (not bare `develop` HEAD) deviation.

- [ ] **Step 6: Commit and push**

```bash
git add .github/workflows/release.yml CONTRIBUTING.md docs/ARCHITECTURE.md docs/MEMORY.md
git commit -m "ci: single shared release workflow with npm trusted publishing"
git push
```

Expected: the push triggers the rewritten `release.yml` on `canary` → `changesets/action` sees the 15 pending changesets + `pre.json` → opens the beta "Version Packages" PR. This is the first canary run beginning; Task 6 takes it from here. Nothing is published yet (only a version PR is opened).

### Task 5: Manual rollout — branch protection and npm trusted publishers (maintainer, web UI)

Cannot be automated here (`gh` unavailable; npm settings are web UI). Do both before Task 6. Can be done in parallel with Tasks 2–4.

**Files:** none (GitHub Settings + npmjs.com). A human maintainer performs this task.

- [ ] **Step 1: Branch protection on `main` and `canary`**

GitHub → Settings → Branches → Add rule, once for `main`, once for `canary`:

- **Branch name pattern:** `main` / `canary`
- **Require a pull request before merging:** on (this is what blocks the workflow's direct push — the examples sync therefore goes through a PR)
- **Require status checks to pass before merging:** on, requiring `build-and-test` and `test-browser`
- **Block force pushes:** on
- **Restrict who can push to matching branches:** maintainers
- **Do not allow bypassing the above settings:** **left unchecked** (admin bypass = emergency escape hatch on a solo repo)
- **Required number of approvals:** leave off — GitHub never counts the PR author's own approval, so requiring approvals would deadlock a sole maintainer; enable when a second active maintainer exists.

- [ ] **Step 2: Configure the six npm trusted publishers**

For each of `@phoria/phoria`, `@phoria/phoria-react`, `@phoria/phoria-svelte`, `@phoria/phoria-vue`, `@phoria/opentelemetry`, `@phoria/vite-plugin-dotnet-dev-certs` on npmjs.com: package → Settings → Access → "Add new publisher" → organization `CMeeg`, repository `phoria`, workflow file `release.yml`, allowed action `npm publish`. npm allows exactly one trusted-publisher config per package — the source of the single-workflow constraint. No npm token is stored anywhere.

- [ ] **Step 3: Confirm prereqs**

Prereqs are already verified: every package's `repository.url` is `git+https://github.com/CMeeg/phoria.git`, and the Node 24 runner bundles npm ≥ 11.5.1. Nothing to do unless a new package is added later (then it needs its own trusted publisher).

### Task 6: First canary run + end-to-end verification

**Files:** none new — verification only, plus a `docs/MEMORY.md` close-out entry.

- [ ] **Step 1: Merge the beta version PR**

Task 4's push opened the beta "Version Packages" PR on `canary` (title `chore: release`). Review and merge it.

Expected: the merge triggers `release.yml` → `changesets/action` sees no pending changesets and the `chore: bump version` commit → runs `publish` (`changeset publish` → npm betas with the `beta` dist-tag; `pnpm --filter phoria-dotnet run publish` → `dotnet pack -p:Version=0.5.0-beta.N` → NuGet push) → pushes release tags → opens the "Sync examples" PR. No version PR is opened for the examples-sync merge later (its commit message doesn't match `chore: bump version`), so there's no publish loop.

- [ ] **Step 2: Merge the "Sync examples" PR**

Review and merge it. The `build-and-test` + `test-browser` checks run on it (approval-required since it was created by the token) and must pass — the required-checks gate from Task 5.

- [ ] **Step 3: Verify the npm beta dist-tags**

```bash
npm view @phoria/phoria dist-tags --json
npm view @phoria/phoria-react dist-tags --json
npm view @phoria/phoria-svelte dist-tags --json
npm view @phoria/phoria-vue dist-tags --json
npm view @phoria/vite-plugin-dotnet-dev-certs dist-tags --json
npm view @phoria/opentelemetry dist-tags --json
```

Expected: every package shows `beta: 0.5.0-beta.N` (exact base per Task 1's recorded values — svelte/vue/opentelemetry/dev-certs may show lower majors if their pending bumps are smaller). `@phoria/opentelemetry` additionally shows `latest` set — the first-publish quirk, accepted.

- [ ] **Step 4: Verify the NuGet beta**

```bash
dotnet package search Phoria --prerelease --take 10
```

Expected: `Phoria` `0.5.0-beta.N` present on nuget.org.

- [ ] **Step 5: Verify the docker blocker is resolved**

```bash
cd examples/getting-started && docker compose build
```

Expected: the build succeeds — `pnpm install --frozen-lockfile` now resolves `@phoria/opentelemetry@^0.5.0-beta.N` from the registry (the previously unpublished-`0.1.0` blocker). This is the PROJECT.md docker success criterion.

- [ ] **Step 6: Verify examples reference the published betas**

```bash
pnpm examples:check
git status --short
git grep -n "opentelemetry" examples/*/WebApp/package.json
```

Expected: `examples:check` passes (registry ranges, no `file:`/`link:` refs); working tree clean after the examples-sync merge; examples reference `^0.5.0-beta.N` including `@phoria/opentelemetry`.

- [ ] **Step 7: Verify CI stays green on `canary`**

Expected: the two required checks pass on the examples-sync PR and on the current `canary` HEAD — `build` → `lint` → `check` → `test` → `dotnet test --solution Phoria.sln --configuration Release` → `test:browser`.

- [ ] **Step 8: Record the close-out in `docs/MEMORY.md` and commit**

Add a dated entry (`## 2026-08-15 — Phase 4 close-out (first canary run)`) recording the observed dist-tags, the NuGet beta version, the docker-compose result, and any deviations. Commit on `canary`:

```bash
git add docs/MEMORY.md
git commit -m "docs: record first canary beta release"
git push
```

(Note: this push triggers `release.yml` again, which finds no pending changesets and does nothing — the expected quiescent behavior. Confirm it on the Actions tab.)

---

## Self-review

**Spec coverage:** Rehearsal → Task 1. Branch model + ci.yml → Task 2. Changesets prerelease setup → Task 3. Single shared release.yml + OIDC + examples sync → Task 4. Branch protection + trusted publishers → Task 5. First canary run + verification → Task 6. Docs deliverables (CONTRIBUTING, ARCHITECTURE, MEMORY, PROJECT, spec) were already committed on `feature/canary-workflow` and are absorbed by `canary`; Tasks 1/4/6 update MEMORY/ARCHITECTURE/CONTRIBUTING where the approved PR-based examples sync changes the story. Staged publishing is out of scope per spec (deferred). Stable-cut runbook is rehearsed (Task 1, Phase B) and documented, with the first live stable cut deferred to release time.

**Placeholder scan:** No TBD/TODO steps; every step has exact commands/content; expected values not hard-coded where the rehearsal determines them (by design — Task 1 records them and later tasks reference the record).

**Type consistency:** The workflow's `examples:bump` reads versions from the post-version-PR package.jsons (`0.5.0-beta.N`), writes `^0.5.0-beta.N` example refs and the `Phoria` `PackageVersion` in `Directory.Packages.props`, matching `scripts/examples.js bump` and the dotnet `-p:Version=` publish path. The `gh pr create` base uses `GITHUB_REF_NAME` (the publishing branch), matching the "both branches" requirement.

**Sequencing decisions (also recorded in MEMORY via Tasks 1/4):** rehearsal precedes any config change (validates the flow against current state and locks expected versions); branch protection + trusted publishers are grouped as one manual task ordered before the first run (canary stays unpublishable until then, so no security gap from deferring protection until after setup); `canary` is created at HEAD rather than bare `develop` HEAD so the Phase-4 docs + rehearsal findings ride along; examples sync is a PR, not a direct push, because GH006 would otherwise fail the first run.
