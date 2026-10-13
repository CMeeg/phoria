# Component Register

The component register is a module Phoria imports at startup to learn which components exist and how to render each one. Islands name a component, and Phoria looks that name up here. The register is imported by both the [Client Entry](./client-entry.md) and the [Server Entry](./server-entry.md), so a component is registered once and either side can find it.

In the getting-started path it is `WebApp/ui/src/components/register.ts`:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: {
      module: () => import("./Counter/Counter.tsx"),
      component: (module) => module.Counter
    },
    framework: "react"
  }
})
```

This tells Phoria to register a component using the name `Counter`, which can be imported from `./Counter/Counter.tsx` using a named export `Counter`, and it uses the `react` framework.

## Keys, loaders and frameworks

The key a component is registered under — `Counter` above — is what the `component` attribute of a `<phoria-island>` matches, and the match ignores case. Each entry has exactly two fields:

- `framework` names the UI framework that renders the component. It has to name a framework Phoria already knows about, or registering the component throws.
- `loader` says how to import the component, and takes one of two forms.

The object form above imports the module and then selects the component out of it, which is what lets you register a component that is a **named** export.

If your component uses a default export you can register it like this instead:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  Counter: {
    loader: () => import("./Counter/Counter.tsx"),
    framework: "react"
  }
})
```

The function form is shorter, but it only works when the component really is the default export — otherwise rendering the island throws, telling you to name the export and select it with the `component` function.

> [!TIP]
> The object passed to `registerComponents` can be used to register multiple components so as you add components that you want to use in Phoria Islands you can just keep adding them here.

## More than one framework

One register can mix frameworks, as long as each entry declares the framework its component is written in:

```ts
import { registerComponents } from "@phoria/phoria"

registerComponents({
  ReactCounter: {
    loader: {
      module: () => import("./counter/counter.tsx"),
      component: (module) => module.Counter,
    },
    framework: "react",
  },
  VueCounter: {
    loader: () => import("./counter/counter-button.vue"),
    framework: "vue",
  },
  SvelteCounter: {
    loader: () => import("./counter/counter.svelte"),
    framework: "svelte",
  },
})
```

The catch is that importing a framework package is what registers it with Phoria, so each entry file has to import every framework it uses *before* the register:

```ts
import "@phoria/phoria-react/client"
import "@phoria/phoria-svelte/client"
import "@phoria/phoria-vue/client"
import "./components/register"
import { PhoriaIsland } from "@phoria/phoria/client"

PhoriaIsland.register()
```

The [framework-multiple](https://github.com/CMeeg/phoria/tree/main/examples/framework-multiple) example is a working version of this. Adding a framework also means adding it to the server entry, which registers the matching server-side renderer.

## Related

- [Creating Phoria Island components](./creating-phoria-island-components.md) — the full walkthrough, including the Tag Helper and component factory.
- [Client Entry](./client-entry.md) — where the register is imported on the browser side, alongside the framework packages.
- [Server Entry](./server-entry.md) — where it is imported on the server side.
- [Phoria Islands](./phoria-islands.md) — the components you are registering, and what Phoria does with them once registered.
- [Supported UI frameworks](./supported-ui-frameworks.md) — the UI frameworks you can name in the `framework` field.
