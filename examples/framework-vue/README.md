# Framework Vue Example

This example demonstrates a Vue-only Phoria island in a standalone .NET web application.

## Run

Install the published dependencies and build the example from its `WebApp` directory:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp listens on port `5273` and the Phoria Server listens on port `5173`.

Run the smoke test against the Aspire-hosted app with:

```bash
pnpm test:e2e
```

The example can also be fetched directly with:

```bash
pnpx giget gh:CMeeg/phoria/examples/framework-vue framework-vue
```
