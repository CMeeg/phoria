# Workspace Example

This example demonstrates a React Phoria island imported from a shared UI package in an alternate pnpm workspace layout.

The workspace root is `examples/with-workspace`, with the application in `apps/WebApp` and the reusable component package in `packages/ui`. Install from the example root so pnpm resolves `@phoriaexamples/ui` before building the islands.

```bash
pnpm install
pnpm build
pnpm dev
```

The WebApp listens on port `5673` and the Phoria Server listens on port `5573`. The page registers the shared `Counter` component with `StartAt = 5`.

Run the smoke test with:

```bash
pnpm test:e2e
```

The example can also be fetched directly with:

```bash
pnpx giget gh:cmeeg/phoria/examples/with-workspace with-workspace
```
