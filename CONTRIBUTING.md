# Contributing to Phoria

Thanks for considering a contribution to [Phoria](https://github.com/CMeeg/phoria), an islands architecture framework for .NET powered by Vite. This guide covers the development setup, the branch model, how to make a change, and how releases work. The architecture is documented in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — worth reading before diving in.

## Prerequisites

- Node.js 24.18.0 (see `.nvmrc`)
- pnpm 11.17.0 (see `packageManager` in the root `package.json`)
- .NET SDK 10.0.302 (see `global.json`, rolls forward to the latest feature)

## Repository structure

A pnpm-workspaces monorepo (with Turborepo for task running and Changesets for versioning):

```
packages/
  phoria-islands/       @phoria/phoria - core Vite plugin + client/server entry
  phoria-react/         @phoria/phoria-react - React integration
  phoria-svelte/        @phoria/phoria-svelte - Svelte integration
  phoria-vue/           @phoria/phoria-vue - Vue integration
  phoria-opentelemetry/ @phoria/opentelemetry - OpenTelemetry instrumentation for the Node sidecar
  vite-plugin-dotnet-dev-certs/  @phoria/vite-plugin-dotnet-dev-certs
  Phoria/               Phoria (.NET) - NuGet package with TagHelpers, SSR, server process
examples/
  getting-started/      Single-framework example (React)
  framework-multiple/   Multi-framework example (React + Svelte + Vue)
```

The examples are deliberately outside the pnpm workspace: each ships its own `WebApp/pnpm-workspace.yaml` and committed lockfile, and references the **published** phoria packages. Local development against in-repo packages is handled by the `examples:link`/`examples:refresh`/`examples:sync` scripts described below.

## Development setup

```shell
pnpm install
pnpm build        # build all packages (Turborepo handles order)
pnpm lint         # Biome
pnpm check        # tsc (no emit)
pnpm test         # Vitest unit tests
pnpm test:browser # Vitest browser-mode component tests
```

```shell
dotnet test --solution Phoria.sln --configuration Release
```

To work on the examples against in-repo packages, run `pnpm build` at the repo root first (so linked `dist` exists), then `pnpm examples:link` to switch the examples to `file:` references and a `ProjectReference`. `file:` hard links go stale on rebuild, so re-run `pnpm examples:refresh` after any root `pnpm build`, and run `pnpm examples:sync` to restore the committed registry references when done. `pnpm examples:check` (also run in CI) fails if an example commits a `link:`/`file:` phoria reference.

## Branch model

All feature work targets the **`canary`** branch. `canary` produces prerelease `beta` builds on every merged change, and `main` receives coordinated stable releases cut from `canary`. Nobody pushes to `main` or `canary` directly — branch protection requires a pull request with passing CI. See the [Release workflow](#release-workflow) section for how this affects what lands where.

## Making a change

All work starts with an issue or discussion. Unsolicited contributions are not welcome: open an issue to report a bug or propose a feature, or start a discussion to explore an idea, and agree the approach with a maintainer before writing code. A pull request that arrives without a prior issue or discussion may be closed.

1. Create a branch off `canary` (fork if you don't have write access).
2. Make your change, following the existing conventions: Biome-enforced formatting (tabs, as-needed semicolons, no trailing commas), TypeScript strict mode, .NET with nullable reference types enabled, and tests co-located with the code they cover.
3. Add a changeset describing your change — `pnpm changeset` at the repo root. Every user-facing change needs one; it is what versions and publishes your work.
4. Push and open a pull request against `canary`. CI runs build, lint, type-check, unit/browser tests, .NET tests, and the examples-published-versions check; the required checks must pass.
5. A maintainer reviews and merges. Because `canary` publishes betas automatically, an approved change ships as a `beta` prerelease soon after merge.

## AI-assisted contributions

Contributions completed with the assistance of a coding agent are welcome — maintainers use them too. AI-only contributions, where the work is generated and submitted without meaningful human involvement (sometimes called "vibe coding"), are not: every contribution needs a human who understands and stands behind the change, its tests, and its trade-offs.

## Release workflow

### Beta stream (canary)

Every merged change with a changeset makes Changesets open a "Version Packages (beta)" pull request on `canary`. Merging that PR runs the release workflow, which publishes each package at its natural 0.x beta version (npm `beta` dist-tag) and a matching beta to NuGet, then opens a release-specific `chore/examples-sync-<branch>-<commit>` pull request that updates the examples to the released beta versions. Merge that examples-sync PR separately. The workflow never deletes or overwrites a fixed examples branch. Before Changesets runs, the shared workflow normalizes `.changeset/config.json.baseBranch` from `GITHUB_REF_NAME`, so the runtime branch identity is always correct. The four framework peer ranges use a prerelease-aware lower bound matching the upcoming core tuple, for example `>=0.5.0-0 <2.0.0`; update that tuple before a later beta cycle. Betas are safe to consume for integration and production testing of work in progress.

### Stable releases (the canary → main cut)

Stable releases are coordinated cuts, run by a maintainer:

1. On `canary`, exit beta mode and commit with `baseBranch: "canary"`.
2. Merge `canary` into `main`; the workflow normalizes `baseBranch` to `main` before opening the stable version PR.
3. Merge the stable version PR, then merge `main` back into `canary`.
4. Restore `baseBranch: "canary"`, enter beta mode, and commit the config plus `pre.json`.
5. Merge the release-specific examples-sync PR separately.

Stable package publishing pushes release tags only; it does not push a branch ref. The separate examples-sync step pushes its release-specific `chore/examples-sync-<branch>-<commit>` branch and opens the pull request. Between steps 1 and 4 the beta stream is quiescent — `canary` publishes nothing until pre mode is re-entered. Do not merge feature work to `canary` during this window. A build failure prevents Changesets from running, a publish failure prevents tag and examples-sync steps, and an examples-sync failure cannot republish packages and is independently retryable.

### Publishing security

Publishing is gated end to end: only maintainers can merge to `main`/`canary` (branch protection), npm publishes use trusted publishing (OIDC) so no npm token lives in the repository or workflows, and NuGet uses a repository secret API key. There is no per-PR npm/NuGet publish — packages only reach the registry through the release workflow.

## Documentation

Contributions that change how Phoria works should review whether the README files or [`docs/guides/`](docs/guides/) need to reflect the change, and update them as part of the contribution. Substantial changes should also keep [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) consistent with the code.

## Getting help

- Open an issue on GitHub for bugs, unclear docs, or feature discussions.
