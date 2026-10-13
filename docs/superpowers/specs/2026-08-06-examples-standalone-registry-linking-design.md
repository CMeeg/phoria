# Design: Standalone examples with local-dev linking

Date: 2026-08-06

## Problem

Two competing goals for the `examples/` apps collided:

- **Packages want `catalog:` back.** Runtime deps in the five JS packages were
  literalized (`catalog:` → semver ranges) because `file:`-referencing examples
  broke with `[ERR_PNPM_SPEC_NOT_SUPPORTED_BY_ANY_RESOLVER]` — pnpm hard-refuses
  `catalog:` in any package consumed from outside the workspace.
- **Examples should be truly standalone.** A committed example that points at
  `file://../../../packages/...` cannot be installed without the repo, killing
  the giget-installability story.

Empirically established earlier:

- A consumer's own `catalog:` section does **not** help — pnpm rejects `catalog:`
  in external packages outright (verified).
- `pnpm link`/`link:` avoids the rejection (the linked manifest is never
  resolved by the consumer) but makes the example silently depend on the repo
  root's `node_modules` for transitive deps (verified: `empathic`,
  `@rollup/pluginutils`, `magic-string@1.1.0` were pruned from the example's own
  store and resolved through the symlink into the repo).

## Goals

1. Restore `catalog:` in the JS packages (undo the literalization).
2. Make committed examples standalone: they reference **published** versions and
   install/build with zero repo coupling.
3. Keep a frictionless local-dev loop against in-repo packages via a
   `link:`-based switch script.
4. Guard against accidental commits of dev-linked state in CI.
5. Keep examples in sync with releases automatically.

## Decisions

- Goal weighting: catalog-DRY and standalone-ness are **equally** important.
- Committed example refs are caret ranges of the last published version,
  updated automatically by the release flow (post-publish).
- The switch story covers **both JS and .NET**: committed `.csproj` uses an
  unversioned `PackageReference` (the version is a `PackageVersion` in the example's
  `Directory.Packages.props`, since examples enable Central Package Management), dev
  state uses `ProjectReference`.
- CI: grep-style guard now; a full example smoke test in CI is deferred to the
  existing "add examples to CI" item.

## Section 1 — Committed state and packages revert

- Revert the literalization in
  `packages/{phoria-islands,phoria-react,phoria-svelte,phoria-vue,vite-plugin-dotnet-dev-certs}/package.json`
  — runtime deps return to `catalog:`.
- Remove the AGENTS.md gotcha bullet ("JS package runtime dependencies must use
  literal semver ranges").
- Re-sync the root `pnpm-lock.yaml`.

`getting-started` committed state (the only example today; the tooling is
future-proofed for more):

- `WebApp/package.json`:
  - `@phoria/phoria: ^0.4.2` (dep)
  - `@phoria/phoria-react: ^0.4.2` (dep)
  - `@phoria/vite-plugin-dotnet-dev-certs: ^0.2.1` (devDep)
- `WebApp/WebApp.csproj`: `ProjectReference` → `<PackageReference Include="Phoria" />` (unversioned; CPM).
- `Directory.Packages.props` (example root): adds `<PackageVersion Include="Phoria" Version="0.4.2" />`.
- Committed `pnpm-lock.yaml` is the registry-resolved state.

Property: a fresh clone (or giget fetch) can `pnpm install && pnpm build` with
zero repo coupling. Caveat during active development: an example may reference APIs
that are not yet published (e.g. `getting-started`'s `server.ts` uses the `logger`
option, absent from published 0.4.2), so the committed state may not type-check until
the next release — this affects verification, not the design.

Version sources: each package's own `package.json` `version` field
(`packages/phoria-islands`, `packages/phoria-react`,
`packages/vite-plugin-dotnet-dev-certs`, `packages/Phoria` for the .NET package).

## Section 2 — The link/sync script

`scripts/examples.js` (Node ESM, style mirrors `scripts/dotnet/publish.js`).
Discovers examples by scanning `examples/*/WebApp` for a `package.json`. Root
scripts: `pnpm examples:link`, `pnpm examples:sync`, `pnpm examples:check`,
plus release-only `pnpm examples:bump`.

| Mode | When | Behavior |
|---|---|---|
| `link` | dev | Rewrites the 3 phoria refs → `link:../../../packages/<dir>`; `PackageReference` → `ProjectReference`; runs `pnpm install` (lockfile → link-state) + `dotnet restore`. Prints "run `pnpm build` at root first". Idempotent. |
| `sync` | dev, done | Restores phoria refs to their **HEAD** values, `git checkout -- pnpm-lock.yaml`, then `pnpm install --frozen-lockfile` to verify. Byte-identical to committed state. Warns on unexpected uncommitted edits. Idempotent. |
| `check` | CI | Exits 1 if any example has `link:`/`file:` phoria refs or a `ProjectReference`. |
| `bump` | release | Reads versions from the local package manifests, writes `^<version>` refs + an unversioned `PackageReference` + a `Directory.Packages.props` `PackageVersion`, runs non-frozen `pnpm install` to re-resolve lockfiles. Runs post-publish. |

Notes:

- `link`/`sync`/`check` need no network — `sync` restores HEAD, which is always
  resolvable.
- `sync`'s `git checkout` of the lockfile is safe because the committed lockfile
  is the registry state; a dev-added dep surfaces as a frozen-install failure
  rather than silent loss.
- The script owns the package-name → `packages/<dir>` mapping, so CI `check`,
  dev `link`, and release `bump` share one source of truth for the patterns.
- `package.json` rewrites must be Biome-conformant: the examples' own
  `biome.jsonc` configures 2-space indent (differs from the root's tabs), and
  the example `pnpm lint` checks JSON files. The script emits 2-space JSON.

## Section 3 — CI and release wiring

CI (`ci.yml`, `build-and-test` job, after install):

```yaml
- name: Check examples reference published versions
  run: pnpm examples:check
```

Release (`release.yml`) — examples are synced **post-publish** to avoid
referencing versions that are not yet published:

```yaml
- name: Sync examples to released versions
  if: ${{ steps.changesets.outputs.published == 'true' }}
  run: |
    pnpm examples:bump
    git add examples
    git commit -m "chore(examples): sync to latest phoria packages"
# existing "Push release tags" step pushes commit + tags
```

`changeset version` deliberately does not touch examples (it runs pre-publish).

## Section 4 — Docs and testing

- AGENTS.md: delete the literal-runtime-deps gotcha; rewrite the Examples
  section to describe published-ref committed state, `examples:link`/`sync`
  workflow, `examples:check` CI guard, and the post-publish release bump.
- `examples/TODO.md`: replace the deferred "file↔published toggle script" item
  (now implemented); keep e2e-conversion, giget, and CI-for-examples items.

Manual verification (this session):

1. `examples:check` passes on the committed state.
2. `examples:link` → example `pnpm install` clean; `check`/`lint`/`build` green
   against linked local packages.
3. `examples:sync` → `git status` clean (byte-identical restore);
   `examples:check` + frozen install pass.
4. Simulated `examples:bump` with real published versions → refs + lockfile
   update correctly.
5. Root: packages build; `pnpm install --frozen-lockfile` passes after the
   catalog revert.

## Out of scope / deferred

- Running examples in CI (install + build against published packages) — deferred
  to the existing "add examples to CI" item.
- Converting the three `e2e/` apps to standalone examples.
- Unit tests for `scripts/examples.js` (not part of the tested packages; covered
  by the CI guard + manual verification).
