# Phoria Server Extension APIs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the logger extension seam into core Phoria, make parsed appsettings generically extensible, and simplify the OpenTelemetry adapter and example APIs without duplicate appsettings reads.

**Architecture:** `@phoria/phoria` owns only the framework-neutral `PhoriaLogger`, `phoriaConsoleLogger`, and generic parsed Phoria settings shape. `@phoria/opentelemetry` consumes that shape, normalizes its own optional observability settings internally, returns the core logger contract, and exposes a readable h3 instrumentation wrapper. Examples parse appsettings once and pass the parsed object to the OTel factories.

**Tech Stack:** TypeScript, Vite, h3, Vitest, Biome, pnpm workspace catalogs, Phoria Server packages.

## Global Constraints

- Core `@phoria/phoria` must not depend on OpenTelemetry.
- Preserve existing public names `PhoriaLogger`, `createPhoriaLogger`, and `createPhoriaRequestSpanHook`.
- Add `phoriaConsoleLogger` as the core default logger.
- `parsePhoriaAppSettings<TAdditional>()` must return the typed generic extension while retaining arbitrary `phoria` JSON properties at runtime.
- `createPhoriaLogger(appsettings)` and `createPhoriaObservability(appsettings)` normalize OTel defaults internally; do not add a public `bindPhoriaObservabilityAppSettings` API.
- `withPhoriaOtelInstrumentation(appsettings)` delegates to `createPhoriaRequestSpanHook({ base: appsettings.base, ssrBase: appsettings.ssrBase })`.
- Move `defu` to the workspace catalog at `^6.1.7`; package manifests use `"defu": "catalog:"`.
- Use tabs in TypeScript, Biome formatting, semicolons-as-needed, and no unnecessary comments.
- Examples must use one `parsePhoriaAppSettings` call and remain compatible with local linking before the unpublished package is released.

---

### Task 1: Core logger and generic appsettings

**Files:**
- Create: `packages/phoria-islands/src/server/logger.ts`
- Modify: `packages/phoria-islands/src/server/routing.ts`
- Modify: `packages/phoria-islands/src/server/appsettings.ts`
- Modify: `packages/phoria-islands/src/server/main.ts`
- Modify: `packages/phoria-islands/src/server/appsettings.test.ts`
- Modify: `packages/phoria-islands/package.json`
- Modify: `packages/phoria-opentelemetry/package.json`
- Modify: `pnpm-workspace.yaml`

**Interfaces:**
- Produces `PhoriaLogger`, `phoriaConsoleLogger`, and `PhoriaAppSettings<TAdditional = object>` for later tasks.
- `parsePhoriaAppSettings<TAdditional>(options?)` returns `Promise<PhoriaAppSettings<TAdditional>>`.

- [ ] **Step 1: Add the core logger module and failing export test**

Create `server/logger.ts` with:

```ts
interface PhoriaLogger {
	info(message: string, data?: Record<string, unknown>): void
	warn(message: string, data?: Record<string, unknown>): void
	error(message: string, data?: Record<string, unknown>): void
}

const phoriaConsoleLogger: PhoriaLogger = {
	info: (message, data) => console.info(message, data),
	warn: (message, data) => console.warn(message, data),
	error: (message, data) => console.error(message, data)
}
```

Export the type and value from `server/main.ts`. Update `routing.ts` to import them, delete its local declarations, and use `phoriaConsoleLogger` as the default for both handler factories.

- [ ] **Step 2: Implement generic appsettings typing**

Split the current concrete settings interface into a base interface plus an extension intersection. Preserve all existing base fields (`root`, `base`, `entry`, `ssrBase`, `ssrEntry`, `server`, `build`) and implement:

```ts
type PhoriaAppSettings<TAdditional extends object = object> =
	PhoriaBaseAppSettings & TAdditional
```

Use a type alias because TypeScript interfaces cannot extend an unconstrained generic type parameter. Apply the generic to `PhoriaAppSettingsOptions.inlineSettings`, `getPhoriaAppSettings`, and `parsePhoriaAppSettings`; preserve default merging and required `entry`/`ssrEntry` validation.

- [ ] **Step 3: Add generic parser regression coverage**

