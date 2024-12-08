# phoria

🏝️ Islands architecture for dotnet powered by ⚡ Vite.

Phoria allows you to easily and efficiently render [islands of interactivity](https://docs.astro.build/en/concepts/islands/) using [React](https://react.dev/), [Svelte](https://svelte.dev/) or [Vue](https://vuejs.org/) within your dotnet web app (Razor Pages or MVC) using both Client Side Rendering (CSR) and Server Side Rendering (SSR).

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
