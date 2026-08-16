# Design: Replace Lerna + Nx with Turborepo

**Date:** 2026-07-31
**Status:** Approved
**Decision:** Migrate task orchestration from Lerna (with its bundled Nx engine) to Turborepo, keeping pnpm for workspace/install management and Changesets for versioning/publishing.

## Background

Phoria's monorepo currently uses three overlapping tools:

- **pnpm** — workspace and install management (the actual owner of the dependency graph)
- **Lerna** — the CLI used to run tasks (`lerna run build` etc.)
- **Nx** — the task engine/cache that Lerna actually delegates to

Lerna 9 bundles Nx as a hard dependency (`nx >=21.5.3 < 23.0.0`), and `lerna run <script>` is a thin CLI over Nx's engine. Evidence in this repo:

- `nx.json` at the root holds all the task logic (`dependsOn: ["^build"]`, caching) that orchestrates builds/tests
- `.nx/workspace-data/` contains Nx's cache DBs and project graph (which includes the e2e apps and `phoria-dotnet`)
- `lerna.json` is three lines with no versioning/publishing config
- Lerna's only unique value — `lerna version`/`lerna publish` — is unused; Changesets owns versioning and `lerna run publish` just runs the `publish` script in `phoria-dotnet`

So Lerna contributes the word "lerna" in scripts and nothing else. Turborepo is the de-facto 2026 default for pnpm monorepos (~2M weekly downloads, Vercel-backed, Rust core) and is the tool a new contributor is most likely to recognize. The migration surface is small because `nx.json` is only seven target defaults.

## Target State

Two tools instead of three: **pnpm** + **Turborepo**, with Changesets for publishing.

### Turbo config

New root `turbo.json`, mirroring the existing `nx.json` `targetDefaults` exactly:

```json
{
	"$schema": "https://turbo.build/schema.json",
	"tasks": {
		"build": { "dependsOn": ["^build"], "cache": true },
		"check": { "dependsOn": ["^build"], "cache": true },
		"lint": { "cache": true },
		"preview": { "dependsOn": ["build"], "persistent": true },
		"test": { "dependsOn": ["^build"], "cache": true },
		"test:browser": { "dependsOn": ["^build"], "cache": true }
	}
}
```

- `dependsOn: ["^build"]` preserves the cross-task ordering that does real work here (framework packages' tests import the built `dist/` of `@phoria/phoria`).
- `preview` is a long-running server task: `persistent: true`, not cacheable (intentional deviation from the `cache: true` it had under Nx — CI runs preview via `pnpm --filter` directly, never through the runner).
- Turborepo's default cache outputs cover the JS `dist/` outputs; no explicit `outputs` needed.
- The e2e apps (`framework-multiple`, `with-workspace`) are in the pnpm workspace graph, so `turbo run build` includes them — identical to current behavior.

### Root package.json

- Add `turbo` (latest 2.x) to devDependencies; remove `lerna`.
- Scripts:
  - `"build": "turbo run build"` (new root convenience script)
  - `"lint": "turbo run lint"` (new)
  - `"check": "turbo run check"` (new)
  - `"test": "turbo run test"` (replaces `lerna run test`)
  - `"test:browser": "turbo run test:browser"` (replaces `lerna run test:browser`)
  - `"publish": "changeset publish && pnpm --filter phoria-dotnet run publish"` (only `phoria-dotnet` has a `publish` script; the runner adds nothing here)
  - Remove the `"lerna": "lerna"` alias
- `"version"` script unchanged.

### Removed / updated files

- **Delete:** `lerna.json`, `nx.json`
- **`pnpm-workspace.yaml`:** remove `nx: true` from `allowBuilds`
- **`.gitignore`:** add `.turbo/` (task-cache dir lives under `node_modules/`, already ignored)
- **`.github/workflows/ci.yml`:** `pnpm lerna run build|lint|check|test` → `pnpm build|lint|check|test`; `pnpm lerna run build` → `pnpm build`; `pnpm lerna run test:browser` → `pnpm test:browser`
- **`.github/workflows/release.yml`:** `pnpm lerna run build` → `pnpm build`
- **`AGENTS.md`:** repo-structure line, Commands section, and build-order note updated to Turborepo
- **`docs/PROJECT.md`:** constraint line and Phase 0 description updated
- **`docs/plans/*`:** historical planning documents — not touched

### Non-changes

- No remote cache (no Vercel/Nx Cloud today; CI stays uncached-remote, same as now).
- Publishing flow (Changesets for JS, `scripts/dotnet/publish.js` for NuGet) unchanged.

## Verification

1. `pnpm install` — lockfile regenerates without `lerna`/`nx`
2. `pnpm build && pnpm lint && pnpm check && pnpm test && pnpm test:browser` — all green
3. Run `pnpm build` twice; second run should report cached tasks
4. `dotnet test --solution Phoria.sln --configuration Release`
5. Grep for stray `lerna`/`nx` references outside `docs/plans/*` and `pnpm-lock.yaml` — none
