# Workspace Example

This example demonstrates a React Phoria island imported from the shared `@phoriaexamples/ui` package in an alternate pnpm workspace layout. The workspace root is `examples/with-workspace`, the app is `apps/WebApp`, and the reusable counter is in `packages/ui`; the page registers it with `StartAt = 5`.

Run installation and the workspace build from `examples/with-workspace`:

```bash
pnpm install
pnpm build
pnpm --dir apps/WebApp dev
```

The WebApp is at `http://localhost:5673`; the Phoria Server is at `http://localhost:5573`. Use `pnpm --dir apps/WebApp preview` after the build and `pnpm --dir apps/WebApp stop` to stop the AppHost. With the app running, use `pnpm --dir apps/WebApp test:e2e`; the suite checks the shared-package island, modulepreload output, and `GET /health`. Set `PHORIA_WEBAPP_URL` to override the default `http://localhost:5673`.

Fetch a standalone copy with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-workspace with-workspace
```
