# Framework React Example

This example demonstrates a React-only Phoria island in a standalone .NET web application. The page server-renders and hydrates a React counter with `Client.Load` and `StartAt = 5`.

Run these commands from `examples/framework-react/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp is at `http://localhost:5173`; the Phoria Server is at `http://localhost:5073`.

After a build, exercise the production-shaped flow with `pnpm preview`. Stop it with `pnpm stop`. With the app running, run `pnpm test:e2e`; the suite checks rendered island markup, React SSR, modulepreload output, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5173`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/framework-react framework-react
```
