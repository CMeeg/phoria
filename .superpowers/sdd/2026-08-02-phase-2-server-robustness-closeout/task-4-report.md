# Task 4 Report: Node Handler Logger Interface

## Status

Complete.

## Changes

- Added and exported the minimal `PhoriaLogger` interface from `@phoria/phoria/server`.
- Added a console-backed default logger with no OTel dependency in the published package.
- Added optional `logger` options to CSR and SSR request handlers while preserving existing callers.
- Routed SSR entry-load, invalid-entry, and render failures through the configured logger.
- Routed missing CSR asset diagnostics through the configured logger.
- Passed OTel-backed `PhoriaLogger` adapters from all applicable e2e `server.ts` files:
  - `e2e/framework-multiple/WebApp/ui/src/server.ts`
  - `e2e/with-sidecar/WebApp/ui/src/server.ts`
  - `e2e/with-workspace/WebApp/ui/src/server.ts`
- Added routing tests covering configured SSR load-failure logging and configured CSR missing-asset logging.

## Verification

- `pnpm --filter @phoria/phoria test`: passed, 6 files / 14 tests.
- `pnpm --filter @phoria/phoria check`: passed.
- `pnpm --filter @phoria/phoria lint`: passed.
- `pnpm --filter @phoria/phoria build`: passed.
- `pnpm --filter framework-multiple lint && pnpm --filter framework-multiple check`: passed with one pre-existing Vue shim suppression warning.
- `pnpm --filter with-sidecar lint && pnpm --filter with-sidecar check`: passed.
- `pnpm --filter with-workspace lint && pnpm --filter with-workspace check`: passed with one pre-existing Vue shim suppression warning.
- `pnpm --filter framework-multiple build`: passed; existing Svelte compiler notices were emitted.
- `pnpm --filter with-sidecar build`: passed.
- `pnpm --filter with-workspace build`: passed; existing Svelte compiler notices were emitted.

## Concerns

- The two Vue-based e2e apps retain existing Biome warnings for an unused suppression in `vue-shim.d.ts`.
- The Svelte e2e builds retain an existing compiler notice about the initial `$state(startAt)` capture.