Extend `appsettings.test.ts` with a typed extension fixture such as `{ observability?: { logging: boolean } }`. Verify `parsePhoriaAppSettings<Extension>` returns the extension property from the merged JSON and still applies the existing core defaults/validation. Keep filesystem fixtures isolated using the existing test patterns.

- [ ] **Step 4: Move `defu` into the catalog**

Add `defu: ^6.1.7` to `pnpm-workspace.yaml`. Change both `packages/phoria-islands/package.json` and `packages/phoria-opentelemetry/package.json` from a literal `^6.1.7` to `catalog:`. Regenerate the root lockfile with `pnpm install` and ensure no unrelated dependency changes are introduced.

- [ ] **Step 5: Verify and commit core changes**

Run:

```bash
pnpm --filter @phoria/phoria check
pnpm --filter @phoria/phoria lint
pnpm --filter @phoria/phoria test
```

Review `git diff --stat`, stage only Task 1 files, and commit `refactor(phoria): expose logger and generic appsettings extensions`.

### Task 2: OTel normalization and instrumentation wrapper

**Files:**
- Modify: `packages/phoria-opentelemetry/src/appsettings.ts`
- Modify: `packages/phoria-opentelemetry/src/logger.ts`
- Modify: `packages/phoria-opentelemetry/src/observability.ts`
- Modify: `packages/phoria-opentelemetry/src/request-spans.ts`
- Modify: `packages/phoria-opentelemetry/src/main.ts`
- Modify: `packages/phoria-opentelemetry/src/appsettings.test.ts`
- Modify: `packages/phoria-opentelemetry/src/logger.test.ts`
- Modify: `packages/phoria-opentelemetry/src/observability.test.ts`
- Modify: `packages/phoria-opentelemetry/src/request-spans.test.ts`
- Modify: `packages/phoria-opentelemetry/package.json`

**Interfaces:**
- Consumes `PhoriaAppSettings<TAdditional>` and `PhoriaLogger` from Task 1.
- Produces `PhoriaOtelAppSettings`, `withPhoriaOtelInstrumentation(appsettings)`, and changes `createPhoriaLogger`/`createPhoriaObservability` to accept parsed generic appsettings and normalize `appsettings.observability` internally.

- [ ] **Step 1: Replace file-reading observability parsing with normalization**

Remove the OTel-specific filesystem reader and environment file discovery. Keep the OTel settings/default types and add an internal normalizer equivalent to:

```ts
function getPhoriaObservabilitySettings(appsettings: PhoriaOtelAppSettings): PhoriaObservabilityAppSettings {
	return defu(appsettings.observability, defaultObservabilityAppSettings) as PhoriaObservabilityAppSettings
}
```

Do not export this normalizer unless implementation needs a focused public API; the approved design keeps it private. Remove the old `parsePhoriaObservabilityAppSettings` export.

- [ ] **Step 2: Update OTel factories and logger contract**

Change `createPhoriaLogger` and `createPhoriaObservability` to accept `PhoriaOtelAppSettings`, call the private normalizer once, and use the normalized signal settings. When logging is disabled, return the imported core `phoriaConsoleLogger`; when enabled, return an object typed as the core `PhoriaLogger`. Add `@phoria/phoria` as a peer dependency with range `>=0.4.0 <1.0.0` and as a workspace dev dependency so the OTel package can import the core logger contract/default while consumers provide their existing core package.

Keep existing OTel behavior, including tracing/metrics/logging gates, sampler, `/hc` instrumentation filter, and shutdown semantics. The core package must not gain any OTel dependency.

- [ ] **Step 3: Add the readable instrumentation wrapper**

Add and export:

```ts
function withPhoriaOtelInstrumentation(appsettings: Pick<PhoriaAppSettings, "base" | "ssrBase">) {
	return createPhoriaRequestSpanHook({ base: appsettings.base, ssrBase: appsettings.ssrBase })
}
```

Export `PhoriaOtelAppSettings` from the package entry point so both examples can use the same extension type when calling the generic core parser.

Retain `createPhoriaRequestSpanHook` as the lower-level export. Add a test that supplies custom `base`/`ssrBase` values and verifies the wrapper returns working hooks with the same path classification behavior.

