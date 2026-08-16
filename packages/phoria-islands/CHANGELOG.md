# @phoria/phoria

## 0.5.0-beta.0

### Minor Changes

- c716413: The Phoria Server bundle is now built by the `phoria` plugin as a `server` environment. A separate `vite.server.config.ts` is no longer required. Framework plugins now scope their entire per-environment plugin instance (via `applyToEnvironment`) to the client and ssr environments — not just their `transform` hook, though `transform` is currently the only per-environment hook these plugins define.
- c716413: `PhoriaIsland.create` now takes a `PhoriaIslandRequest` instead of an h3 `H3Event`. Request handler factories (`createPhoriaSsrRequestHandler`, `createPhoriaDevSsrRequestHandler`, `createPhoriaCsrRequestHandler`, `createPhoriaDevCsrRequestHandler`) return the new `PhoriaRequestHandler` type.
- c716413: Require Vite 8. Peer dependency widened to ^8.0.0; vite-tsconfig-paths replaced by Vite's built-in resolve.tsconfigPaths.

### Patch Changes

- c716413: Raise minimum Node.js to ^20.19.0 || ^22.12.0 || >=24.0.0 and pnpm to 11
- c716413: Build with TypeScript 6 and vite-plugin-dts 5
- c716413: Update dependencies.
- c716413: Preserve user rolldownOptions; stop copying publicDir into the SSR build.

## 0.4.2

### Patch Changes

- 2881b78: Fix build warning due to exports order
- 2881b78: Fix build and runtime errors when running Vite outside of the web app directory

## 0.4.1

### Patch Changes

- edff1eb: Improve type narrowing of Phoria Islands

## 0.4.0

### Minor Changes

- 95978f0: Rename some types to fix some inconsistencies and conflicts
- 95978f0: Allow for custom SSR render strategies

## 0.3.1

### Patch Changes

- bc0ed79: Fix ssr outdir not always being set

## 0.3.0

### Minor Changes

- 1b81b31: Update to target Vite 6

## 0.2.0

### Minor Changes

- a0e3be7: Set server config in phoria plugin

## 0.1.1

### Patch Changes

- 780ffad: Fixes appsettings supplied directly to the Vite plugin not being merged with defaults

## 0.1.0

### Minor Changes

- 8f93f57: First release of 🏝️ Phoria Islands for dotnet.
