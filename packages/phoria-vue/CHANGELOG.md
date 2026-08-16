# @phoria/phoria-vue

## 0.4.0-beta.0

### Minor Changes

- c716413: The Phoria Server bundle is now built by the `phoria` plugin as a `server` environment. A separate `vite.server.config.ts` is no longer required. Framework plugins now scope their entire per-environment plugin instance (via `applyToEnvironment`) to the client and ssr environments — not just their `transform` hook, though `transform` is currently the only per-environment hook these plugins define.
- c716413: Require Vite 8. Peer dependency widened to ^8.0.0; vite-tsconfig-paths replaced by Vite's built-in resolve.tsconfigPaths.

### Patch Changes

- c716413: Raise minimum Node.js to ^20.19.0 || ^22.12.0 || >=24.0.0 and pnpm to 11
- c716413: Build with TypeScript 6 and vite-plugin-dts 5
- c716413: Update dependencies.
- c716413: Widen the `@phoria/phoria` peer dependency range to `>=0.5.0-0 <2.0.0` so the first beta prerelease stays in the 0.x version family without a peer-range major cascade.

## 0.3.2

### Patch Changes

- 2881b78: Fix build warning due to exports order

## 0.3.1

### Patch Changes

- edff1eb: Improve type narrowing of Phoria Islands

## 0.3.0

### Minor Changes

- 95978f0: Rename some types to fix some inconsistencies and conflicts
- 95978f0: Allow for custom SSR render strategies

## 0.2.1

## 0.2.0

### Minor Changes

- 1b81b31: Update to target Vite 6

## 0.1.0

### Minor Changes

- 8f93f57: First release of 🏝️ Phoria Islands for dotnet.

### Patch Changes

- Updated dependencies [8f93f57]
  - @phoria/phoria@0.1.0
