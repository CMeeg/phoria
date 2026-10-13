# Server Entry

The Server Entry is the entrypoint for the [Phoria Server](./phoria-server.md) and is responsible for generating the markup of your registered Phoria Island components using the appropriate SSR strategy provided by your chosen UI framework(s). The Phoria Server SSR response is proxied back to the web app to be appended to the HTTP response stream.

> [!NOTE]
> `island.render()` will call the associated UI framework plugin's default render strategy, which in the case of React is [`renderToReadableStream`](https://react.dev/reference/react-dom/server/renderToReadableStream).
>
> The `island.render()` function does accept a custom render strategy if you need further control over it, for example if you are using a library like [styled components](https://styled-components.com/docs/advanced#server-side-rendering).

## Related

- [Client Entry](./client-entry.md) — the other half; together they are the two entry points Phoria renders through.
- [Phoria Server](./phoria-server.md) — the sidecar `node` process this entry runs inside, and whose SSR response is proxied back to the web app.
- [Phoria Islands](./phoria-islands.md) — the components a Server Entry turns into the markup in the response.
