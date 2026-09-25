# Canary Release Workflow Reconciliation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the approved Phase 4 release workflow by making branch identity, tag publication, and examples synchronization safe across `canary` and `main`.

**Architecture:** The existing `canary` branch and beta prerelease state remain the source of beta releases. A single shared workflow normalizes Changesets configuration from `GITHUB_REF_NAME`, pushes tags without branch refs, and creates a unique examples-sync PR branch per release. External branch protection, npm trusted publishers, publishing, and registry verification remain explicit maintainer steps.

**Tech Stack:** GitHub Actions, Changesets 2.31.1, pnpm 11, npm trusted publishing/OIDC, NuGet, GitHub CLI.

**Spec:** `docs/superpowers/specs/2026-08-15-canary-release-workflow-design.md`

## Current Baseline

- Task 1 reconciliation is recorded through commit `1812944`.
- Task 2 local branch/CI work is committed as `09c5acf`; local `develop` and `feature/canary-workflow` refs were safely deleted; remote refs are unchanged.
- Task 3 committed `baseBranch: "canary"` and generated `.changeset/pre.json` as `6881157`.
- Task 4's initial workflow draft is `5a3b1c3`; this plan replaces its unsafe seams.
- The current branch is `canary`, and the current `.changeset/pre.json` is generated state with `mode: "pre"`, `tag: "beta"`, seven `initialVersions`, and an empty `changesets` array until versioning runs.

## Global Constraints

- One `release.yml` runs on pushes to both `main` and `canary`.
- Before Changesets runs, `.changeset/config.json.baseBranch` is normalized to `GITHUB_REF_NAME`.
- The first beta peer range is `>=0.5.0-0 <2.0.0`; later beta cycles update the lower-bound tuple before versioning.
- npm publishing uses `id-token: write` and no `NPM_TOKEN` or `NODE_AUTH_TOKEN`; NuGet continues using `NUGET_API_KEY`.
- Published tags are pushed explicitly without pushing a protected branch ref.
- Examples sync uses `chore/examples-sync-<branch>-<commit>` and never deletes or overwrites an existing examples branch.
- Build failure prevents Changesets; publish failure prevents tag/examples steps; examples-sync failure cannot republish packages.
- External pushes, merges, branch protection, npm trusted publisher configuration, package publishing, and registry-dependent verification require maintainer action.

---

### Task 1: Make the shared release workflow branch-safe

**Files:**
- Modify: `.github/workflows/release.yml`
- Test: `.github/workflows/release.yml` via `actionlint` when available and manual YAML review otherwise

**Interfaces:**
- Consumes: local `canary` branch, generated `.changeset/pre.json`, `changesets/action@v1.9.0`, and `GITHUB_REF_NAME`.
- Produces: a workflow that uses the correct Changesets base branch, pushes tags only, and creates collision-free examples-sync PRs.

- [ ] **Step 1: Add runtime base-branch normalization**

Insert this step after dependency installation/build and before `changesets/action`:

```yaml
      - name: Normalize Changesets base branch
        env:
          RELEASE_BRANCH: ${{ github.ref_name }}
        run: |
          node --input-type=module <<'NODE'
          import fs from "node:fs"

          const path = ".changeset/config.json"
          const config = JSON.parse(fs.readFileSync(path, "utf8"))
          const branch = process.env.RELEASE_BRANCH

          if (config.baseBranch !== branch) {
            config.baseBranch = branch
            fs.writeFileSync(path, `${JSON.stringify(config, null, "\t")}\n`)
          }
          NODE
```

Expected: a canary run keeps `baseBranch: "canary"`; a main run repairs a merged canary config to `baseBranch: "main"` before the version PR is created.

- [ ] **Step 2: Replace the branch-and-tag push**

Replace the existing tag step command:

```yaml
      - name: Push release tags
        if: steps.changesets.outputs.published == 'true'
        run: git push origin --tags
        shell: bash
```

Expected: the command pushes tags only and never pushes `HEAD` or a protected branch ref.

- [ ] **Step 3: Use a release-specific examples branch**

Replace the examples-sync shell body with this shape:

```yaml
      - name: Open examples-sync pull request
        if: steps.changesets.outputs.published == 'true'
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          EXAMPLES_BRANCH: chore/examples-sync-${{ github.ref_name }}-${{ github.sha }}
        run: |
          if git ls-remote --exit-code --heads origin "$EXAMPLES_BRANCH" >/dev/null 2>&1; then
            echo "Examples sync branch already exists: $EXAMPLES_BRANCH"
            exit 1
          fi

          git config user.name "github-actions[bot]"
          git config user.email "41898282+github-actions[bot]@users.noreply.github.com"
          pnpm examples:bump
          git add examples

          if git diff --cached --quiet; then
            echo "Examples are already synchronized"
            exit 0
          fi

          git commit -m "chore(examples): sync to latest phoria packages"
          git checkout -b "$EXAMPLES_BRANCH"
          git push -u origin "$EXAMPLES_BRANCH"
          gh pr create --base "${GITHUB_REF_NAME}" --head "$EXAMPLES_BRANCH" \
            --title "chore(examples): sync to latest phoria packages" \
            --body "Automated sync of the example dependencies to the just-released phoria versions."
```

Expected: no fixed branch deletion, no overwrite of an existing branch, no empty commit, and a PR base matching the publishing branch.

- [ ] **Step 4: Validate the workflow**

Run:

```bash
npx --yes actionlint .github/workflows/release.yml
git diff --check
```

