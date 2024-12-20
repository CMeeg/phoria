<div align="center">
  <img width="120" height="133" src="/docs/assets/phoria.svg">
  <h1>phoria</h1>
  <p><i>🏝️ Islands architecture for dotnet powered by ⚡ Vite.</i></p>
  <hr /><br />
</div>

Phoria allows you to easily and efficiently render [islands of interactivity](https://docs.astro.build/en/concepts/islands/) using [React](https://react.dev/), [Svelte](https://svelte.dev/) or [Vue](https://vuejs.org/) within your dotnet web app (Razor Pages or MVC) using both Client Side Rendering and Server Side Rendering.

* ⚡ Built around [Vite](https://vite.dev/), which means you can enjoy a first class development experience, lightning fast HMR and access to its expansive plugin catalogue and ecosystem
* 🏝️ Easily and efficiently render islands using any supported UI framework(s)
* 🌊 Client Side Rendering (CSR) and support for multiple client hydration strategies via client directives such as on load, on idle, on visible and on match media query
* 🔋 Server Side Rendering (SSR) of Islands within your dotnet web app (Razor Pages or MVC) views
* 📦 Easily pass props from your dotnet web app to your UI components
* ⚙️ Shared configuration between dotnet and Vite using `appsettings.json` files and a `dotnet dev-certs` plugin

## Getting started

Please see the [getting started](./docs/guides/getting-started.md) guide.

## Usage

This documentation is TBC, but will cover:

* [Phoria Islands](./docs/guides/phoria-islands.md)
  * [Supported UI frameworks](./docs/guides/supported-ui-frameworks.md)
  * [Component register](./docs/guides/component-register.md)
* [Phoria Server](./docs/guides/phoria-server.md)
  * [Client Entry](./docs/guides/client-entry.md)
  * [Server Entry](./docs/guides/server-entry.md)
* [Phoria Web App](./docs/guides/phoria-web-app.md)
* [Configuration](./docs/guides/configuration.md)
* [Building for production](./docs/guides/building-for-production.md)
* [Deployment](./docs/guides/deployment.md)

## Acknowledgements

### Inspiration

The idea for this project came about after using [Astro](https://astro.build/) and thoroughly enjoying the whole experience with their implementation of the [Islands architecture](https://docs.astro.build/en/concepts/islands/). Astro was the catalyst and continues to inspire to this day.

The approach that the Remix team took to their [Vite plugin](https://remix.run/docs/en/main/guides/vite) and how they structure their applications is also a big inspiration.

This [presentation](https://www.youtube.com/watch?v=Ptqaqls2SYo) and [sample code](https://github.com/bholmesdev/vite-conf-islands-arch/blob/main/src/client.ts) by Ben Holmes (core maintainer of Astro) was the inspiration for the Phoria Island implementation.

### Implementation

This project would have been significantly slower to get off the ground if it wasn't for the amazing work done by the maintainers of:

* [Vite.AspNetCore](https://github.com/Eptagone/Vite.AspNetCore); and
* [NodeReact.NET](https://github.com/DaniilSokolyuk/NodeReact.NET)

Parts of their codebases are used in the dotnet Phoria library and helped form a solid foundation from which to build out the features that Phoria provides.
