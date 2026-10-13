# Phoria Server

The Phoria Server is a [h3](https://h3.unjs.io/) server that effectively runs as a "sidecar" to your web app by running inside its own `node` process. It delegates requests to CSR, SSR or static file handlers as required, which use Vite's Dev Server in development and the build output from Vite when in production.

## Related

- [Client Entry](./client-entry.md) and [Server Entry](./server-entry.md) — the two entry points the server renders through.
- [Phoria Web App](./phoria-web-app.md) — the .NET app that starts and proxies to the server.
- [Phoria Islands](./phoria-islands.md) — what the server renders.
- [Deployment](./deployment.md) — who starts and monitors the server process, and what the app does when it is unavailable.
