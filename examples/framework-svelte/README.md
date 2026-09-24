# Framework Svelte Example

This example demonstrates a Svelte-only Phoria island in a standalone .NET web application. The page uses a Svelte counter with `Client.Visible("50px")` hydration.

Run these commands from `examples/framework-svelte/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp is at `http://localhost:5473`; the Phoria Server is at `http://localhost:5373`.

After a build, use `pnpm preview` for the production-shaped flow and `pnpm stop` to stop the AppHost. With the app running, `pnpm test:e2e` checks Svelte SSR, modulepreload output, island markup, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5473`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/framework-svelte framework-svelte
```
