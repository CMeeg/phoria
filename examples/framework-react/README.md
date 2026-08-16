# Framework React Example

This example demonstrates a React-only Phoria island in a standalone .NET web application.

## Run

Install the published dependencies and build the example from its `WebApp` directory:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp listens on port `5173` and the Phoria Server listens on port `5073`.

Run the smoke test against the Aspire-hosted app with:

```bash
pnpm test:e2e
```

The example can also be fetched directly with:

```bash
pnpx giget gh:cmeeg/phoria/examples/framework-react framework-react
```
