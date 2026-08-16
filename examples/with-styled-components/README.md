# Styled Components Example

This example demonstrates a React-only Phoria island with styled-components SSR. Its local `src/entry-server.tsx` uses Phoria's `renderComponent` option to collect and prepend styled-components style tags; it intentionally does not add a Babel plugin. Production bundles use the example's `.tsx` to `.ts` runtime filename alias, while development uses the `.tsx` entry directly through Vite.

Run these commands from `examples/with-styled-components/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

The WebApp is at `http://localhost:5873`; the Phoria Server is at `http://localhost:5773`. Use `pnpm preview` after the build and `pnpm stop` to stop the AppHost. With the app running, `pnpm test:e2e` checks styled-components SSR, modulepreload output, island markup, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5873`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-styled-components with-styled-components
```
