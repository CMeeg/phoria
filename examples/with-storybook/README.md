# Storybook Example

This example demonstrates a React-only Phoria island in a standalone .NET web application with Storybook for isolated component development. Run these commands from `examples/with-storybook/WebApp`:

```bash
pnpm install
pnpm build
pnpm dev
```

The WebApp is at `http://localhost:5973`; the Phoria Server is at `http://localhost:5873`. Use `pnpm preview` after the build and `pnpm stop` to stop the AppHost. With the app running, `pnpm test:e2e` checks the React island, modulepreload output, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5973`.

Build and run Storybook separately with `pnpm build:storybook` and `pnpm storybook`; Storybook listens on port `6006`. This example pins Vite to `~8.0.16` because Storybook currently has a Rolldown regression with later Vite 8.1.x releases. Remove the pin when the Storybook/Vite/Rolldown combination builds and runs without that regression.

Storybook 10 provides the former Essentials features through core, so this example intentionally does not install `@storybook/addon-essentials`; there is no compatible Storybook 10 release of that addon and the core features are sufficient for this example.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-storybook with-storybook
```
