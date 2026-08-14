# Task 4 Report

Commit: `18fe649` (`test: add framework test parity`)

## Changes

- React: added hydration retention coverage, plugin transform/filter and environment coverage, SSR service/island tests, registration identity tests, and the `*.test.tsx` node include.
- Svelte: added browser Vitest configuration and package wiring, a Svelte fixture and module declaration, CSR mount/hydrate coverage, SSR service/island/override coverage, and registration identity tests.
- Vue: added browser Vitest configuration and package wiring, CSR mount coverage, SSR service/island/override coverage, registration identity tests, and the SSR environment externalization test.
- Updated `pnpm-lock.yaml` for the browser test dependencies.

## Verification

- `pnpm install`: passed.
- `pnpm build`: passed, 7 tasks.
- `pnpm lint`: passed, 6 tasks.
- `pnpm check`: passed, 7 tasks.
- `pnpm test`: passed, 57 tests across 26 test files.
- `pnpm test:browser`: passed, 9 Chromium tests across islands, React, Svelte, and Vue.
- `git diff --check`: passed.

## Deviations

- Svelte 5 wraps the hand-rolled SSR output with fragment markers, so the test asserts `<span>Hello World</span>` is present rather than asserting marker-free output.
- Node registration tests define a minimal `HTMLElement` before dynamic imports because the shared client entry declares its custom element during import.
- The Svelte browser config imports the named `svelte` plugin export, matching the installed `@sveltejs/vite-plugin-svelte` API.

## Reviewer Focus

- Confirm `vi.resetModules()` plus dynamic imports preserve registry/service identity.
- Confirm React transform filtering excludes `node_modules` while preserving the cwd-relative component path.
- Confirm hydration retains pre-rendered React and Svelte content.
- Confirm exact wrong-framework guard messages and Vue SSR externalization.
- Confirm browser configs run only browser tests and do not alter root `package.json` or `turbo.json`.
