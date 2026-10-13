# Fix Remaining Dev-Mode Island Bugs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three independent, pre-existing dev-mode bugs uncovered while diagnosing the Vue dev-mode duplicate-instance bug: React islands fail to hydrate in dev, Svelte islands crash during SSR in dev, and Vite logs a spurious dependency-scan warning on every dev server start.

**Architecture:** Each task is a small, independent, root-caused fix to a single Vite plugin (or client entry) in the `packages/phoria-*` workspace. There is no dependency between tasks - they touch different packages and can be implemented, reviewed, and shipped in any order or by different people.

**Tech Stack:** Vite 8 (Rolldown), `@vitejs/plugin-react` 6, Svelte 5, Vitest 4 (`vitest run` for Node tests, browser-mode Vitest with the Playwright provider for `*.browser.test.tsx`), Biome, TypeScript 5 (`tsc --noEmit`), pnpm 11 workspaces.

## Global Constraints

- Code style: Biome - tabs, semicolons omitted where possible, no trailing commas, 120-char lines. Run `pnpm biome check <path>` / `--write` to verify/fix.
- Comments are opt-in: only add a comment when it explains a non-obvious decision (each fix below includes one because the root cause isn't obvious from the code alone).
- CI order is build → lint → check → test; run `pnpm --filter <pkg> build` before `check` if you need the compiled `dist` for manual verification (not required for the unit/browser tests themselves, which run against `src` directly via Vitest).
- Each task's automated test lives in the same package as the fix and must pass via that package's own `test` (or `test:browser`) script - do not add a root-level test runner invocation.
- `packages/phoria-vue`, `packages/phoria-react`, and `packages/phoria-svelte` currently have **uncommitted, staged** changes from a prior, already-verified fix (pre-bundling each framework's runtime via `optimizeDeps.include` to fix a separate Vue dev-mode duplicate-module bug). Do not revert or amend that staged work - these three tasks add to it.
- Manual, end-to-end verification against the real example app requires the example to be in **linked** dev mode (`pnpm examples:link` at the repo root, then `pnpm build`, then `pnpm examples:refresh` after any subsequent package rebuild - `file:` hard links go stale on rebuild and are not refreshed by a plain `pnpm install`). Run `pnpm examples:sync` when you're done to restore the committed registry state. See `AGENTS.md` "Examples" section.

---

## File Structure

- Modify: `packages/phoria-islands/src/vite/plugin.ts` - fix `setEntry` double-prefixing the entry path with `root`.
- Modify: `packages/phoria-islands/src/vite/plugin.test.ts` - regression test for the entry path.
- Modify: `packages/phoria-react/src/client/csr.tsx` - define the react-refresh globals Phoria's .NET-served HTML never injects.
- Modify: `packages/phoria-react/src/client/csr.browser.test.tsx` - regression test for the globals.
- Modify: `packages/phoria-svelte/src/vite/plugin.ts` - externalize `svelte` for the `ssr` environment alongside the existing `@phoria/phoria-svelte/server` external.
- Modify: `packages/phoria-svelte/src/vite/plugin.test.ts` - regression test for the `ssr` environment's `resolve.external`.

---

## Task 1: Fix the `setEntry` dependency-scan warning

**Files:**
- Modify: `packages/phoria-islands/src/vite/plugin.ts:47-58` (`setEntry`) and its three call sites at lines 71 (`setClientEnvironment`), 95 (`setSsrEnvironment`), 124 (`setServerEnvironment`)
- Test: `packages/phoria-islands/src/vite/plugin.test.ts`

**Root cause:** `setEntry` builds `input` as `` `${root}/${entryFile}` `` (e.g. `"ui/src/entry-client.ts"`) and assigns it to `options.rolldownOptions.input`. But `setRoot` (line 15-19) already sets Vite's `config.root` to that same `appsettings.root` value (`"ui"`). Vite resolves a relative `rolldownOptions.input` string **against `config.root`**, so `"ui/src/entry-client.ts"` resolves to `"ui/ui/src/entry-client.ts"`, which doesn't exist. This only surfaces as a warning during the dev server's on-demand dependency scan (`(!) Failed to run dependency scan... failed to resolve rolldownOptions.input value: "ui/src/entry-client.ts"`) - a full `vite build` happens to still find the file (rolldown's build-time entry resolution is more lenient), which is why this went unnoticed. The fix is to stop prefixing with `root` at all: since `entryFile` is already relative to `root`, and `root` is already Vite's config root, passing `entryFile` alone resolves correctly in both the scan and the build.

**Interfaces:**
- Produces: `setEntry(options: BuildEnvironmentOptions, entryFile?: string)` (root parameter removed) - unchanged for other tasks, no other file references `setEntry`.

- [ ] **Step 1: Write the failing test**

Add to `packages/phoria-islands/src/vite/plugin.test.ts`, inside the existing `describe("phoria plugin", ...)` block, after the existing test:

```ts
	it("sets the entry input relative to root instead of double-prefixing it with root", async () => {
		const plugin = phoria({
			appsettings: { root: "ui", entry: "src/entry-client.ts", ssrEntry: "src/entry-server.ts" }
		}) as {
			config: (config: object, env: object) => Promise<void> | void
			configEnvironment: (name: string, options: EnvironmentOptions, env: object) => void
		}
		const options: EnvironmentOptions = {}

		await plugin.config({}, { command: "serve", mode: "development" })
		plugin.configEnvironment("client", options, { command: "serve", mode: "development" })

		expect(options.build?.rolldownOptions?.input).toBe("src/entry-client.ts")
	})
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `pnpm --filter @phoria/phoria test`
Expected: FAIL - `expected 'ui/src/entry-client.ts' to be 'src/entry-client.ts'`

- [ ] **Step 3: Fix `setEntry` and its call sites**

In `packages/phoria-islands/src/vite/plugin.ts`, replace:

```ts
function setEntry(options: BuildEnvironmentOptions, root?: string, entryFile?: string) {
	if (typeof entryFile === "undefined") {
		return
	}

	const input = root ? `${root}/${entryFile}` : entryFile

	options.rolldownOptions = {
		...options.rolldownOptions,
		input
	}
}
```

with:

```ts
function setEntry(options: BuildEnvironmentOptions, entryFile?: string) {
	if (typeof entryFile === "undefined") {
		return
	}

	// `entryFile` is relative to the Vite config root, which `setRoot` already
	// sets to `appsettings.root` - resolving it against root again here would
	// double up the root segment (e.g. "ui/ui/src/entry-client.ts").

	options.rolldownOptions = {
		...options.rolldownOptions,
		input: entryFile
	}
}
```

Then update the three call sites (drop the `appsettings.root` / `serverEntry`-only second argument):

- Line ~71, in `setClientEnvironment`: `setEntry(options.build, appsettings.root, appsettings.entry)` → `setEntry(options.build, appsettings.entry)`
- Line ~95, in `setSsrEnvironment`: `setEntry(options.build, appsettings.root, appsettings.ssrEntry)` → `setEntry(options.build, appsettings.ssrEntry)`
- Line ~124, in `setServerEnvironment`: `setEntry(options.build, appsettings.root, serverEntry)` → `setEntry(options.build, serverEntry)`

- [ ] **Step 4: Run the test to verify it passes**

Run: `pnpm --filter @phoria/phoria test`
Expected: PASS (all tests in the file, including the pre-existing one)

- [ ] **Step 5: Typecheck and lint**

Run: `pnpm --filter @phoria/phoria check && pnpm --filter @phoria/phoria lint`
Expected: both clean

- [ ] **Step 6: Manual verification (optional but recommended)**

With the example linked (`pnpm examples:link` at repo root, `pnpm build`, then from `examples/framework-multiple/WebApp`: `pnpm dev:server`), confirm the dev server no longer logs `(!) Failed to run dependency scan... rolldownOptions.input`. Run `pnpm examples:sync` at the repo root afterward.

- [ ] **Step 7: Commit**

```bash
git add packages/phoria-islands/src/vite/plugin.ts packages/phoria-islands/src/vite/plugin.test.ts
git commit -m "fix(phoria-islands): stop double-prefixing the entry input with root"
```

---

## Task 2: Fix React dev islands failing to hydrate

**Files:**
- Modify: `packages/phoria-react/src/client/csr.tsx`
- Test: `packages/phoria-react/src/client/csr.browser.test.tsx`

**Root cause:** Phoria serves the page HTML from the .NET host (via the `<phoria-island-scripts />` tag helper), never through Vite's own HTML pipeline. `@vitejs/plugin-react`'s Fast Refresh transform relies on a preamble (normally injected by Vite's `transformIndexHtml` into the served HTML) that runs `window.$RefreshReg$ = () => {}` and `window.$RefreshSig$ = () => (type) => type` before any transformed component module evaluates. Every dev-mode `.tsx` module the plugin transforms embeds a guard - `if (import.meta.hot && !inWebWorker) { if (!window.$RefreshReg$) throw new Error("@vitejs/plugin-react can't detect preamble. Something is wrong.") ... }` - that runs at module-evaluation time. Because Phoria's HTML never runs the preamble, the very first dynamic `import()` of a compiled React island component throws that error, and `csrMountMode.hydrate`/`render` never happens (confirmed directly in a browser: `await import(".../Counter.tsx")` throws `Error: @vitejs/plugin-react can't detect preamble. Something is wrong.` at the component module's own top level; defining the two globals before the import makes the same component mount and its counter increment normally).

