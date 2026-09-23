# Getting Started Example

This example demonstrates a React Phoria island in a standalone .NET web application built from the official Razor Pages template. It is the recommended starting point for new Phoria projects: a familiar Razor Pages app configured with Phoria and ready to go, with an island counter that server-renders and hydrates via `Client.Load`. Each page renders the island through the `phoria-island` tag helper, and the `ui/src/components/register.ts` module shows the component-registration contract for the framework.

Run these commands from `examples/getting-started/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp is at `http://localhost:5373`; the Phoria Server is at `http://localhost:5273`.

After a build, exercise the production-shaped flow with `pnpm preview`. Stop it with `pnpm stop`. With the app running, run `pnpm test:e2e`; the suite checks rendered island markup, modulepreload output, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5373`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/getting-started getting-started
```