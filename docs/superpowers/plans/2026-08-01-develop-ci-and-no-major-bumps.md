# Develop CI + No Premature Major Bumps Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run the CI workflow on `develop` (without triggering a release) and stop changesets from releasing `1.0.0` until v1 prep, then commit everything as a single commit.

**Architecture:** Add `develop` to the CI triggers and a `changeset status` presence check (with a full-history checkout so `git merge-base main HEAD` works). Widen the framework packages' `@phoria/phoria` peer range from `~0.4.0` to `>=0.4.0 <1.0.0` so the release plan produces no major bumps. `release.yml` stays unchanged — only pushes to `main` release. Document the peer-range reconciliation in `docs/PROJECT.md` and `AGENTS.md`.

**Tech Stack:** GitHub Actions (YAML), pnpm + Changesets, npm `package.json` metadata.

## Global Constraints

- Working on branch `feature/upgrade` (git flow: feature → PR → `develop` → PR → `main`; branch/PR mechanics are done by the user).
- All changes committed as a **single commit** at the end (per user request).
- `release.yml` must remain untouched — `develop` must never trigger a release.
- The changeset status check must not fail on pushes to `main` (there `baseBranch == HEAD`, so nothing is "changed").
- Peer range change requires updating `pnpm-lock.yaml` (`pnpm install --no-frozen-lockfile`).
- Peer range change to a publishable package requires a changeset.
- Biomes formats `package.json` with tabs; use `pnpm biome check --write` to match repo style.
- CI order (verify at end): `pnpm build` → `pnpm lint` → `pnpm check` → `pnpm test`.

---

### Task 1: Add `develop` to CI triggers and add a changeset presence check

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: CI runs on push/PR to `develop`; `build-and-test` job fails if a versionable package changed without a changeset.

- [ ] **Step 1: Add `develop` to push and pull_request triggers**

In `.github/workflows/ci.yml`, change the `on:` block (lines 3-10):

```yaml
on:
  workflow_dispatch:
  push:
    branches:
      - main
      - develop
  pull_request:
    branches:
      - main
      - develop
```

- [ ] **Step 2: Full-history checkout for the `build-and-test` job**

`changeset status` computes changed packages via `git merge-base <baseBranch> HEAD`, which requires `main` to exist in the clone. Add `fetch-depth: 0` to the `Checkout` step of the `build-and-test` job (line 23-24):

```yaml
      - name: Checkout
        uses: actions/checkout@v7
        with:
          fetch-depth: 0
```

Only the `build-and-test` job needs this (the changeset check lives there); leave `test-browser` as-is.

- [ ] **Step 3: Add the changeset presence check step**

Insert after the `Install dependencies` step (line 42-43) in `build-and-test`:

```yaml
      - name: Check changesets
        run: pnpm exec changeset status
```

`changeset status` exits 1 (failing the job) when versionable packages changed since `main` but no changeset exists (see `@changesets/cli` source, `status()`). On `main` pushes `baseBranch == HEAD`, so the diff is empty and it passes trivially.

- [ ] **Step 4: Sanity-check the workflow YAML**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/ci.yml'))"`
Expected: no output, exit 0.

---

### Task 2: Widen the `@phoria/phoria` peer range in framework packages

**Files:**
- Modify: `packages/phoria-react/package.json` (line 86)
- Modify: `packages/phoria-svelte/package.json` (line 79)
- Modify: `packages/phoria-vue/package.json` (line 79)
- Modify: `pnpm-lock.yaml` (regenerated)

**Interfaces:**
- Produces: each framework package peer-deps `@phoria/phoria` at `>=0.4.0 <1.0.0`, so `changeset version` no longer cascades a major bump when `@phoria/phoria` moves to `0.5.0`.

- [ ] **Step 1: Update the three `package.json` files**

In each of `packages/phoria-react/package.json`, `packages/phoria-svelte/package.json`, and `packages/phoria-vue/package.json`, change the `peerDependencies` entry:

```json
"@phoria/phoria": ">=0.4.0 <1.0.0"
```

(from `~0.4.0`).

- [ ] **Step 2: Regenerate the lockfile**