Expected: actionlint reports no diagnostics; if unavailable, manually verify `on.push.branches`, expression interpolation, permissions, shell quoting, and step conditions, then record the unavailable-tool limitation.

- [ ] **Step 5: Commit the workflow seam**

```bash
git add .github/workflows/release.yml
git commit -m "ci: make shared release workflow branch-safe"
```

### Task 2: Align public release documentation with the workflow seam

**Files:**
- Modify: `CONTRIBUTING.md`
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/MEMORY.md`

**Interfaces:**
- Consumes: Task 1's runtime normalization and release-specific branch behavior.
- Produces: stable-cut instructions that restore `baseBranch: "canary"` after merging `main` back, and documentation of tag-only publishing and examples PR ownership.

- [ ] **Step 1: Update the beta stream documentation**

State that the workflow opens a release-specific examples-sync PR and never deletes a fixed branch. State that the workflow normalizes `baseBranch` from `GITHUB_REF_NAME` before Changesets.

- [ ] **Step 2: Update the stable-cut runbook**

Use this sequence:

```text
1. On canary, exit beta mode and commit with baseBranch: "canary".
2. Merge canary into main; the workflow normalizes baseBranch to main before opening the stable version PR.
3. Merge the stable version PR, then merge main back into canary.
4. Restore baseBranch: "canary", enter beta mode, and commit config plus pre.json.
5. Merge the release-specific examples-sync PR separately.
```

- [ ] **Step 3: Record the final architecture decision**

Append a dated `docs/MEMORY.md` entry containing the runtime normalization choice, unique examples branch choice, tag-only push, and failure boundaries. Do not claim that external branch protection or publishing was performed locally.

- [ ] **Step 4: Verify documentation consistency**

Run:

```bash
git grep -n "git push --follow-tags" -- .github/workflows/release.yml CONTRIBUTING.md docs/ARCHITECTURE.md || true
git grep -n "chore/examples-sync" -- .github/workflows/release.yml CONTRIBUTING.md docs/ARCHITECTURE.md
git diff --check
```

Expected: no active documentation describes the old fixed-branch deletion or branch-pushing tag command.

- [ ] **Step 5: Commit documentation**

```bash
git add CONTRIBUTING.md docs/ARCHITECTURE.md docs/MEMORY.md
git commit -m "docs: document branch-safe release flow"
```

### Task 3: Maintainer-only security and branch rollout

**Files:** none; GitHub and npm web configuration

- [ ] **Step 1: Apply branch protection**

On GitHub, configure `main` and `canary` to require pull requests and the `build-and-test` and `test-browser` checks, block force pushes, restrict pushes to maintainers, leave admin bypass enabled, and require no approvals for the sole-maintainer workflow.

- [ ] **Step 2: Configure npm trusted publishers**

For each of the six public JS packages, configure organization `CMeeg`, repository `phoria`, workflow file `release.yml`, and allowed action `npm publish`. Do not create or store an npm token.

- [ ] **Step 3: Confirm external configuration**

Verify the required branch rules and each package's trusted-publisher entry in the GitHub/npm web UIs. Record any unavailable action as a maintainer blocker; do not simulate it locally.

### Task 4: First canary release verification

**Files:**
- Modify: `docs/MEMORY.md` with the close-out entry after the live run

- [ ] **Step 1: Push and merge the canary setup**

The maintainer pushes local `canary`, deletes the remote `develop` branch, and merges the required setup/version PRs only after Task 3 is complete.

- [ ] **Step 2: Merge the beta version PR**

Confirm the workflow opens the beta version PR, review it, and merge it. Verify the publish run opens a release-specific examples-sync PR.

- [ ] **Step 3: Merge the examples-sync PR**

Run required CI checks on the generated PR, review the changed registry refs, and merge it independently from package publishing.

- [ ] **Step 4: Verify published artifacts**

Run:

```bash
npm view @phoria/phoria dist-tags --json
npm view @phoria/phoria-react dist-tags --json
npm view @phoria/phoria-svelte dist-tags --json
npm view @phoria/phoria-vue dist-tags --json
npm view @phoria/opentelemetry dist-tags --json
npm view @phoria/vite-plugin-dotnet-dev-certs dist-tags --json
dotnet package search Phoria --prerelease --take 10
```

Expected: beta dist-tags point to the recorded natural 0.x beta versions; `@phoria/opentelemetry` may also receive `latest` on its first npm publish; NuGet contains the matching `phoria-dotnet` beta.

- [ ] **Step 5: Verify examples and Docker**

Run:

```bash
pnpm examples:check
docker compose -f examples/getting-started/docker-compose.yml build
```

Expected: examples contain registry beta refs, no `file:`/`link:` refs, and the Docker install resolves the published OpenTelemetry beta.

- [ ] **Step 6: Record close-out**

Append `## 2026-08-15 — Phase 4 close-out (first canary run)` to `docs/MEMORY.md` with observed npm tags, NuGet version, Docker result, examples PR, and deviations. Commit and push only as a maintainer after verifying the canary branch.

## Verification Matrix

- Local workflow/config validation: Task 1 Step 4.
- Documentation consistency: Task 2 Step 4.
- Package tests and checks: run `pnpm build`, `pnpm lint`, `pnpm check`, and `pnpm test` before the maintainer rollout.
- External branch protection, trusted publishers, publish, Docker registry resolution, and final CI: Tasks 3–4 only.

## Self-review

- The runtime normalization seam covers the stable-cut merge hazard.
- The release-specific branch covers cross-stream and existing-PR collisions without destructive cleanup.
- Tag publication is explicitly branch-safe.
- External side effects are isolated to maintainer-only tasks.