**Interfaces:**
- Produces: `service.mount` (unchanged signature) now defines `window.$RefreshReg$` / `window.$RefreshSig$` (dev-only) before triggering the dynamic imports of `react`, `react-dom/client`, and the component module.

- [ ] **Step 1: Write the failing test**

Add to `packages/phoria-react/src/client/csr.browser.test.tsx`, inside the existing `describe("react csr service", ...)` block, after the existing test:

```tsx
	it("defines the react-refresh globals so islands can hydrate without Vite's HTML preamble", async () => {
		// Phoria serves HTML from the .NET host, so Vite's dev-only react-refresh
		// preamble (window.$RefreshReg$/$RefreshSig$, normally injected via
		// transformIndexHtml) never runs. Without it, @vitejs/plugin-react's
		// refresh transform throws "can't detect preamble" the first time a
		// compiled component module evaluates.

		window.$RefreshReg$ = undefined
		window.$RefreshSig$ = undefined

		const island = document.createElement("div")
		document.body.appendChild(island)

		const Hello = () => createElement("span", null, "Hello World")

		await service.mount(island, { name: "Hello", framework: "react", loader: async () => ({ default: Hello }) }, null, {
			mode: "render"
		})

		expect(typeof window.$RefreshReg$).toBe("function")
		expect(typeof window.$RefreshSig$).toBe("function")

		island.remove()
	})
```

