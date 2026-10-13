# Supported UI frameworks

> [!NOTE]
> This guide is incomplete. It will cover the full API of each framework package. For now, each section links the package's example, which is the fastest way to see a framework working end to end.

Phoria supports:

* [React](#react)
* [Svelte](#svelte)
* [Vue](#vue)

> [!NOTE]
> Support for other frameworks may be added in the future. If you have a specific request, please raise an issue.

Each framework ships a package of the same shape: a client-side service and a server-side service, which Phoria calls to hydrate or mount a component and to render its markup, and a Vite plugin that transforms the framework's own component files so Phoria can find them. Each plugin takes its wrapped framework plugin's options under a key named after the framework, and passing `false` there leaves the wrapped plugin out and keeps Phoria's. [Component register](./component-register.md) covers how a package's services are registered before the components that use them.

## React

`@phoria/phoria-react` provides the React CSR and SSR services and the React Vite plugin. The plugin wraps `react()` from `@vitejs/plugin-react` and configures the shared factory with React's component filter, `**/*.jsx` and `**/*.tsx`.

React's client service hydrates server-rendered markup with `hydrateRoot`, and renders with `createRoot().render()` when the island is client-rendering only. Its server service renders with `renderToReadableStream`, and also exports `renderToString` for when you would rather not stream.

See the [`framework-react`](../../examples/framework-react) example, or [`framework-multiple`](../../examples/framework-multiple) for React alongside Svelte and Vue.

## Svelte

`@phoria/phoria-svelte` provides the Svelte CSR and SSR services and the Svelte Vite plugin. The plugin wraps `svelte()` from `@sveltejs/vite-plugin-svelte`, configures the shared factory with Svelte's `**/*.svelte` filter, and additionally externalises `svelte` itself for the server environment.

Svelte's client service hydrates with `hydrate` and mounts with `mount` for a client-rendering-only island. Its server service renders with `renderComponentToString`.

See the [`framework-svelte`](../../examples/framework-svelte) example, or [`framework-multiple`](../../examples/framework-multiple) for all three frameworks together.

## Vue

`@phoria/phoria-vue` provides the Vue CSR and SSR services and the Vue Vite plugin. The plugin wraps `vue()` from `@vitejs/plugin-vue` and configures the shared factory with Vue's `**/*.vue` filter.

Vue's client service mounts the component with `createApp().mount`, taking the island element as the mount target. Its server service renders with `renderToWebStream`, and also exports `renderToString`.

See the [`framework-vue`](../../examples/framework-vue) example, or [`framework-multiple`](../../examples/framework-multiple) for all three frameworks together.

## Related

- [Workspaces](./workspaces.md) — the `workspacePackages` option that every framework plugin accepts.
- [Component register](./component-register.md) — how a component declares which framework renders it.
- [Phoria Islands](./phoria-islands.md) — the three rendering modes a framework's services get asked for.
- [Creating Phoria Island components](./creating-phoria-island-components.md) — the walkthrough, in React, for writing a component and rendering it from .NET (Svelte and Vue follow the same pattern).
