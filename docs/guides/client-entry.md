# Client Entry

The Client Entry is the entrypoint for the browser and is responsible for hydrating your registered Phoria Island components using the appropriate CSR strategy provided by your chosen UI framework(s).

You can also choose to initialise other client-side code here, or import "global" CSS, if you wish.

> [!NOTE]
> Phoria supports client-only, server-only and "isomorphic" rendering of components in Islands. Server-only is the default, but you can opt-in to client-side rendering on an Island-by-Island basis using one or more client [directives](./directives.md) such as "client only", "on load" etc. If an Island is server-only then it will not hydrate on the client and therefore does not request any additional JavaScript.

> [!NOTE]
> `PhoriaIsland` is a custom HTML element that is used to hydrate the components that you have [registered](./component-register.md) in your component registration file (i.e. `./components/register`) for Islands that you have opted-in to client-side rendering.

## Related

- [Server Entry](./server-entry.md) — the other half; together they are the two entry points Phoria renders through.
- [Phoria Island directives](./directives.md) — the client directives that decide when, and whether, an island hydrates.
- [Phoria Islands](./phoria-islands.md) — the components a Client Entry hydrates once the page has loaded.