Note: this test verifies the fix *mechanism* (the globals get defined), not the full crash-reproduction - Vitest's browser-mode dev server runs with HMR disabled (`hmr === false` internally short-circuits `@vitejs/plugin-react`'s refresh transform, so it never injects the `$RefreshReg$` guard in that environment), so the exact "can't detect preamble" throw cannot be reproduced inside this test harness. Step 6 covers real-world confirmation.

- [ ] **Step 2: Run the test to verify it fails**

Run: `pnpm --filter @phoria/phoria-react test:browser`
Expected: FAIL - `expected 'undefined' to be 'function'`

- [ ] **Step 3: Fix `csr.tsx`**

In `packages/phoria-react/src/client/csr.tsx`, add a global type declaration and set the globals at the top of `mount`:

```tsx
import { importComponent } from "@phoria/phoria"
import { csrMountMode, type PhoriaIslandComponentCsrService } from "@phoria/phoria/client"
import type { FunctionComponent } from "react"
import { framework } from "~/main"

declare global {
	interface Window {
		$RefreshReg$?: (type: unknown, id: string) => void
		$RefreshSig$?: () => (type: unknown) => unknown
	}
}

const service: PhoriaIslandComponentCsrService<typeof framework.name, FunctionComponent> = {
	mount: async (island, component, props, options) => {
		if (component.framework !== framework.name) {
			throw new Error(`${framework.name} cannot render the ${component.framework} component named "${component.name}".`)
		}

		// Phoria serves HTML from the .NET host, so Vite's dev-only react-refresh
		// preamble (normally injected via `transformIndexHtml`) never runs. Define
		// its globals here so `@vitejs/plugin-react`'s refresh transform doesn't
		// throw "can't detect preamble" the first time a component module evaluates.

		if (import.meta.env.DEV) {
			window.$RefreshReg$ ??= () => {}
			window.$RefreshSig$ ??= () => (type) => type
		}

		const mode = options?.mode ?? csrMountMode.hydrate

		Promise.all([
```

