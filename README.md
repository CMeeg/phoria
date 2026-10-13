<div align="center">
  <p><img width="120" height="133" src="./docs/assets/phoria.svg" alt="Phoria logo"></p>
  <h1>Phoria<br><br></h1>
  <p>🏝️ <i>Islands architecture for .NET powered by Vite</i> ⚡</p>
  <p><hr></p>
</div>

Phoria renders [islands of interactivity](https://docs.astro.build/en/concepts/islands/) using [React](https://react.dev/), [Svelte](https://svelte.dev/) or [Vue](https://vuejs.org/) inside .NET Razor Pages or MVC applications with both Client Side Rendering (CSR) and Server Side Rendering (SSR).

* ⚡ Built around [Vite](https://vite.dev/) with HMR and access to its plugin ecosystem
* 🏝️ Use one or multiple supported UI frameworks in the same .NET application
* 🌊 Choose client-only rendering or hydration with `load`, `idle`, `visible` and media-query directives
* 🔋 Server-render islands from Razor Pages or MVC views
* 📦 Pass typed .NET data to islands as serialized props
* ⚙️ Share configuration between .NET and Vite through `appsettings.json`
* 🩺 Monitor the Phoria Server with health checks and choose graceful degradation or fail-fast behavior
* 📈 Add optional OpenTelemetry logging, tracing and metrics to the Node sidecar and example applications

![Screenshot showing a Phoria Island TagHelper being used in a dotnet Razor Pages app to render a React component](./docs/assets/intro.png)

## Getting started

Run an example with Docker. Docker and Node are the only prerequisites — the multi-stage build compiles the .NET app and the frontend inside the container:

```shell
pnpx giget gh:cmeeg/phoria/examples/getting-started getting-started
cd getting-started && docker compose up --build -d
```

Then open <http://localhost:8080>, and stop with `docker compose down`. The first build pulls the .NET and Node base images, so expect it to take a few minutes.

The same two commands fetch any example — swap the name in the path and the directory. [`examples/getting-started`](./examples/getting-started) is React only; [`examples/framework-multiple`](./examples/framework-multiple) shows React, Svelte and Vue together.

> To target the canary branch instead, append `#canary` to the ref — `gh:cmeeg/phoria/examples/getting-started#canary`.

To develop against the source, or to add Phoria to an existing .NET project, see the [Getting started guide](./docs/guides/getting-started.md).

## Usage

Run it, then build on it, then go deeper:

1. [Getting started](./docs/guides/getting-started.md) — both ways in: clone an example project, or add Phoria to an existing .NET project by hand.
2. [Creating Phoria Island components](./docs/guides/creating-phoria-island-components.md) — the walkthrough, in React, for creating a UI component, registering it, and rendering it from .NET with the `PhoriaIslandTagHelper` or the `PhoriaIslandComponentFactory`.
3. [Phoria Islands](./docs/guides/phoria-islands.md) — what an island is, the three rendering modes it can use, and where an island sits in the page.
4. [Phoria Island directives](./docs/guides/directives.md) — the client directives, from `Client.Load` onwards, and what each one waits for.
5. [Component register](./docs/guides/component-register.md) — how a component name becomes something Phoria can render, the keys and loaders it holds, and covering more than one framework.
6. [Client Entry](./docs/guides/client-entry.md) and [Server Entry](./docs/guides/server-entry.md) — the two entry points an island renders through: the browser one hydrating, the server one generating the markup with an SSR strategy.
7. [Phoria Server](./docs/guides/phoria-server.md) — the Node.js sidecar that renders your islands, serving from Vite's Dev Server in development and from the build output in production.
8. [Phoria Web App](./docs/guides/phoria-web-app.md) — the .NET app with the `Phoria` package installed: supervising the Phoria Server, and what the app does when the server is unavailable.
9. [Building for production](./docs/guides/building-for-production.md) — the build scripts and preview scripts a Phoria solution needs in production.
10. [Deployment](./docs/guides/deployment.md) — deploying a production build to Azure Container Apps.

> [!NOTE]
> The guides cover the current setup and runtime model. If something is unclear or missing, please raise an issue.

## About Phoria

The idea for this project came about after using [Astro](https://astro.build/) and enjoying the whole experience with their implementation of [Islands architecture](https://docs.astro.build/en/concepts/islands/). I began to wonder what it would be like to have a similar experience in dotnet where the back-end is driven by dotnet Razor Pages or MVC, but you could easily add islands of interactivity using modern UI framework components.

Looking at the existing dotnet ecosystem:

* Blazor allows for a component-driven UI architecture, but it requires buying into a completely different and much less mature ecosystem than those offered, and already embraced by, the wider UI development community
* Microsoft seem to have settled on recommending a BFF (Backend For Frontend) pattern for integrating dotnet with UI frameworks such as React, Vue etc - this is certainly a valid approach, but isn't always a good fit if you would prefer or have to use dotnet at the application layer instead of "just" to deliver APIs

Phoria's aim is to allow you to build a dotnet web application using the tools and libraries you are already familiar with on the back-end, but with the added benefit of being able to easily and efficiently render islands of interactivity using the UI frameworks and libraries you are familiar with on the front-end.

## Acknowledgements

### Inspiration

[Astro](https://astro.build/) is the primary inspiration for this project in both its conception and implementation.

The approach that the Remix team took to their [Vite plugin](https://remix.run/docs/en/main/guides/vite) and how they structure their applications is also a big inspiration.

This [presentation](https://www.youtube.com/watch?v=Ptqaqls2SYo) and [sample code](https://github.com/bholmesdev/vite-conf-islands-arch/blob/main/src/client.ts) by Ben Holmes (core maintainer of Astro) was the inspiration for using custom HTML elements in the implementation of Phoria Islands.

### Implementation

This project would have been significantly slower to get off the ground if it wasn't for the amazing work done by the maintainers of:

* [Vite.AspNetCore](https://github.com/Eptagone/Vite.AspNetCore); and
* [NodeReact.NET](https://github.com/DaniilSokolyuk/NodeReact.NET)

The initial idea was to just consume and use these libraries in Phoria, but the scope for Phoria quickly diverged and would have required submitting changes upstream that seemed at odds with the scope of these libraries. For example, `Vite.AspNetCore` is focused on client-side rendering and `NodeReact.NET` is focused (unsurprisingly) on React and uses Webpack.

Parts of their codebases are used in the dotnet Phoria library (with license attribution) and helped form a basis from which to build out some of the features that Phoria provides. So a massive thank you to the maintainers of these libraries!

Phoria also wouldn't be possible without:

* [Vite](https://vite.dev/) and its amazing ecosystem of plugins and tools
* [h3](https://h3.unjs.io/) and the [unjs](https://unjs.io/) ecosystem
* The [Tinylibs](https://tinylibs.github.io/) libraries