- [ ] **Step 4: Update OTel tests**

Replace parser/file fixture tests with parsed-settings normalization tests. Verify absent observability settings produce logging false, tracing disabled with sampling ratio `0.1`, and metrics false; verify partial settings merge correctly. Preserve logger fallback and enabled-provider tests.

- [ ] **Step 5: Verify and commit OTel changes**

Run:

```bash
pnpm --filter @phoria/opentelemetry check
pnpm --filter @phoria/opentelemetry lint
pnpm --filter @phoria/opentelemetry test
```

Stage only Task 2 files and commit `refactor(opentelemetry): consume generic Phoria appsettings`.

### Task 3: Migrate both examples

**Files:**
- Modify: `examples/getting-started/WebApp/ui/src/server.ts`
- Modify: `examples/framework-multiple/WebApp/ui/src/server.ts`

**Interfaces:**
- Consumes the generic parser, `createPhoriaLogger(appsettings)`, `createPhoriaObservability(appsettings)`, and `withPhoriaOtelInstrumentation(appsettings)` from Task 1/2.

- [ ] **Step 1: Update imports and typed parser calls**

Import `type PhoriaOtelAppSettings` if exported, or define the structural extension type locally using the public `PhoriaAppSettings` type. Parse once:

```ts
const appsettings = await parsePhoriaAppSettings<PhoriaOtelAppSettings>({
	environment: dotnetEnv,
	cwd: __dirname
})
```

Delete the second observability parser call.

- [ ] **Step 2: Use the OTel factories and wrapper**

Replace the current settings parser/factory setup with:

```ts
const phoriaLogger = createPhoriaLogger(appsettings)
const observability = createPhoriaObservability(appsettings)
```

Replace the h3 construction with:

```ts
const app = createApp(withPhoriaOtelInstrumentation(appsettings))
```

Preserve all existing handler routing, error logging, listener options, and shutdown behavior.

- [ ] **Step 3: Verify each example locally**

After `pnpm build` at the repository root, use the supported local-link workflow and run in each example `WebApp` directory:

```bash
pnpm check
pnpm lint
pnpm build
```

Restore registry refs with `pnpm examples:sync`; do not commit local links or generated lockfile changes.

- [ ] **Step 4: Commit example migration**

Stage only the two `server.ts` files and commit `refactor(examples): use generic Phoria server extension APIs`.

### Task 4: Full verification and integration review

**Files:**
- No planned source changes; only reports or targeted fixes if verification finds a real regression.

- [ ] **Step 1: Run repository verification**

Run in order:

```bash
pnpm build
pnpm lint
pnpm check
pnpm test
dotnet test --solution Phoria.sln --configuration Release
```

Expected: all JS workspace tasks pass, OTel tests include the normalization and wrapper cases, and 108+ .NET tests pass on net8.0 and net10.0.

- [ ] **Step 2: Verify examples and scope**

Run `pnpm examples:link`, both example `check`/`lint`/`build` sequences, `pnpm examples:sync`, and `pnpm examples:check`. Confirm `git diff --check`, `git status --short`, and `git diff --stat` show only intended implementation commits/files; ignored local `TODO.md` and `.superpowers/` reports must not be staged.

- [ ] **Step 3: Review public exports and dependency changes**

Confirm `@phoria/phoria/server` exports `PhoriaLogger`, `phoriaConsoleLogger`, generic `PhoriaAppSettings`, and `parsePhoriaAppSettings`; confirm `@phoria/opentelemetry` exports `createPhoriaLogger`, `createPhoriaObservability`, and `withPhoriaOtelInstrumentation` without the removed parser/binder. Confirm `defu` appears once in the workspace catalog and both package manifests use `catalog:`.

- [ ] **Step 4: Commit any verified fixes and report residual limitations**

If verification exposes only the known OpenTelemetry .NET 1.17.0 limitation that metrics instrumentation has no per-request `/hc` filter, do not add an incompatible dependency to core; document it as a residual limitation. Otherwise fix the smallest verified regression, rerun the affected command, and leave the worktree clean.
