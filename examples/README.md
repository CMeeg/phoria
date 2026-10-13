# Examples

Phoria's examples are standalone applications outside the root pnpm workspace. Each example uses published Phoria package ranges and includes its own pnpm workspace and lockfile.

## Catalog

| Example | Frameworks and features | Dev port | Docker port |
| --- | --- | ---: | ---: |
| [getting-started](./getting-started) | React; basic SSR, hydration, and health check | `5373` | `8080` |
| [framework-multiple](./framework-multiple) | React, Svelte, and Vue; query-filtered islands and OpenTelemetry | `5573` | `8080` |
| [framework-react](./framework-react) | React-only island | `5173` | `8080` |
| [framework-svelte](./framework-svelte) | Svelte-only island | `5473` | `8080` |
| [framework-vue](./framework-vue) | Vue-only island | `5273` | `8080` |
| [with-tailwind](./with-tailwind) | React with Tailwind CSS v4 and the official Vite plugin | `5773` | `8080` |
| [with-styled-components](./with-styled-components) | React with styled-components SSR | `5873` | `8080` |
| [with-storybook](./with-storybook) | React with Storybook 10 | `5973` | `8080` |
| [with-workspace](./with-workspace) | React island imported from a shared package in an alternate pnpm workspace | `5673` | `8080` |

Both port columns are the WebApp: `Dev port` is the port `pnpm dev` serves on, and `Docker port` is the port the container publishes.

> To fetch from the canary branch, append `#canary` to the ref — `gh:cmeeg/phoria/examples/<name>#canary`.

## Try it

Every example runs the same way, so the commands are written once. Fetch the example you have chosen, then build and start it in Docker:

```shell
pnpx giget gh:cmeeg/phoria/examples/getting-started getting-started
cd getting-started && docker compose up --build -d
```

Then open <http://localhost:8080> and stop it with `docker compose down`. The `Docker port` is the same for every example, so the URL does not change when you swap the example — only the name in the fetch path and the directory you change into. The first build pulls the .NET and Node base images, so expect it to take a few minutes.

Docker and Node are the only prerequisites: `pnpx` fetches the example, and the container compiles the .NET app and builds the frontend. To fetch from the canary branch, use the ref given in the note above; the [root README's getting-started block](../README.md#getting-started) is the same path written for the first example.
