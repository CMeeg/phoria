# Framework Vue Example

This example demonstrates a Vue-only Phoria island in a standalone .NET web application. The page server-renders a Vue counter and hydrates it with `Client.Idle()` from `StartAt = 5`.

Run these commands from `examples/framework-vue/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp is at `http://localhost:5273`; the Phoria Server is at `http://localhost:5173`.

After a build, use `pnpm preview` for the production-shaped flow and `pnpm stop` to stop the AppHost. With the app running, `pnpm test:e2e` checks Vue SSR, modulepreload output, island markup, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5273`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/framework-vue framework-vue
```
