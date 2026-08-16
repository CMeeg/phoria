# Styled Components Example

This example demonstrates a React-only Phoria island with styled-components SSR in a standalone .NET web application. The example-local SSR entry uses Phoria's existing `renderComponent` option to collect and prepend styled-components style tags. It intentionally does not add a Babel plugin.

## Run

Install the published dependencies and build the example from its `WebApp` directory:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp listens on port `5873` and the Phoria Server listens on port `5773`.

Run the smoke test against the Aspire-hosted app with:

```bash
pnpm test:e2e
```

The example can also be fetched directly with:

```bash
  pnpx giget gh:cmeeg/phoria/examples/with-styled-components with-styled-components
```
