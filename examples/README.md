# Examples

Phoria's examples are standalone applications outside the root pnpm workspace. Each example uses published Phoria package ranges and includes its own pnpm workspace and lockfile. Install and run commands from the directory shown in the catalog unless noted otherwise.

## Catalog

| Example | Frameworks and features | Layout | WebApp port | Phoria Server port | Primary commands |
| --- | --- | --- | ---: | ---: | --- |
| [getting-started](./getting-started) | React; basic SSR, hydration, and health check | `examples/getting-started/WebApp` | `5373` | `5273` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [framework-multiple](./framework-multiple) | React, Svelte, and Vue; query-filtered islands and OpenTelemetry | `examples/framework-multiple/WebApp` | `5573` | `5473` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [framework-react](./framework-react) | React-only island | `examples/framework-react/WebApp` | `5173` | `5073` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [framework-svelte](./framework-svelte) | Svelte-only island | `examples/framework-svelte/WebApp` | `5473` | `5373` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [framework-vue](./framework-vue) | Vue-only island | `examples/framework-vue/WebApp` | `5273` | `5173` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [with-tailwind](./with-tailwind) | React with Tailwind CSS v4 and the official Vite plugin | `examples/with-tailwind/WebApp` | `5773` | `5673` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [with-styled-components](./with-styled-components) | React with styled-components SSR | `examples/with-styled-components/WebApp` | `5873` | `5773` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm test:e2e`, `pnpm stop` |
| [with-storybook](./with-storybook) | React with Storybook 10 | `examples/with-storybook/WebApp` | `5973` | `5873` | `pnpm install`, `pnpm build`, `pnpm dev`, `pnpm preview`, `pnpm build:storybook`, `pnpm storybook`, `pnpm test:e2e`, `pnpm stop` |
| [with-workspace](./with-workspace) | React island imported from a shared package in an alternate pnpm workspace | `examples/with-workspace/apps/WebApp` and `examples/with-workspace/packages/ui` | `5673` | `5573` | `pnpm install`, `pnpm build`, `pnpm --dir apps/WebApp dev`, `pnpm --dir apps/WebApp preview`, `pnpm --dir apps/WebApp test:e2e`, `pnpm --dir apps/WebApp stop` |

The WebApp port is the URL used by the smoke tests. The Phoria Server port is the sidecar endpoint used by the .NET application. HTTPS launch-profile ports are the corresponding WebApp ports plus `1000` for the eight single-WebApp examples; `with-workspace` uses the HTTP URL from its AppHost configuration.

## Contributor Contract

Every single-WebApp example must keep `AppHost/`, `WebApp/`, `WebApp/Properties/launchSettings.json`, `WebApp/package.json`, `WebApp/pnpm-workspace.yaml`, `WebApp/pnpm-lock.yaml`, and the `WebApp` scripts for `build`, `dev`, `dev:server`, `dev:webapp`, `preview`, `preview:server`, `stop`, and `test:e2e`. The alternate workspace must keep `AppHost/`, `apps/WebApp/`, `packages/ui/`, the root `package.json`, `pnpm-workspace.yaml`, and the corresponding `apps/WebApp` scripts.

Run single-WebApp commands from `examples/<name>/WebApp`; Vite discovers `vite.config.ts` and resolves its root from that working directory. Run `with-workspace` install and root build commands from `examples/with-workspace`, then run application commands with `pnpm --dir apps/WebApp ...` because its AppHost resolves `apps/WebApp`.

Keep the port map above collision-free. Each e2e suite reads `PHORIA_WEBAPP_URL` and defaults to its catalog WebApp URL, so a smoke test against another port must set the variable explicitly, for example `PHORIA_WEBAPP_URL=http://localhost:5373 pnpm test:e2e`.

The `/health` endpoint is part of the smoke-test contract. A running example must return HTTP `200` and `Healthy` from its WebApp URL at `/health`; the Aspire AppHost separately checks the Phoria Server at `/hc`.

Committed examples must use published registry references for `@phoria/phoria`, framework integrations, and related Phoria packages. Do not commit `link:` or `file:` references or a Phoria `ProjectReference`. Run `pnpm examples:check` from the repository root before opening a change. For local package development, use `pnpm examples:link`, rebuild, refresh as required, and run `pnpm examples:sync` before committing.

Verify standalone distribution with giget, not a repository-relative copy. From a temporary directory, run `pnpx giget gh:cmeeg/phoria/examples/<name> <name>`, enter the fetched example, run its documented `pnpm install` and `pnpm build`, and confirm the build succeeds without the source repository or root workspace. For `with-workspace`, run those commands from the fetched example root and confirm the `apps/WebApp` filter build resolves `packages/ui`.

The normal development flow is `pnpm dev`; use `pnpm preview` after `pnpm build` to exercise the production-shaped Aspire flow, and `pnpm stop` to stop all discoverable AppHosts. Run `pnpm test:e2e` against the running WebApp, or set `PHORIA_WEBAPP_URL` when the port is changed.
