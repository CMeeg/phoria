# Phoria Islands

A Phoria Island is a UI component that your .NET application hands to Phoria to render. You place one in a Razor page or view with the `<phoria-island>` Tag Helper, naming a component you have [registered](./component-register.md), and Phoria renders it into the page's HTML — on the server, in the browser, or both, depending on how you configure the island.

## Three rendering modes

An island's rendering mode follows from its `client` attribute. Each mode puts something different in the HTML, which is the quickest way to tell them apart in a rendered page:

| Mode | How you get it | What lands in the HTML |
| --- | --- | --- |
| Server-rendering only | Leave `client` off — this is the default | The component's markup on its own, with no `<phoria-island>` element around it |
| Both | Any directive other than `Client.Only`, such as `Client.Load` | A `<phoria-island>` element carrying the component name and the directive, with the markup inside it |
| Client-rendering only | `Client.Only` | A `<phoria-island>` element carrying the component name and `client:only`, and nothing inside it |

The rendered element is not a copy of what you wrote. `client="Client.Load"` in your Razor becomes `client:load` on the element, because that is the form the [Client Entry](./client-entry.md) reads. [Directives](./directives.md) is the reference for the strategies on offer and when each one fires, and covers the effect a server-only island has on the page's JavaScript.

## When the server cannot render the island

The mode you configure is the mode you get, for as long as the [Phoria Server](./phoria-server.md) is healthy. An island that needs server markup cannot be rendered without it, so when the server is not healthy an island set to both falls back to client-rendering only, while a server-only island cannot be rendered at all and has nothing to fall back to. An island set to client-rendering only is unaffected, because it never needed the server. [Phoria Web App](./phoria-web-app.md) covers what the app does then, and how to configure it.

## Where an island sits

An island is a component rendered at a point inside markup your .NET application already produces: the layout, navigation, forms and everything around it stay ordinary .NET and are never handed to a UI framework. It is not a page, a micro-frontend, or a separate application — there is one application, and Phoria is the boundary between it and the component. Your .NET code decides what to render and what to pass in; the component decides how it looks and how it behaves.

## Related

- [Creating Phoria Island components](./creating-phoria-island-components.md) — the walkthrough, in React, for writing a component and rendering it from .NET.
- [Phoria Island directives](./directives.md) — the reference for the client directives, and what each one waits for.
- [Component register](./component-register.md) — how a component becomes something Phoria can render.
- [Client Entry](./client-entry.md) — the browser entry point that picks up a hydrating island's component.
- [Server Entry](./server-entry.md) — the entry point the Phoria Server renders an island's markup through.
- [Phoria Web App](./phoria-web-app.md) — the app that hosts the Tag Helper and decides what happens when the server is down.
