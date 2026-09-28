# Contributing to Phoria

Thanks for considering a contribution to [Phoria](https://github.com/CMeeg/phoria), an islands architecture framework for .NET powered by Vite. This guide covers the development setup, the branch model, how to make a change, how to change an example, and how releases work. The architecture is documented in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) — worth reading before diving in.

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

## Changing an example

Every single-WebApp example must keep `AppHost/`, `WebApp/`, `WebApp/Properties/launchSettings.json`, `WebApp/package.json`, `WebApp/pnpm-workspace.yaml`, `WebApp/pnpm-lock.yaml`, and the `WebApp` scripts for `build`, `dev`, `dev:server`, `dev:webapp`, `preview`, `preview:server`, `stop`, and `test:e2e`. The alternate workspace must keep `AppHost/`, `apps/WebApp/`, `packages/ui/`, the root `package.json`, `pnpm-workspace.yaml`, and the corresponding `apps/WebApp` scripts.

Run single-WebApp commands from `examples/<name>/WebApp`; Vite discovers `vite.config.ts` and resolves its root from that working directory. Run `with-workspace` install and root build commands from `examples/with-workspace`, then run application commands with `pnpm --dir apps/WebApp ...` because its AppHost resolves `apps/WebApp`.

| Example | WebApp | Phoria Server |
| --- | ---: | ---: |
| `getting-started` | `5373` | `5273` |
| `framework-multiple` | `5573` | `5473` |
| `framework-react` | `5173` | `5073` |
| `framework-svelte` | `5473` | `5373` |
| `framework-vue` | `5273` | `5173` |
| `with-tailwind` | `5773` | `5673` |
| `with-styled-components` | `5873` | `5773` |
| `with-storybook` | `5973` | `5873` |
| `with-workspace` | `5673` | `5573` |

Keep this port map collision-free. The WebApp port is the URL the smoke tests use, and the Phoria Server port is the sidecar endpoint the .NET application calls. The WebApp column is the `Dev port` in the [examples catalog](examples/README.md); HTTPS launch-profile ports are the WebApp port plus `1000` for every single-WebApp example, and `with-workspace` takes its URLs from its AppHost configuration. Every container publishes the WebApp on the catalog's `Docker port`. Each e2e suite reads `PHORIA_WEBAPP_URL` and defaults to its WebApp port, so a smoke test against another port must set the variable explicitly, for example `PHORIA_WEBAPP_URL=http://localhost:5373 pnpm test:e2e`.

The `/health` endpoint is part of the smoke-test contract. A running example must return HTTP `200` and `Healthy` from its WebApp URL at `/health`; the Aspire AppHost separately checks the Phoria Server at `/hc`.

A committed example must use published registry references for `@phoria/phoria`, its framework integrations, and the other Phoria packages; `link:`, `file:`, and a Phoria `ProjectReference` do not belong in it. Run `pnpm examples:check` from the repository root before opening a change. The `examples:link`/`examples:refresh`/`examples:sync` workflow for working against in-repo packages is in [Development setup](#development-setup).

Verify standalone distribution with giget, not a repository-relative copy. From a temporary directory, run `pnpx giget gh:cmeeg/phoria/examples/<name> <name>`, enter the fetched example, run its documented `pnpm install` and `pnpm build`, and confirm the build succeeds without the source repository or root workspace. For `with-workspace`, run those commands from the fetched example root and confirm the `apps/WebApp` filter build resolves `packages/ui`.

The normal development flow is `pnpm dev`; use `pnpm preview` after `pnpm build` to exercise the production-shaped Aspire flow, and `pnpm stop` to stop all discoverable AppHosts. Run `pnpm test:e2e` against the running WebApp, or set `PHORIA_WEBAPP_URL` when the port is changed.

## AI-assisted contributions

Contributions completed with the assistance of a coding agent are welcome — maintainers use them too. AI-only contributions, where the work is generated and submitted without meaningful human involvement (sometimes called "vibe coding"), are not: every contribution needs a human who understands and stands behind the change, its tests, and its trade-offs.

## Release workflow

### Beta stream (canary)

Every merged change with a changeset makes the release workflow open a version pull request titled `chore: release` on `canary`. Merging that PR runs the release workflow, which publishes each package at its natural 0.x beta version (npm `beta` dist-tag) and a matching beta to NuGet, then opens a release-specific `chore/examples-sync-<branch>-<commit>` pull request that updates the examples to the released beta versions. Merge that examples-sync PR separately. The workflow never deletes or overwrites a fixed examples branch. Before Changesets runs, the shared workflow normalizes `.changeset/config.json.baseBranch` from `GITHUB_REF_NAME`, so the runtime branch identity is always correct. The four framework peer ranges use a prerelease-aware lower bound matching the upcoming core tuple, for example `>=0.5.0-0 <2.0.0`; update that tuple before a later beta cycle. Betas are safe to consume for integration and production testing of work in progress.

### Stable releases (the canary → main cut)

Stable releases are coordinated cuts, run by a maintainer:

1. On `canary`, exit beta mode and commit with `baseBranch: "canary"`.
2. Merge `canary` into `main`; the workflow normalizes `baseBranch` to `main` before opening the stable version PR.
3. Merge the stable version PR, then merge `main` back into `canary`.
4. Restore `baseBranch: "canary"`, enter beta mode, and commit the config plus `pre.json`.
5. Merge the release-specific examples-sync PR separately.

Stable package publishing pushes release tags only; it does not push a branch ref. The separate examples-sync step pushes its release-specific `chore/examples-sync-<branch>-<commit>` branch and opens the pull request. Between steps 1 and 4 the beta stream is quiescent — `canary` publishes nothing until pre mode is re-entered. Do not merge feature work to `canary` during this window. A build failure prevents Changesets from running, a publish failure prevents tag and examples-sync steps, and an examples-sync failure cannot republish packages and is independently retryable.

### Publishing security

Publishing is gated end to end: only maintainers can merge to `main`/`canary` (branch protection), npm publishes use trusted publishing (OIDC) so no npm token lives in the repository or workflows, and NuGet uses trusted publishing (OIDC) via `NuGet/login@v1`, so no long-lived NuGet API key is required. There is no per-PR npm/NuGet publish — packages only reach the registry through the release workflow.

## Documentation

Contributions that change how Phoria works must update the documentation that describes them, in the same contribution. Review the README files and [`docs/guides/`](docs/guides/); where a change alters how Phoria's pieces cooperate, [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) must match the code.

**Every plan carries a documentation review that names the documents it checked.** State which documents were read and what was confirmed about each — the guide documenting a changed export, the package README whose install command or version range moved, the example README whose commands changed. Where a change altered no public surface, say so and give the reason; the entry still exists. A plan without a named documentation review is incomplete.

## Getting help

- Open an issue on GitHub for bugs, unclear docs, or feature discussions.
