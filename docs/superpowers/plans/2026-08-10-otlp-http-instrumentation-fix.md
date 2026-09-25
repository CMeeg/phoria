# OTLP HTTP/HTTPS Instrumentation Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure Phoria Server's `HttpInstrumentation` patches `node:http`/`node:https` even though those modules are already loaded before `sdk.start()`, so request metrics (`http.server.request.duration`) are recorded and exported in Production/Preview (not just in dev, where Vite's require graph incidentally re-requires them).

**Architecture:** After `NodeSDK.start()` registers the `require-in-the-middle` hook, call `process.getBuiltinModule("http")` and `process.getBuiltinModule("https")` to re-run the (now-registered) require hook against the preloaded core modules. This is idempotent — RITM returns already-patched modules from its cache without re-wrapping (verified in `require-in-the-middle@8.0.1/index.js:195-198`) — so the already-working dev path is unaffected. A regression test asserts the `Server.prototype.emit` patch is applied.

**Tech Stack:** Node.js, TypeScript, `@phoria/opentelemetry` (Vite-built ESM+CJS), OpenTelemetry SDK + `@opentelemetry/instrumentation-http`, Vitest, Biome.

## Global Constraints

- Follow Biome style: tabs, no semicolons, no trailing commas, line width 120, `lowerCamelCase` constants.
- No new dependencies.
- `pnpm biome check` must pass after each task.
- Add a Changeset for the published `@phoria/opentelemetry` package (patch bump).
- Do NOT commit unless the task's commit step says so.

---

### Task 1: Add a failing regression test

**Files:**
- Create: `packages/phoria-opentelemetry/src/observability.instrumentation.test.ts`

**Interfaces:**
- Consumes: `createPhoriaObservability(appsettings: PhoriaOtelAppSettings)` from `./observability`, `PhoriaOtelAppSettings` from `./appsettings`.
- Produces: nothing consumed by later tasks; it is the red test Task 2 must turn green.

- [ ] **Step 1: Write the test file**

`packages/phoria-opentelemetry/src/observability.instrumentation.test.ts`:

```ts
import http from "node:http"
import https from "node:https"
import { describe, expect, it } from "vitest"
import type { PhoriaOtelAppSettings } from "./appsettings"
import { createPhoriaObservability } from "./observability"

const metricsSettings: PhoriaOtelAppSettings = {
	root: "ui",
	base: "/ui",
	entry: "entry.ts",
	ssrBase: "/ssr",
	ssrEntry: "ssr.ts",
	server: { host: "localhost", https: false },
	build: { outDir: "dist" },
	observability: { logging: false, tracing: { enabled: false, samplingRatio: 0.1 }, metrics: true }
}

describe("createPhoriaObservability instrumentation", () => {
	it("patches preloaded node:http and node:https", async () => {
		const pristineHttpEmit = http.Server.prototype.emit
		const pristineHttpsEmit = https.Server.prototype.emit

		const observability = createPhoriaObservability(metricsSettings)

		expect(http.Server.prototype.emit).not.toBe(pristineHttpEmit)
		expect(https.Server.prototype.emit).not.toBe(pristineHttpsEmit)

		await observability.shutdown()
	})
})
```

Note: this must live in its own file. `observability.ts` is a module-level singleton (`initialized` flag set before the enabled-signals check), and the existing `observability.test.ts` already calls it with disabled settings — in the same module instance a later metrics-enabled call would early-return. Vitest isolates modules per file, so this new file starts with a clean state.

- [ ] **Step 2: Run the test to verify it fails**

Run: `pnpm --filter @phoria/opentelemetry exec vitest run src/observability.instrumentation.test.ts`
Expected: FAIL — `expect(http.Server.prototype.emit).not.toBe(pristineHttpEmit)` receives the original, unpatched `emit`. If it unexpectedly PASSES, something re-required `node:http` inside the vitest worker after `sdk.start()`; still proceed — the fix in Task 2 is required for Production regardless.

Note: `shutdown()` flushes the metric reader and the OTLP exporter may log a benign `ECONNREFUSED` to `127.0.0.1:4318` (no endpoint configured). This does not fail the test.

- [ ] **Step 3: Commit**

```bash
git add packages/phoria-opentelemetry/src/observability.instrumentation.test.ts
git commit -m "test(opentelemetry): assert http/https patch on observability start"
```

---

### Task 2: Implement the fix

**Files:**
- Modify: `packages/phoria-opentelemetry/src/observability.ts:62-63`

**Interfaces:**
- Consumes: existing `sdk`/`tracingEnabled`/`metricsEnabled` locals; Node's `process.getBuiltinModule` (available Node ≥20.16; package engines floor is `^20.19.0`).
- Produces: no API change — `createPhoriaObservability` signature and return shape are unchanged.

- [ ] **Step 1: Write the implementation**

In `packages/phoria-opentelemetry/src/observability.ts`, after `sdk.start()` (current line 63) add:

```ts
	sdk = new NodeSDK(config)
	sdk.start()

	if (tracingEnabled || metricsEnabled) {
		// `require-in-the-middle` only patches core modules as they are required.
		// Phoria's own static import chain loads node:http/node:https before
		// `sdk.start()`, so without this the patch never fires. Re-requiring via
		// `process.getBuiltinModule` runs the now-registered hook; RITM returns
		// already-patched modules unchanged, so this is idempotent in dev.
		process.getBuiltinModule("http")
		process.getBuiltinModule("https")
	}
```

Rationale for the comment: the code's purpose is non-obvious (why re-require modules that are already loaded), so it warrants an explanation per repo convention.

- [ ] **Step 2: Run the regression test to verify it passes**

Run: `pnpm --filter @phoria/opentelemetry exec vitest run src/observability.instrumentation.test.ts`
Expected: PASS (2 assertions). Also run the existing suite to confirm no regressions: `pnpm --filter @phoria/opentelemetry test` — Expected: all pass.

- [ ] **Step 3: Lint and type-check**

Run: `pnpm biome check packages/phoria-opentelemetry/src` and `pnpm --filter @phoria/opentelemetry check`
Expected: no errors.

- [ ] **Step 4: Commit**

```bash
git add packages/phoria-opentelemetry/src/observability.ts
git commit -m "fix(opentelemetry): patch preloaded http/https on observability start"
```

---

### Task 3: Changeset

**Files:**
- Create: `.changeset/<random-name>.md`

**Interfaces:**
- Produces: a Changeset that bumps `@phoria/opentelemetry` with a `patch` bump when the Release PR merges.

- [ ] **Step 1: Create the changeset**

Run: `pnpm changeset`
Select package `@phoria/opentelemetry`, choose **patch**, and describe:

> Fix Phoria Server request metrics (`http.server.request.duration`) missing in production by forcing the HTTP/HTTPS instrumentation patch to apply to the already-loaded core modules.

- [ ] **Step 2: Commit**

```bash
git add .changeset
git commit -m "chore: changeset for http instrumentation fix"
```

---

### Task 4: Build and refresh linked examples

**Files:**
- Build outputs: `packages/phoria-opentelemetry/dist/*`

- [ ] **Step 1: Build the workspace**

Run (from repo root): `pnpm build`
Expected: Turborepo builds all packages successfully; `@phoria/opentelemetry` produces fresh `dist/main.js`, `dist/main.cjs`.

- [ ] **Step 2: Refresh linked examples**

Run (from repo root): `pnpm examples:refresh`
Expected: the example's `file:`-linked packages point at the freshly built `dist`.

---

### Task 5: Manual verification in Preview (the user-observable fix)

**Files:**
- None — runtime verification only.

- [ ] **Step 1: Confirm the bug reproduced before this fix (optional but recommended)**

If you have a clean checkout of the pre-fix commit: `cd examples/framework-multiple/WebApp && pnpm preview`, open the app, load a page, wait ~60s, then check the Aspire dashboard → `phoria-server` → **Metrics**. Expected pre-fix: no `http.server.request.duration` (traces and logs still appear).

- [ ] **Step 2: Verify the fix in Preview**

From `examples/framework-multiple/WebApp`: `pnpm preview` (or `pnpm build && pnpm preview` to ensure the example's production bundle is current). Open the app, load a page (health-check `/hc` requests are intentionally ignored), wait ~60s for the first metric export interval. Check the Aspire dashboard → `phoria-server` → **Metrics**: `http.server.request.duration` should now appear with data.

- [ ] **Step 3: Tear down**

Run: `pnpm stop` (in `examples/framework-multiple/WebApp`).

---

### Task 6: Dev-mode re-verification

**Files:**
- None — runtime verification only.

- [ ] **Step 1: Verify dev still works (no regression)**

From `examples/framework-multiple/WebApp`: `pnpm dev` (Aspire dev flow). Load a page, wait ~60s, check the dashboard → `phoria-server` → **Metrics**: `http.server.request.duration` appears. Optionally set `OTEL_LOG_LEVEL=debug` on the Node server process and confirm the phoria-server console logs `Applying instrumentation patch for nodejs core module on require hook { module: http }`.

- [ ] **Step 2: Tear down**

Run: `pnpm stop` (in `examples/framework-multiple/WebApp`).

---

## Self-Review Notes

- **Spec coverage:** the plan covers the fix (Task 2), regression coverage (Task 1), release plumbing (Task 3), rebuild/refresh of linked examples (Task 4), and both runtime verifications — Preview where the bug manifests (Task 5) and dev where it must not regress (Task 6).
- **Placeholder scan:** all code, commands, and expected outputs are concrete; no TBDs.
- **Type consistency:** the test reuses the exact `PhoriaOtelAppSettings` shape from `observability.test.ts` and imports `createPhoriaObservability`/`PhoriaOtelAppSettings` from the same modules Task 2 modifies; the fix adds no new exported identifiers.
