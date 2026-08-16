# Storybook Example

This example demonstrates a React-only Phoria island in a standalone .NET web application with Storybook for isolated component development.

## Run

Install the published dependencies and build the example from its `WebApp` directory:

```bash
pnpm install
pnpm build
pnpm dev
```

`pnpm dev` starts the WebApp, the Phoria Server with Vite HMR, and the Aspire dashboard. The WebApp listens on port `5973` and the Phoria Server listens on port `5873`.

Build and run Storybook separately with:

```bash
pnpm build:storybook
pnpm storybook
```

This example pins Vite to `~8.0.16` because Storybook currently has a Rolldown regression with later Vite 8.1.x releases. Remove the pin when the Storybook/Vite/Rolldown combination builds and runs without that regression.

Run the smoke test against the Aspire-hosted app with:

```bash
pnpm test:e2e
```

The example can also be fetched directly with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-storybook with-storybook
```