Run: `pnpm install --no-frozen-lockfile`
Expected: `pnpm-lock.yaml` updates the peer-range specifiers for `@phoria/phoria-react`, `@phoria/phoria-svelte`, `@phoria/phoria-vue`.

---

### Task 3: Add the peer-range widening changeset

**Files:**
- Create: `.changeset/widen-phoria-peer-range.md`

**Interfaces:**
- Produces: a `patch` changeset for the three framework packages so `changeset status` passes and the metadata change is released.

- [ ] **Step 1: Create the changeset**

Create `.changeset/widen-phoria-peer-range.md`:

```markdown
---
"@phoria/phoria-react": patch
"@phoria/phoria-svelte": patch
"@phoria/phoria-vue": patch
---

Widen the `@phoria/phoria` peer dependency range to `>=0.4.0 <1.0.0` to keep pre-1.0 releases non-breaking.
```

- [ ] **Step 2: Verify the release plan has no majors**

Run: `pnpm exec changeset status --output /tmp/opencode/release-plan.json`
Then: `node -e "const p=require('/tmp/opencode/release-plan.json'); const m=p.releases.filter(r=>r.type==='major'); if(m.length){console.error('MAJOR:', m.map(r=>r.name+':'+r.newVersion)); process.exit(1)} console.log('No majors. Plan:'); p.releases.filter(r=>r.type!=='none').forEach(r=>console.log(' ', r.name, r.oldVersion+'->'+r.newVersion, '('+r.type+')'))"`
Expected: exit 0, output like `@phoria/phoria 0.4.2->0.5.0 (minor)` and **no** `1.0.0` targets.

---

### Task 4: Document the v1 peer-range reconciliation

**Files:**
- Modify: `docs/PROJECT.md`
- Modify: `AGENTS.md`

- [ ] **Step 1: Add a Phase 5 note to `docs/PROJECT.md`**

In the Phase 5 bullet (line 116-117), extend it with the peer-range reconciliation note:

```markdown
5. **Release prep** — version reconciliation to `1.0.0`, docs pass, changesets,
   GitHub milestone/issues, inline-TODO cleanup. NOTE: framework peer ranges on
   `@phoria/phoria` are widened to `>=0.4.0 <1.0.0` during pre-1.0 (to prevent
   premature `1.0.0` releases via the changesets peer cascade); tighten them to
   `^1.0.0` as part of this phase.
```

- [ ] **Step 2: Update the `AGENTS.md` peer-dependency gotcha**

Replace the existing bullet:

```markdown
- **Peer dependencies matter**: framework packages peer-depend on `@phoria/phoria` at `>=0.4.0 <1.0.0` (widened from `~0.4.0` to prevent premature `1.0.0` releases via the changesets peer cascade) — version bumps need care. This must be reconciled when all packages reach `1.0.0` (Phase 5).
```

---

### Task 5: Verify and commit as a single commit

- [ ] **Step 1: Format the edited `package.json`/markdown files**

Run: `pnpm biome check --write .github/workflows/ci.yml packages/phoria-react/package.json packages/phoria-svelte/package.json packages/phoria-vue/package.json AGENTS.md docs/PROJECT.md docs/superpowers/plans/2026-08-01-develop-ci-and-no-major-bumps.md`
Expected: no errors (biome may rewrite formatting; check the diff is only whitespace).

- [ ] **Step 2: Run the full verification pipeline**

Run: `pnpm build && pnpm lint && pnpm check && pnpm test`
Expected: all green.

- [ ] **Step 3: Re-run the changeset status check**

Run: `pnpm exec changeset status`
Expected: prints the release plan with no major bumps; exit 0.

- [ ] **Step 4: Inspect and commit**

Run: `git status && git diff --stat`
Expected: only the intended files changed (`.github/workflows/ci.yml`, three `packages/*/package.json`, `pnpm-lock.yaml`, `.changeset/widen-phoria-peer-range.md`, `docs/PROJECT.md`, `AGENTS.md`, this plan).

Run: `git add -A && git commit -m "ci: run checks on develop and prevent premature major releases"`
Expected: commit succeeds.