(the rest of the file - the `Promise.all([...]).then(...)` block and its contents - is unchanged)

- [ ] **Step 4: Run the test to verify it passes**

Run: `pnpm --filter @phoria/phoria-react test:browser`
Expected: PASS (both tests in the file)

- [ ] **Step 5: Run the remaining checks**

Run: `pnpm --filter @phoria/phoria-react test && pnpm --filter @phoria/phoria-react check && pnpm --filter @phoria/phoria-react lint`
Expected: all clean

- [ ] **Step 6: Manual verification (required - the automated test can't reproduce the real preamble crash)**

With the example linked and built (`pnpm examples:link`, `pnpm build`, `pnpm examples:refresh` at the repo root), from `examples/framework-multiple/WebApp` run `pnpm dev:server`, open the app in a browser, open the console, and confirm:
- No `Error: @vitejs/plugin-react can't detect preamble. Something is wrong.` is thrown.
- Clicking the React counter's button increments its count (proving it hydrated, not just rendered statically).

Run `pnpm examples:sync` at the repo root afterward.

- [ ] **Step 7: Commit**

```bash
git add packages/phoria-react/src/client/csr.tsx packages/phoria-react/src/client/csr.browser.test.tsx
git commit -m "fix(phoria-react): define react-refresh globals so dev islands can hydrate"
```

---

## Task 3: Fix Svelte dev islands crashing during SSR

**Files:**
- Modify: `packages/phoria-svelte/src/vite/plugin.ts:28-38` (`setSsrEnvironment`)
- Test: `packages/phoria-svelte/src/vite/plugin.test.ts`

**Root cause:** Same class of bug as the already-fixed Vue duplicate-module issue, but on the server side. `setSsrEnvironment` externalizes `@phoria/phoria-svelte/server` for the `ssr` Vite environment, which means it's loaded via **Node's native ESM loader**, not Vite's SSR module runner - and Node then resolves that dist's `import { render } from "svelte/server"` via its own module cache. Meanwhile, the compiled `*.svelte` component (e.g. `Counter.svelte`) is transformed and evaluated **by Vite's SSR module runner**, which resolves its `import ... from "svelte/internal/server"` (emitted by the Svelte compiler) through its own, separate module cache. Even though both resolve to the same file on disk, they end up as two independent JS module instances with two independent copies of `svelte/internal/server/context.js`'s module-level mutable `ssr_context` variable. The renderer (Node-loaded instance) sets `ssr_context` before calling into the component; the compiled component's `push_element` (runner-loaded instance) reads its own copy of `ssr_context`, which was never set, and crashes with `TypeError: Cannot read properties of null (reading 'function')` at `svelte/internal/server/dev.js:59`. Verified experimentally: a fresh `createServer` with `environments.ssr.optimizeDeps.exclude` for svelte still crashes (module graph inspection confirms a single *file* is used, but the crash persists because it's still two *instances* - Node ESM cache vs. runner cache); adding `svelte` to `environments.ssr.resolve.external` (so **both** the dist and the compiled component resolve `svelte` via Node's ESM loader) fixes it. Confirmed against the real `@phoria/phoria` example app after rebuilding `phoria-svelte`: `SvelteCounter` now SSRs to `<div class="svelte-counter ...">...count is 10...</div>` instead of throwing.

**Interfaces:**
- Produces: `setSsrEnvironment` sets `options.resolve.external = ["@phoria/phoria-svelte/server", "svelte"]` (was `["@phoria/phoria-svelte/server"]` only).

- [ ] **Step 1: Write the failing test**

Add `EnvironmentOptions` to the existing `vite` type-only import and add a new test to `packages/phoria-svelte/src/vite/plugin.test.ts`:

```ts
import type { EnvironmentOptions } from "vite"
import { describe, expect, it } from "vitest"
import { phoriaSvelte } from "./plugin"
```

...and, inside `describe("phoria-svelte plugin", ...)`, after the existing tests:

```ts
	it("externalizes svelte for the ssr environment so the compiled component and the SSR renderer share one module instance", () => {
		const plugins = phoriaSvelte() as unknown as {
			configEnvironment: (name: string, options: EnvironmentOptions) => void
		}[]
		const plugin = plugins[plugins.length - 1]
		const options: EnvironmentOptions = {}

		plugin.configEnvironment("ssr", options)

		expect(options.resolve?.external).toEqual(["@phoria/phoria-svelte/server", "svelte"])
	})
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `pnpm --filter @phoria/phoria-svelte test`
Expected: FAIL - received `["@phoria/phoria-svelte/server"]`, expected `["@phoria/phoria-svelte/server", "svelte"]`

- [ ] **Step 3: Fix `setSsrEnvironment`**

In `packages/phoria-svelte/src/vite/plugin.ts`, replace:

```ts
function setSsrEnvironment(options: EnvironmentOptions) {
	const external = ["@phoria/phoria-svelte/server"]
```

with:

```ts
function setSsrEnvironment(options: EnvironmentOptions) {
	// `@phoria/phoria-svelte/server` is externalized (loaded via Node's ESM
	// loader), so it resolves `svelte/server` outside of Vite's SSR module
	// runner. Externalizing `svelte` too keeps both sides on the same Node
	// module instance - otherwise the compiled `*.svelte` component (transformed
	// by Vite's SSR runner) gets its own copy of `svelte/internal/server`, whose
	// module-level `ssr_context` never sees the renderer's context and `push_element`
	// crashes reading `null`.

	const external = ["@phoria/phoria-svelte/server", "svelte"]
```

(the rest of the function - the `options.resolve ??= {}` block and the `external` push/assign logic - is unchanged)

- [ ] **Step 4: Run the test to verify it passes**

Run: `pnpm --filter @phoria/phoria-svelte test`
Expected: PASS (all three tests in the file)

- [ ] **Step 5: Typecheck and lint**

Run: `pnpm --filter @phoria/phoria-svelte check && pnpm --filter @phoria/phoria-svelte lint`
Expected: both clean

- [ ] **Step 6: Manual/integration verification against the real example app**

```bash
# from the repo root
pnpm examples:link
pnpm build
pnpm --filter @phoria/phoria-svelte build   # rebuild after the fix if not already covered by the root build
pnpm examples:refresh                       # required - file: hard links go stale on rebuild
```

Then, from `examples/framework-multiple/WebApp`, run `pnpm dev:server`, load a page containing `SvelteCounter`, and confirm:
- No `TypeError: Cannot read properties of null (reading 'function')` in the server console or the response.
- The Svelte counter's SSR'd markup renders (`count is <startAt>`) instead of the page failing to load.

Run `pnpm examples:sync` at the repo root afterward.

- [ ] **Step 7: Commit**

```bash
git add packages/phoria-svelte/src/vite/plugin.ts packages/phoria-svelte/src/vite/plugin.test.ts
git commit -m "fix(phoria-svelte): externalize svelte for ssr to avoid a duplicate module instance"
```
