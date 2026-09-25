# Phase 1: Dependency & Platform Updates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring every Phoria dependency and platform target up to date — Vite 8 (Rolldown/Oxc), TypeScript 6, Vitest 4, Biome 2, Lerna 9, pnpm 11, Node 24 LTS, .NET 10 + xUnit v3 — behind a regression net that is extended *first* to cover the seams the upgrade is most likely to break.

**Architecture:** Strictly sequenced, one-variable-at-a-time. Task 1 closes the untested `ssr-manifest → __phoriaComponentPath → preload tag helper` seam before any dependency moves. Tasks 2–6 land upgrades that are *independent of the bundler* (toolchain floor, repo tooling, TypeScript, Vitest, .NET) so each failure is attributable. Tasks 7–9 then change only the bundler. Tasks 10–12 decouple h3, sweep remaining minors, and land CI/docs/changesets.

**Tech Stack:** pnpm 11 workspaces + catalog, Lerna 9 (publishing), Nx (caching), Vite 8, TypeScript 6, Vitest 4, Biome 2, Changesets, .NET 10 SDK, xUnit v3 on Microsoft.Testing.Platform.

## Global Constraints

- **Node:** `.nvmrc` = `v24.18.0`. Package `engines.node` = `"^20.19.0 || ^22.12.0 || >=24.0.0"` (the Vite 8 / plugin-react 6 / vite-plugin-svelte 7 floor).
- **pnpm:** `11.17.0`. **All** pnpm settings live in `pnpm-workspace.yaml` (pnpm 10+ moved them out of `package.json`/`.npmrc`; only auth/registry stay in `.npmrc`).
- **.NET:** SDK `10.0.302`, `rollForward: latestFeature`. `Phoria.csproj` targets `net8.0;net10.0`. **net9.0 is dropped** — it is STS and reaches EOL on the same day as net8 (2026-11-10), so it carries cost with no coverage benefit.
- **Vite:** `^8.1.5`. Vite 8 replaces esbuild/Rollup with Oxc/Rolldown and Lightning CSS.
- **TypeScript:** `~6.0.3`. **Not 7** — TS 7 ships no programmatic API, so `unplugin-dts` (behind `vite-plugin-dts`) and Volar (Vue/Svelte) cannot use it.
- **h3:** `^1.15.11` (v1 line). h3 v2 is `2.0.1-rc.26` — an RC; 1.0.0 will not ship a runtime dependency on an RC.
- **Biome:** `^2.5.5`, config: tabs, semicolons as-needed, no trailing commas, line width 120, lf.
- **Central NuGet versions** via `Directory.Packages.props`; `PackageReference` carries no `Version`.
- **Every task ends green:** `pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && pnpm test:browser && dotnet test Phoria.sln` plus `pnpm --filter framework-multiple test:smoke`. Do not proceed to the next task on red.
- **One changeset per task** that changes published package behaviour, written at the end of that task.

---

## File Structure

**New files**

- `packages/Phoria.Tests/Vite/ViteSsrManifestTests.cs` — `ViteSsrManifest` lookup + key-derivation contract.
- `packages/Phoria.Tests/Islands/PhoriaIslandPreloadTagHelperTests.cs` — asserts emitted `<link>` markup for a known ssr-manifest + componentPath pair.
- `packages/Phoria.Tests/TestData/ssr-manifest.json` — a golden ssr-manifest captured from a real `framework-multiple` build (the Vite 6 baseline).
- `packages/phoria-islands/src/server/phoria-island.test.ts` — unit tests for `PhoriaIsland.create` (possible once h3 is decoupled in Task 10).

**Modified — config / manifests**

- `.nvmrc`, `global.json`, `package.json` (root), `pnpm-workspace.yaml`, `biome.jsonc`, `nx.json`, `lerna.json`, `.changeset/config.json`
- `Directory.Build.props`, `Directory.Packages.props`, `packages/Phoria/Phoria.csproj`, `packages/Phoria.Tests/Phoria.Tests.csproj`, both `e2e/**/WebApp.csproj`
- All 5 package `package.json` (`engines`, `peerDependencies`, `devDependencies`)
- All 7 `tsconfig*.json`
- All 5 library `vite.config.ts`; all 7 `vitest*.config.ts`
- Both e2e `vite.config.ts`; both `vite.server.config.ts` (**deleted** in Task 9)
- `.github/workflows/ci.yml`, `.github/workflows/release.yml`

**Modified — source**

- `packages/phoria-islands/src/vite/plugin.ts` — `setEntry` clobber fix, SSR `copyPublicDir`, new `server` environment, `buildApp` hook
- `packages/phoria-{react,svelte,vue}/src/vite/plugin.ts` — `applyToEnvironment` scoping
- `packages/phoria-islands/src/server/routing.ts` — Phoria-owned handler type, Vite's `isRunnableDevEnvironment`
- `packages/phoria-islands/src/server/phoria-island.ts` — remove `H3Event` from the public signature
- `packages/Phoria/Server/PhoriaServerProcess.cs`, `packages/Phoria/IO/StreamPool.cs` (analyzer/warning fixes only — **behavioural server fixes are Phase 2, not here**)
- `docs/guides/getting-started.md`, `AGENTS.md`, `docs/MEMORY.md`

---

## Task 1: Close the ssr-manifest → preload test gap (before any dependency moves)

This is the least-defended, highest-risk seam in the repo and it is exactly what Rolldown changes silently. `ssr-manifest.json` keys look like `"../../\u0000/abs/path/react-dom/cjs/react-dom-client.production.js?commonjs-exports"`; Rolldown produces different module IDs and drops `?commonjs-*` suffixes entirely.

**Files:**

- Create: `packages/Phoria.Tests/TestData/ssr-manifest.json`
- Create: `packages/Phoria.Tests/Vite/ViteSsrManifestTests.cs`
- Create: `packages/Phoria.Tests/Islands/PhoriaIslandPreloadTagHelperTests.cs`
- Modify: `packages/Phoria.Tests/Phoria.Tests.csproj` (copy test data to output)
- Modify: `e2e/framework-multiple/tests/smoke.test.ts` (assert modulepreload output)

**Interfaces:**

- Consumes: `Phoria.Vite.ViteSsrManifest(IReadOnlyDictionary<string, string[]>)`, `IViteSsrManifest.this[string]`; `Phoria.Islands.PhoriaIslandPreloadTagHelper`, `IPhoriaIslandScopedContext`, `PhoriaIsland.ComponentPath`.
- Produces: a golden `ssr-manifest.json` baseline and a passing assertion on the exact `<link>` markup, so Task 7 (Vite 8) fails loudly rather than silently degrading preloading.

- [ ] **Step 1: Capture the Vite 6 golden ssr-manifest**

Build the e2e app on the *current* toolchain and copy the real manifest:

```bash
pnpm install
pnpm lerna run build
pnpm --filter framework-multiple build:islands
cp e2e/framework-multiple/WebApp/ui/dist/phoria/client/.vite/ssr-manifest.json \
   packages/Phoria.Tests/TestData/ssr-manifest.json
```

Then hand-edit the copied file to replace machine-specific absolute paths with a stable placeholder root (e.g. `/repo`) so the test is portable. Keep at least: one `Counter` component key per framework, one `?commonjs-exports` key, and one key whose value list contains a second-level lookup key.

- [ ] **Step 2: Make the test data available to the test assembly**

In `packages/Phoria.Tests/Phoria.Tests.csproj`, add before `</Project>`:

```xml
	<ItemGroup>
		<None Include="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
	</ItemGroup>
```

- [ ] **Step 3: Write the failing `ViteSsrManifest` tests**

`packages/Phoria.Tests/Vite/ViteSsrManifestTests.cs`:

```csharp
using System.Text.Json;
using Phoria.Vite;

namespace Phoria.Tests.Vite;

public class ViteSsrManifestTests
{
	private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

	private static ViteSsrManifest LoadGoldenManifest()
	{
		string path = Path.Combine(AppContext.BaseDirectory, "TestData", "ssr-manifest.json");
		using FileStream stream = File.OpenRead(path);

		IReadOnlyDictionary<string, string[]> files =
			JsonSerializer.Deserialize<IReadOnlyDictionary<string, string[]>>(stream, jsonOptions)!;

		return new ViteSsrManifest(files);
	}

	[Fact]
	public void Indexer_ComponentPathKey_ReturnsChunkFiles()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		string[]? files = manifest["src/components/Counter/Counter.tsx"];

		Assert.NotNull(files);
		Assert.NotEmpty(files);
		Assert.All(files, f => Assert.StartsWith("/ui/assets/", f, StringComparison.Ordinal));
	}

	[Fact]
	public void Indexer_UnknownKey_ReturnsNull()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		Assert.Null(manifest["does/not/exist.tsx"]);
	}

	[Fact]
	public void Indexer_FilenameOnlyKey_SupportsSecondLevelLookup()
	{
		// PhoriaIslandPreloadTagHelper does a second lookup using Path.GetFileName(file).
		// This asserts that contract holds against a real manifest.
		ViteSsrManifest manifest = LoadGoldenManifest();

		string[]? files = manifest["src/components/Counter/Counter.tsx"];
		Assert.NotNull(files);

		string filename = Path.GetFileName(files[0]);

		// Either the second-level lookup resolves, or it is legitimately absent -
		// what matters is that it does not throw and returns a well-formed result.
		string[]? deps = manifest[filename];
		if (deps != null)
		{
			Assert.All(deps, d => Assert.StartsWith("/", d, StringComparison.Ordinal));
		}
	}
}
```

> Adjust the literal component key in step 3 to match whatever your golden manifest actually contains — read the captured file and use a real key. Do **not** invent one.

- [ ] **Step 4: Run and confirm the tests fail for the right reason**

Run: `dotnet test Phoria.sln --filter-class Phoria.Tests.Vite.ViteSsrManifestTests`

Expected: FAIL — `FileNotFoundException` on `TestData/ssr-manifest.json` if step 1/2 were skipped, or an assertion failure naming a key you got wrong. Fix the key literals until green. This is a characterisation test: it must pass on Vite 6 *unchanged*.

- [ ] **Step 5: Write the preload tag-helper test**

`packages/Phoria.Tests/Islands/PhoriaIslandPreloadTagHelperTests.cs` — construct the tag helper with a stub `IViteSsrManifestReader` returning the golden manifest, a stub `IPhoriaIslandScopedContext` containing one `PhoriaIsland` whose `ComponentPath` matches a golden key, and a stub `IUrlHelperFactory`. Assert the rendered `TagHelperOutput.Content` contains exactly the expected `<link rel="modulepreload" href="...">` for each file, with no duplicates.

Run: `dotnet test Phoria.sln --filter-class Phoria.Tests.Islands.PhoriaIslandPreloadTagHelperTests`

Expected: PASS on the current toolchain.

- [ ] **Step 6: Add a smoke assertion on rendered preload tags**

In `e2e/framework-multiple/tests/smoke.test.ts`, add alongside the existing `phoria-island` assertion:

```ts
it("emits modulepreload directives for server-rendered islands", async () => {
	const res = await fetch("http://localhost:5247")
	const html = await res.text()

	expect(html).toMatch(/<link\s+rel="modulepreload"\s+href="\/ui\/assets\/[^"]+\.js"/)
})
```

Run: `pnpm --filter framework-multiple build && pnpm --filter framework-multiple preview & ... && pnpm --filter framework-multiple test:smoke`

Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add packages/Phoria.Tests e2e/framework-multiple/tests/smoke.test.ts
git commit -m "test: cover ssr-manifest key derivation and preload directive output"
```

---

## Task 2: Toolchain floor — Node 24 LTS + pnpm 11 + engines

Must be first among the upgrades: pnpm 11 requires Node >= 22.13, and Vite 8 / plugin-react 6 / vite-plugin-svelte 7 all require Node >= 20.19 / 22.12. The current `v22.11.0` is below every one of those.

**Files:** `.nvmrc`, root `package.json`, `pnpm-workspace.yaml`, all 5 package `package.json`.

**Interfaces:**

- Produces: `pnpm-workspace.yaml` becomes the home for **all** pnpm settings (`peerDependencyRules`, `overrides`, `onlyBuiltDependencies`) — Tasks 3 and 7 rely on this.

- [ ] **Step 1: Bump `.nvmrc`** to `v24.18.0` (Active LTS until 2026-10-20, maintenance to 2028-04-30).

- [ ] **Step 2: Bump `packageManager`** in root `package.json` to `pnpm@11.17.0+sha512.<hash>` — obtain the real hash with `corepack use pnpm@11.17.0`, which rewrites the field for you. Do not hand-write the hash.

- [ ] **Step 3: Bump `engines.node`** in all five package `package.json` files from `"^18.17.1 || ^20.3.0 || >=22.0.0"` to `"^20.19.0 || ^22.12.0 || >=24.0.0"`. Hand-format with tabs (package.json is Biome-excluded until Task 3).

- [ ] **Step 4: Install and verify**

Run: `nvm install && nvm use && corepack enable && pnpm install`

Expected: install succeeds with no `EBADENGINE` warnings.

- [ ] **Step 5: Full verification**

Run: `pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && dotnet test Phoria.sln`

Expected: all green (nothing but the runtime changed).

- [ ] **Step 6: Changeset + commit**

```bash
pnpm changeset   # patch all 5 packages: "Raise minimum Node.js to ^20.19.0 || ^22.12.0 || >=24.0.0"
git add .nvmrc package.json packages/*/package.json .changeset
git commit -m "chore: raise Node baseline to 24 LTS and pnpm to 11"
```

---

## Task 3: Repo tooling — Biome 2, Lerna 9, Changesets 2.31

Done early and separately so that the lint/format churn does not confound the Vite and TypeScript tasks. Includes the sanctioned re-evaluation of the `package.json` formatting exclusion.

**Files:** root `package.json`, `biome.jsonc`, `lerna.json`, `.changeset/config.json`, plus whatever `biome check --write` reformats.

- [ ] **Step 1: Bump the dev dependencies**

In root `package.json` `devDependencies`: `@biomejs/biome` → `^2.5.5`, `@changesets/cli` → `^2.31.1`, `lerna` → `^9.0.7`. Then `pnpm install`.

- [ ] **Step 2: Run Biome's own migration**

Run: `pnpm biome migrate --write`

This handles `files.ignore`/`include` → `files.includes`, `organizeImports` → `assist.actions.source.organizeImports`, and severity pinning for `style` rules (which no longer error by default in v2).

- [ ] **Step 3: Hand-finish `biome.jsonc`**

The migration will not do all of it. The result should be:

```jsonc
{
	"$schema": "https://biomejs.dev/schemas/2.5.5/schema.json",
	"files": {
		"includes": ["**", "!**/*.min.css", "!**/*.min.css.map", "!!**/dist", "!!**/node_modules"],
		"ignoreUnknown": false
	},
	"vcs": {
		"enabled": true,
		"clientKind": "git",
		"defaultBranch": "main",
		"useIgnoreFile": true
	},
	"linter": {
		"enabled": true,
		"rules": {
			"preset": "recommended"
		}
	},
	"assist": {
		"enabled": true,
		"actions": {
			"source": {
				"organizeImports": "on"
			}
		}
	},
	"formatter": {
		"enabled": true,
		"formatWithErrors": true,
		"indentStyle": "tab",
		"indentWidth": 2,
		"lineEnding": "lf",
		"lineWidth": 120,
		"useEditorconfig": true
	},
	"javascript": {
		"linter": { "enabled": true },
		"formatter": {
			"enabled": true,
			"semicolons": "asNeeded",
			"trailingCommas": "none"
		}
	},
	"json": {
		"linter": { "enabled": true },
		"formatter": { "enabled": true }
	},
	"css": {
		"parser": { "cssModules": true },
		"linter": { "enabled": true },
		"formatter": { "enabled": true }
	}
}
```

Three deliberate changes beyond the migration:

1. `linter.rules.recommended: true` → `linter.rules.preset: "recommended"` (`recommended` is deprecated in v2).
2. `!!**/dist` / `!!**/node_modules` force-ignore patterns — v2 has a scanner that indexes files for the `project` domain; force-ignoring output dirs avoids pointless indexing.
3. **`formatter.ignore: ["**/package.json"]` is removed.** Biome 2 defaults `package.json` to `expand: "always"`, which formats objects and arrays on multiple lines regardless of width — the exact shape Changesets writes. The v1 conflict that motivated the exclusion no longer exists.

- [ ] **Step 4: Prove the package.json exclusion is genuinely unnecessary — do not assume**

```bash
pnpm biome check --write .
git diff --stat            # review the churn, especially in package.json files
```

Then simulate a Changesets version bump and confirm Biome agrees with its output:

```bash
pnpm changeset            # create a throwaway patch changeset for @phoria/phoria
pnpm changeset version    # rewrites package.json files + CHANGELOG.md
pnpm biome check .        # MUST be clean
```

Expected: `biome check` reports no formatting diagnostics on the Changesets-rewritten `package.json` files.

**If it is not clean, stop.** Restore `formatter.includes` with `"!**/package.json"`, leave the `AGENTS.md` note in place, and record the reason in `docs/MEMORY.md`. Do not force it.

Then discard the simulation:

```bash
git checkout -- . && git clean -fd .changeset
```

- [ ] **Step 5: Bump Lerna and verify Nx still caches**

`lerna@9` depends on `nx >=21.5.3 <23.0.0` and resolves it itself. `lerna.json` needs no change beyond confirming the `$schema` path still resolves.

Run: `pnpm lerna run build && pnpm lerna run build`

Expected: the second run reports Nx cache hits for every package.

- [ ] **Step 6: Full verification**

Run: `pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && pnpm test:browser && dotnet test Phoria.sln`

- [ ] **Step 7: Update `AGENTS.md`**

If step 4 passed, delete the "**`package.json` files are excluded from Biome formatting**" gotcha and the matching bullet under *Constraints* in `docs/PROJECT.md`. Replace with a one-liner noting Biome 2 formats `package.json` with `expand: always`, which matches Changesets.

- [ ] **Step 8: Commit** (`chore: upgrade Biome to 2, Lerna to 9, Changesets to 2.31`). No changeset — none of this affects published packages.

---

## Task 4: TypeScript 6 + vite-plugin-dts 5

Sequenced before Vite 8 so `check` is green before the bundler changes. TS 6 makes several previously-optional things mandatory.

**Files:** `pnpm-workspace.yaml` catalog, all 7 `tsconfig*.json`, all 5 library `vite.config.ts`.

- [ ] **Step 1: Bump the catalog**

In `pnpm-workspace.yaml`: `typescript: ~6.0.3`, `vite-plugin-dts: ^5.0.3`, `@types/node: ^24.13.3`. Then `pnpm install`.

- [ ] **Step 2: Migrate every tsconfig for TS 6's new defaults**

TS 6 changes that bite this repo:

- **`baseUrl` is no longer supported.** All 7 tsconfigs set `"baseUrl": "."`. Remove it, and rewrite `paths` to be relative to the project root (which, for every config here, is the same directory `baseUrl` pointed at — so the `paths` values are unchanged; only the `baseUrl` line is deleted).
- **`types` now defaults to `[]`.** Any config relying on ambient `@types/*` globals must list them explicitly.
- `strict` now defaults to `true` — already set everywhere, no-op.
- `module` now defaults to `esnext` — already set, no-op.
- `esModuleInterop`/`allowSyntheticDefaultImports` can no longer be `false` — already `true`, no-op.

For `packages/phoria-islands/tsconfig.json` the diff is:

```jsonc
{
	"compilerOptions": {
		"allowImportingTsExtensions": true,
		// "baseUrl": ".",              <- DELETE
		"esModuleInterop": true,
		"isolatedModules": true,
		"lib": ["ES2022", "DOM", "DOM.Iterable"],
		"module": "ESNext",
		"moduleDetection": "force",
		"moduleResolution": "Bundler",
		"noEmit": true,
		"noFallthroughCasesInSwitch": true,
		"noUncheckedSideEffectImports": true,
		"noUnusedLocals": true,
		"noUnusedParameters": true,
		"paths": {
			"~/*": ["src/*"]
		},
		"skipLibCheck": true,
		"sourceMap": true,
		"strict": true,
		"target": "ES2022",
		"types": ["node", "vite/client"],   // <- ADD (was implicit)
		"useDefineForClassFields": true,
		"verbatimModuleSyntax": true
	},
	"include": ["src/**/*.ts", "src/**/*.d.ts"]
}
```

Apply the same `baseUrl` deletion to all 7. For `types`, set per package:

- `phoria-islands`: `["node", "vite/client"]`
- `phoria-react`: `["node", "vite/client"]` (React types come from imports, not globals)
- `phoria-svelte`, `phoria-vue`: `["node", "vite/client"]`
- `vite-plugin-dotnet-dev-certs`: `["node"]`
- both e2e `tsconfig.json`: `["node", "vite/client"]`
- both e2e `tsconfig.node.json`: `["node"]`

- [ ] **Step 3: Run the type-check and iterate**

Run: `pnpm lerna run check`

Expected: initially FAIL. Likely causes, in order of probability: a missing entry in `types` (symptom: `Cannot find name 'process'` / `import.meta.env` errors); `paths` no longer resolving (symptom: `Cannot find module '~/register'`). Fix by adding the missing `types` entry or correcting the `paths` value — **not** by loosening `strict` or adding `// @ts-ignore`.

- [ ] **Step 4: Verify `.d.ts` output is byte-identical apart from expected changes**

`vite-plugin-dts@5` is a thin wrapper over `unplugin-dts@1`. Zero options are passed today, so no config change is required — but the emit must be checked, not assumed:

```bash
cp -r packages/phoria-islands/dist /tmp/dts-before
pnpm --filter @phoria/phoria build
diff -ru /tmp/dts-before packages/phoria-islands/dist --include='*.d.ts'
```

Expected: no diff, or only cosmetic formatting differences. Any *semantic* change to an exported signature is a bug — investigate before proceeding.

- [ ] **Step 5: Full verification** — `build`, `lint`, `check`, `test`, `test:browser`, `dotnet test`, smoke.

- [ ] **Step 6: Changeset + commit**

```bash
pnpm changeset   # patch all 5: "Build with TypeScript 6 and vite-plugin-dts 5"
git commit -am "chore: upgrade to TypeScript 6 and vite-plugin-dts 5"
```

---

## Task 5: Vitest 4

Landed on Vite 6 deliberately — Vitest 4 declares `vite: ^6.0.0 || ^7.0.0 || ^8.0.0`, so upgrading it *before* Vite 8 means Task 7 changes exactly one variable.

**Files:** `pnpm-workspace.yaml`, 5 package `package.json`, all 7 `vitest*.config.ts`, root `package.json`.

- [ ] **Step 1: Swap the browser packages in the catalog**

`vitest: ^4.1.10`; **remove** `"@vitest/browser"`; **add** `"@vitest/browser-playwright": ^4.1.10`; keep `playwright: ^1.62.0`.

In `packages/phoria-islands/package.json` and `packages/phoria-react/package.json` `devDependencies`, replace `"@vitest/browser": "catalog:"` with `"@vitest/browser-playwright": "catalog:"`. Then `pnpm install`.

- [ ] **Step 2: Rewrite the two browser configs for the v4 provider factory**

In v4 `browser.provider` takes a factory object, not a string, and `@vitest/browser` no longer exists as a package. `packages/phoria-react/vitest.browser.config.ts`:

```ts
import { playwright } from "@vitest/browser-playwright"
import react from "@vitejs/plugin-react"
import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths(), react()],
	optimizeDeps: {
		include: ["react", "react-dom/client"]
	},
	test: {
		include: ["src/**/*.browser.test.tsx"],
		browser: {
			enabled: true,
			headless: true,
			provider: playwright(),
			instances: [{ browser: "chromium" }]
		}
	}
})
```

Apply the same `provider: playwright()` change to `packages/phoria-islands/vitest.browser.config.ts`.

- [ ] **Step 3: Migrate `@vitest/browser/context` imports**

```bash
rg -l '@vitest/browser/(context|utils)' packages
```

Replace `from "@vitest/browser/context"` with `from "vitest/browser"` in every match. (`@vitest/browser/context` still works during the transition period but is slated for removal.)

- [ ] **Step 4: Reinstate the `exclude` patterns the v4 defaults dropped**

v4 no longer excludes `dist` by default. All five unit configs use an explicit `include: ["src/**/*.test.ts"]`, which already scopes them — so no change is needed there. **Verify** rather than assume:

Run: `pnpm test`

Expected: the same test count as before the upgrade, with no tests discovered under any `dist/` directory. If extra files are collected, add `dir: "./src"` to that project's `test` block.

- [ ] **Step 5: Run both suites**

Run: `pnpm test` then `pnpm exec playwright install --with-deps chromium && pnpm test:browser`

Expected: PASS, same counts as before.

- [ ] **Step 6: Commit** (`chore: upgrade Vitest to 4 and migrate browser provider`). No changeset — dev-only dependency.

---

## Task 6: .NET 10 — SDK, `net8.0;net10.0`, xUnit v3 on MTP

Independent of the entire JS side; keep it sequential anyway so a red run has one candidate cause.

**Files:** `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `packages/Phoria/Phoria.csproj`, `packages/Phoria.Tests/Phoria.Tests.csproj`, both `e2e/**/WebApp.csproj`, `.github/workflows/*.yml` (CI is finished in Task 12).

- [ ] **Step 1: Move the SDK and opt into MTP mode of `dotnet test`**

`global.json`:

```json
{
  "sdk": {
    "version": "10.0.302",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

The `test` section enables the .NET 10 SDK's native MTP mode, which is what xUnit v3 wants. Without it you would need `TestingPlatformDotnetTestSupport` and an extra `--` before test-app arguments — a legacy path that MTP v2 removes.

- [ ] **Step 2: Retarget the library**

`packages/Phoria/Phoria.csproj`:

```xml
	<PropertyGroup>
		<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
		<AnalysisLevel>latest-recommended</AnalysisLevel>
		<EnablePackageValidation>true</EnablePackageValidation>
	</PropertyGroup>
```

`EnablePackageValidation` is cheap insurance: going from 2 to 2-but-different TFMs, it catches an accidentally divergent public surface between the `net8.0` and `net10.0` assets. (Leave `PackageValidationBaselineVersion` unset until 1.0.0 is published.)

- [ ] **Step 3: Pin `LangVersion` explicitly**

`Directory.Build.props` currently says `<LangVersion>latest</LangVersion>`, which is **not** per-TFM — on the .NET 10 SDK it silently becomes C# 15 for the `net8.0` compilation too, and a handful of language features need BCL support that net8 lacks. Change to:

```xml
		<LangVersion>13.0</LangVersion>
```

C# 13 is the highest version guaranteed to compile cleanly against `net8.0`. Revisit if/when net8 is dropped.

- [ ] **Step 4: Condition the ASP.NET Core package version per TFM**

`Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` is pinned at `9.0.0` and referenced by both e2e apps. A 9.0.0 ASP.NET Core package on a `net10.0` app produces framework-mismatch warnings. In `Directory.Packages.props`:

```xml
	<ItemGroup>
		<PackageVersion Include="CliWrap" Version="3.10.3" />
		<PackageVersion Include="Microsoft.IO.RecyclableMemoryStream" Version="3.0.1" />
		<PackageVersion Include="xunit.v3" Version="3.2.2" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
		<PackageVersion Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="8.0.29" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
		<PackageVersion Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="10.0.10" />
	</ItemGroup>
```

Note `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` are **removed** — xUnit v3 on MTP needs neither.

- [ ] **Step 5: Migrate the test project to xUnit v3 + MTP**

`packages/Phoria.Tests/Phoria.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net10.0</TargetFrameworks>
		<OutputType>Exe</OutputType>
		<IsPackable>false</IsPackable>
		<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="xunit.v3" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Phoria\Phoria.csproj" />
	</ItemGroup>

	<ItemGroup>
		<None Include="TestData\**\*" CopyToOutputDirectory="PreserveNewest" />
	</ItemGroup>

</Project>
```

`OutputType=Exe` and `UseMicrosoftTestingPlatformRunner` are both required for MTP. The redundant `Nullable`/`ImplicitUsings` properties are dropped — they are already in `Directory.Build.props`. `xunit.v3` 3.2.2 ships `net472` and `net8.0` assets, so both TFMs resolve.

- [ ] **Step 6: Fix the xUnit v3 source-level differences**

Run: `dotnet build Phoria.sln`

Expected: FAIL with `CS0246`-class errors on `Xunit` usings. In each test file, `using Xunit;` is still correct, but v3 moves a few assertion members. Fix each reported error individually; the existing tests use only `Assert.Equal`/`Assert.NotNull`/`Assert.True`/`Fact`, all of which are unchanged.

- [ ] **Step 7: Retarget the e2e apps to net10.0**

In both `e2e/framework-multiple/WebApp/WebApp.csproj` and `e2e/with-workspace/WebApp/WebApp.csproj`: `<TargetFramework>net9.0</TargetFramework>` → `net10.0`, and `<AnalysisLevel>9.0-recommended</AnalysisLevel>` → `latest-recommended`.

This matters: the e2e apps are the only end-to-end signal, and if they stay on net9 you get zero net10 integration coverage.

- [ ] **Step 8: Triage the new analyzer output — decide, don't suppress blindly**

`latest-recommended` will surface new CA diagnostics. `TreatWarningsAsErrors` is not set, so none break the build, but they will pollute CI output. Fix the cheap and genuinely-correct ones:

- `ServiceCollectionExtensions.cs:20` — `services.BuildServiceProvider()` is **ASP0000** and is also a real bug (it builds a throwaway container, duplicating any singleton resolved during `AddPhoria`). Replace the config read with `services.AddOptions<PhoriaOptions>().BindConfiguration(PhoriaOptions.SectionName).Configure(configure)`.
- `PhoriaServerMiddleware.cs:94-96` — `ContainsKey` + indexer → `TryGetValue` (CA1854).
- `PhoriaServerMonitorService` → add `sealed` (CA1852).
- `PhoriaIslandEntryTagHelper.cs:41-42` — `new Regex(..., RegexOptions.Compiled)` → `[GeneratedRegex]` (SYSLIB1045).
- `PhoriaIslandEntryTagHelper.cs:254-256` — `Task.Factory.StartNew<TagHelperContent>` → `Task.FromResult`.
- `StreamPool.cs:26` — `GenerateCallStacks = true` ships in production and makes RMS capture a stack trace on every island render. Gate it: `GenerateCallStacks = Debugger.IsAttached`.
- `PhoriaIslandComponentException` — add the two standard missing constructors (CA1032).

**Explicitly out of scope for this task:** the `Process.Kill()` process-tree bug, the `StartServer`/`StopServer` semaphore race, the undisposed `StreamPool`s, unconditional `DangerousAcceptAnyServerCertificateValidator`, and any `IMemoryPoolFactory<byte>` adoption. Those are **Phase 2 (server robustness)** and must not be smuggled into a dependency-upgrade task. Record each one as a GitHub issue instead.

- [ ] **Step 9: Verify both TFM legs**

Run: `dotnet test Phoria.sln --configuration Release`

Expected: PASS for `net8.0` and `net10.0`. Confirm the summary reports per-assembly counts for both.

Then confirm the e2e app still builds and the smoke test passes:

Run: `pnpm --filter framework-multiple build && pnpm --filter framework-multiple test:smoke`

- [ ] **Step 10: Changeset + commit**

```bash
pnpm changeset   # MINOR for Phoria: "Target net8.0 and net10.0. net9.0 support is dropped (EOL 2026-11-10)."
git commit -am "feat(dotnet)!: target net8.0 and net10.0, migrate tests to xUnit v3 on MTP"
```

---

## Task 7: Vite 8 — the bundler change

Only now, with everything else green and the ssr-manifest seam under test, change the bundler.

**Files:** `pnpm-workspace.yaml`, all 5 package `package.json` (`peerDependencies`), all 5 library `vite.config.ts`, all 7 `vitest*.config.ts`, both e2e `vite.config.ts`.

- [ ] **Step 1: Set the target catalog**

```yaml
catalog:
  "@meeg/vite-plugin-inspect-config": ~0.2.0
  "@rollup/pluginutils": ^5.4.0
  "@sveltejs/vite-plugin-svelte": ^7.2.0
  "@types/node": ^24.13.3
  "@types/react": ^19.2.17
  "@types/react-dom": ^19.2.3
  "@vitejs/plugin-react": ^6.0.4
  "@vitejs/plugin-vue": ^6.0.8
  "@vitest/browser-playwright": ^4.1.10
  cross-env: ^10.1.0
  destr: ^2.0.5
  empathic: ^2.0.1
  h3: ^1.15.11
  listhen: ^1.10.1
  magic-string: ^1.1.0
  npm-run-all: ^4.1.5
  playwright: ^1.62.0
  postcss-preset-env: ^11.3.2
  react: ^19.2.8
  react-dom: ^19.2.8
  svelte: ^5.56.8
  tsx: ^4.23.1
  typescript: ~6.0.3
  vite: ^8.1.5
  vite-plugin-dts: ^5.0.3
  vite-plugin-externalize-deps: ^0.10.0
  vitest: ^4.1.10
  vue: ^3.5.40
```

`vite-tsconfig-paths` is **removed** — see step 4.

- [ ] **Step 2: Unblock the two plugins whose peer ranges lag Vite 8**

`vite-plugin-externalize-deps@0.10.0` declares `vite` up to `^7.0.0`, and `@meeg/vite-plugin-inspect-config@0.2.0` up to `^6.0.0`. Both work on Vite 8 (they touch only `config`/`configResolved`), so allow them rather than removing them — `inspectConfig()` in particular produces the `.vite-config/vite.config.json` snapshots you need as the before/after diff baseline for this very task.

Append to `pnpm-workspace.yaml`:

```yaml
peerDependencyRules:
  allowedVersions:
    "vite-plugin-externalize-deps>vite": "8"
    "@meeg/vite-plugin-inspect-config>vite": "8"
```

Then open a follow-up issue on your own `@meeg/vite-plugin-inspect-config` to publish `0.3.0` with `vite: ^8.0.0`, and remove that entry once released.

- [ ] **Step 3: Widen every `peerDependencies.vite`**

All 5 packages declare `"vite": "^6.0.0"`. Change to `"^8.0.0"`. Also in `phoria-react`: `"@vitejs/plugin-react": "^6.0.0"`; `phoria-vue`: `"@vitejs/plugin-vue": "^6.0.0"`; `phoria-svelte`: `"@sveltejs/vite-plugin-svelte": "^7.0.0"` and `"svelte": "^5.46.4"` (the floor vite-plugin-svelte 7 requires).

Update the matching `devDependencies` pins in `phoria-react` (`@vitejs/plugin-react: "catalog:"`), `phoria-svelte`, and `phoria-vue` to use `catalog:` rather than the hard-coded `^5.x`/`^4.x` values they carry today.

- [ ] **Step 4: Replace `vite-tsconfig-paths` with Vite's built-in resolver**

Vite 8 ships `resolve.tsconfigPaths`, which is no longer experimental. Remove the plugin from all 5 library `vite.config.ts` and all 7 `vitest*.config.ts`, and from both e2e `vite.config.ts`.

`packages/phoria-islands/vite.config.ts` becomes:

```ts
import { defineConfig } from "vite"
import dts from "vite-plugin-dts"
import { externalizeDeps } from "vite-plugin-externalize-deps"

// https://vite.dev/config/
export default defineConfig({
	plugins: [externalizeDeps(), dts()],
	resolve: {
		tsconfigPaths: true
	},
	build: {
		lib: {
			entry: {
				client: "src/client/main.ts",
				main: "src/main.ts",
				server: "src/server/main.ts",
				vite: "src/vite/plugin.ts"
			},
			name: "phoria"
		}
	}
})
```

`packages/phoria-islands/vitest.config.ts` becomes:

```ts
import { defineConfig } from "vitest/config"

export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	test: {
		environment: "node",
		include: ["src/**/*.test.ts"],
		exclude: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"]
	}
})
```

Apply the same shape to the other four library configs and the remaining vitest configs.

Both e2e `vite.config.ts` files pass `tsconfigPaths({ root: "../../" })` / `({ root: "../" })` because the tsconfig lives above the Vite root. `resolve.tsconfigPaths` resolves the nearest `tsconfig.json` per-file rather than taking a root, so the option is simply `true` — verify the `~/*` alias still resolves in step 7 before accepting this.

- [ ] **Step 5: Install and expect peer noise to be gone**

Run: `pnpm install`

Expected: no unmet-peer warnings for `vite`.

- [ ] **Step 6: Snapshot the resolved config diff**

```bash
cp e2e/framework-multiple/.vite-config/vite.config.json /tmp/vite6-resolved.json
pnpm --filter framework-multiple build:islands
diff -u /tmp/vite6-resolved.json e2e/framework-multiple/.vite-config/vite.config.json | head -200
```

Read the diff. Expected and acceptable: `rollupOptions` → `rolldownOptions`, `esbuild` → `oxc`, `cssMinify` now Lightning CSS, `build.target` moving from `chrome87/edge88/firefox78/safari14` to `chrome111/edge111/firefox114/safari16.4`. **Unexpected and blocking:** any change to `build.outDir`, `manifest`, `ssrManifest`, or the `environments.ssr.resolve.external` list.

- [ ] **Step 7: Run the full suite and work the failures**

Run: `pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && pnpm test:browser && dotnet test Phoria.sln`

Then, critically:

```bash
pnpm --filter framework-multiple build
pnpm --filter framework-multiple preview &
# wait for http://localhost:5247
pnpm --filter framework-multiple test:smoke
```

Known hazards in priority order:

1. **The preload assertion from Task 1 fails.** This is the expected high-risk outcome. Rolldown changes module IDs and drops `?commonjs-*` suffixes, so the `__phoriaComponentPath` → ssr-manifest key mapping in `PhoriaIslandPreloadTagHelper.cs` may no longer match. Diagnose by dumping the new `ssr-manifest.json` and comparing keys against `packages/Phoria.Tests/TestData/ssr-manifest.json`. Fix in `PhoriaIslandPreloadTagHelper.cs`, then **add a second golden file** (`TestData/ssr-manifest.rolldown.json`) and parameterise `ViteSsrManifestTests` over both, so the helper is proven against Rollup *and* Rolldown key shapes.
2. **`ViteChunk` silently drops `name`.** Vite emits `"name"` in manifest chunks and the .NET POCO has no such property. Harmless today, but add `public string? Name { get; init; }` while you are here — Phase 3's asset-bundling feature will want name-based lookup.
3. **CJS default-import interop changed.** Vite 8 handles `default` imports from CJS consistently. If a dependency breaks, the escape hatch is `legacy: { inconsistentCjsInterop: true }` — use it only as a temporary unblock and file an upstream issue.
4. **The React HMR preamble.** `PhoriaIslandEntryTagHelper.cs:145-161` hard-codes `@vitejs/plugin-react`'s preamble (`window.__vite_plugin_react_preamble_installed__`, `/@react-refresh`). Verify against plugin-react 6's actual `preambleCode` and update the C# string if it changed. There is no automated test for this — check it manually with `pnpm --filter framework-multiple dev` and confirm HMR works in the browser.
5. **`server.https` with file paths.** `@phoria/vite-plugin-dotnet-dev-certs` assigns `{ cert: <path>, key: <path> }` (paths, not PEM contents). Confirm Vite 8 still reads them, via `pnpm --filter framework-multiple dev` over https.

- [ ] **Step 8: Changeset + commit**

```bash
pnpm changeset   # MINOR for all 5: "Require Vite 8. Peer dependency widened to ^8.0.0;
                 #  vite-tsconfig-paths replaced by Vite's built-in resolve.tsconfigPaths."
git commit -am "feat!: upgrade to Vite 8 (Rolldown/Oxc)"
```

---

## Task 8: Vite plugin correctness fixes

Small, self-contained fixes in the file Task 7 just touched.

**Files:** `packages/phoria-islands/src/vite/plugin.ts`, `packages/phoria-islands/src/server/routing.ts`, `packages/phoria-islands/src/vite/plugin.test.ts` (new tests).

- [ ] **Step 1: Write the failing test for the `rollupOptions` clobber**

`setEntry` does `options.rollupOptions = { input }`, destroying any user-supplied `rollupOptions`. Add to `packages/phoria-islands/src/vite/plugin.test.ts`:

```ts
import type { EnvironmentOptions } from "vite"
import { describe, expect, it } from "vitest"
import { phoria } from "./plugin"

describe("phoria plugin", () => {
	it("preserves user-supplied rollupOptions when setting the entry input", async () => {
		const plugin = getPhoriaPlugin(phoria({ cwd: "test/fixtures" }))
		const options: EnvironmentOptions = {
			build: {
				rollupOptions: {
					output: { entryFileNames: "custom-[name].js" }
				}
			}
		}

		await plugin.config?.({}, { command: "build", mode: "production" })
		plugin.configEnvironment?.("client", options, { command: "build", mode: "production" })

		expect(options.build?.rollupOptions?.output).toEqual({ entryFileNames: "custom-[name].js" })
		expect(options.build?.rollupOptions?.input).toBeDefined()
	})
})
```

Run: `pnpm --filter @phoria/phoria test`

Expected: FAIL — `output` is `undefined` because it was overwritten.

- [ ] **Step 2: Fix `setEntry`**

```ts
function setEntry(options: BuildEnvironmentOptions, root?: string, entryFile?: string) {
	if (typeof entryFile === "undefined") {
		return
	}

	const input = root ? `${root}/${entryFile}` : entryFile

	options.rollupOptions = {
		...options.rollupOptions,
		input
	}
}
```

Run: `pnpm --filter @phoria/phoria test` → PASS.

- [ ] **Step 3: Stop duplicating `public/` into the SSR output**

The resolved SSR env has `copyPublicDir: true`, so `react.svg`/`svelte.svg`/`vue.svg` are written into `dist/phoria/ssr/` as dead weight. In `setSsrEnvironment`, after `options.build.emptyOutDir ??= true`, add:

```ts
	options.build.copyPublicDir ??= false
```

Verify: `pnpm --filter framework-multiple build:islands && ls e2e/framework-multiple/WebApp/ui/dist/phoria/ssr/` — no `.svg` files.

- [ ] **Step 4: Use Vite's own environment type guard**

`routing.ts:137-141` hand-rolls `isRunnableDevEnvironment` via `"runner" in environment`. Vite exports the real guard; using it means Vite maintains the type contract instead of you.

```ts
import { isRunnableDevEnvironment, type ViteDevServer } from "vite"
```

and delete the local function. The `DevEnvironment` / `RunnableDevEnvironment` type imports become unnecessary — remove them.

- [ ] **Step 5: Full verification + commit**

```bash
pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && dotnet test Phoria.sln
pnpm changeset   # patch @phoria/phoria: "Preserve user rollupOptions; stop copying publicDir into the SSR output."
git commit -am "fix(vite): preserve user rollupOptions and skip publicDir in the SSR build"
```

---

## Task 9: Collapse the three build passes with `builder.buildApp`

Today `run-p build:islands build:webapp build:server` runs two independent Vite processes plus MSBuild **in parallel with no ordering guarantee** — and the client environment is what emits `ssr-manifest.json`, so the current ordering works by luck.

**Files:** `packages/phoria-islands/src/vite/plugin.ts`, `packages/phoria-{react,svelte,vue}/src/vite/plugin.ts`, both e2e `package.json`, `e2e/framework-multiple/vite.server.config.ts` (**delete**), `e2e/with-workspace/WebApp/vite.server.config.ts` (**delete**), `docs/guides/getting-started.md`.

**Interfaces:**

- Consumes: `PhoriaAppSettings.root`, `.ssrEntry`, `.build.outDir` from `src/server/appsettings.ts`; the fixed output layout `${outDir}/server/server.js` that `PhoriaServerProcess` spawns via `appsettings.Production.json`.
- Produces: a third environment named `server`, plus `PhoriaPluginOptions.serverEntry?: string | false` (default `"src/server.ts"`).

- [ ] **Step 1: Add a `serverEntry` option and a `server` environment**

In `packages/phoria-islands/src/vite/plugin.ts`:

```ts
const environment = {
	client: "client",
	server: "server",
	ssr: "ssr"
} as const

interface PhoriaPluginOptions {
	cwd: string
	appsettings: Partial<PhoriaAppSettings>
	/** Entry for the Phoria Server bundle. Set to `false` to skip building it. */
	serverEntry: string | false
}
```

with `serverEntry: "src/server.ts"` in the defaults.

- [ ] **Step 2: Configure the `server` environment to reproduce `vite.server.config.ts` exactly**

```ts
function setServerEnvironment(
	options: EnvironmentOptions,
	appsettings: Partial<PhoriaAppSettings>,
	serverEntry: string
) {
	const external = ["@phoria/phoria"]

	options.resolve ??= {}

	if (typeof options.resolve.external === "undefined") {
		options.resolve.external = external
	} else if (Array.isArray(options.resolve.external)) {
		options.resolve.external.push(...external)
	}

	options.build ??= {}
	options.build.ssr = true
	options.build.target ??= "es2022"
	options.build.copyPublicDir ??= false
	options.build.emptyOutDir ??= true
	options.build.outDir = `${appsettings.build?.outDir ?? defaultOutDir}/server`

	setEntry(options.build, appsettings.root, serverEntry)
}
```

`outDir` is deliberately `${outDir}/server` — **not** `${outDir}/phoria/server`. That keeps `WebApp/ui/dist/server/server.js` exactly where both `appsettings.Production.json` and the `preview:server` script already expect it, so nothing downstream changes.

Wire it into `config` (`config.environments[environment.server] ??= {}`, only when `serverEntry !== false`) and into the `configEnvironment` switch.

- [ ] **Step 3: Add the `buildApp` hook for deterministic ordering**

The client environment emits both `manifest.json` and `ssr-manifest.json`, so it must complete first. Default `vite build --app` builds in `environments` record order, but make it explicit:

```ts
		buildApp: {
			order: "pre",
			async handler(builder) {
				const order = [environment.client, environment.ssr, environment.server]

				for (const name of order) {
					const env = builder.environments[name]

					if (env && !env.isBuilt) {
						await builder.build(env)
					}
				}
			}
		}
```

`environment.isBuilt` guards against double-building if another plugin also participates.

- [ ] **Step 4: Scope the framework `transform` hooks to client + ssr only**

Critically important now that a `server` environment exists: `phoria-react`/`-svelte`/`-vue` each append `__phoriaComponentPath` in `transform`, and today those hooks run in **every** environment. The server bundle must not be rewritten. In each of the three plugins add:

```ts
		applyToEnvironment(environment) {
			return environment.name === "client" || environment.name === "ssr"
		},
```

- [ ] **Step 5: Delete the standalone server configs and simplify the scripts**

```bash
git rm e2e/framework-multiple/vite.server.config.ts
git rm e2e/with-workspace/WebApp/vite.server.config.ts
```

In both e2e `package.json`, remove the `build:server` script:

```json
		"build": "run-p build:* -c",
		"build:islands": "vite build --app",
		"build:webapp": "dotnet build --configuration Release"
```

- [ ] **Step 6: Verify the output layout is byte-for-byte compatible**

```bash
pnpm --filter framework-multiple build
find e2e/framework-multiple/WebApp/ui/dist -maxdepth 3 -type d | sort
test -f e2e/framework-multiple/WebApp/ui/dist/server/server.js && echo "server entry OK"
test -f e2e/framework-multiple/WebApp/ui/dist/phoria/client/.vite/ssr-manifest.json && echo "ssr-manifest OK"
```

Expected: `dist/phoria/client`, `dist/phoria/ssr`, `dist/server` — the same three directories as before, and both marker files present.

- [ ] **Step 7: Confirm the server bundle was not rewritten**

```bash
grep -c "__phoriaComponentPath" e2e/framework-multiple/WebApp/ui/dist/server/server.js || echo "clean (expected)"
```

Expected: no matches — proving step 4's `applyToEnvironment` scoping works.

- [ ] **Step 8: End-to-end verification**

Run: `pnpm --filter framework-multiple preview` in one shell, then `pnpm --filter framework-multiple test:smoke`. Also verify `pnpm --filter with-workspace build` succeeds.

- [ ] **Step 9: Update the getting-started guide**

`docs/guides/getting-started.md` documents creating `vite.server.config.ts` and a `build:server` script. Rewrite that section to describe the single `vite build --app` pass and the `serverEntry` plugin option.

- [ ] **Step 10: Changeset + commit**

```bash
pnpm changeset   # MINOR for @phoria/phoria + the three framework packages:
                 # "The Phoria Server bundle is now built by the `phoria` plugin as a `server` environment.
                 #  A separate vite.server.config.ts is no longer required. Framework plugins now scope
                 #  their transform to the client and ssr environments."
git commit -am "feat(vite): build the Phoria Server via builder.buildApp instead of a separate config"
```

---

## Task 10: Decouple h3 from the public API (and bump to 1.15.x)

h3 currently leaks from `@phoria/phoria/server` in two places, so any future v2 migration is automatically a consumer-breaking change. Fix that *now*, pre-1.0, while staying on the stable v1 line.

**Files:** `packages/phoria-islands/src/server/routing.ts`, `packages/phoria-islands/src/server/phoria-island.ts`, `packages/phoria-islands/src/server/main.ts`, `packages/phoria-islands/src/server/phoria-island.test.ts` (new).

**Interfaces:**

- Produces: exported `type PhoriaRequestHandler` and `interface PhoriaIslandRequest`, replacing the leaked `EventHandler<EventHandlerRequest, any>` and `H3Event<EventHandlerRequest>` in the published `.d.ts`.

- [ ] **Step 1: Bump h3 and listhen**

Catalog: `h3: ^1.15.11`, `listhen: ^1.10.1`. `pnpm install`. Run `pnpm test && pnpm --filter framework-multiple test:smoke` — expect green; 1.13 → 1.15 is additive.

- [ ] **Step 2: Introduce a Phoria-owned handler type**

In `routing.ts`, add and export:

```ts
import type { EventHandler, EventHandlerRequest } from "h3"

/**
 * A request handler that can be mounted on a Phoria Server app.
 *
 * The underlying HTTP library is an implementation detail of Phoria and may
 * change in a future minor release. Treat values of this type as opaque.
 */
type PhoriaRequestHandler = EventHandler<EventHandlerRequest, unknown>
```

Annotate all four factories with an explicit `: PhoriaRequestHandler` return type:

```ts
function createPhoriaSsrRequestHandler(
	appsettings: PhoriaAppSettings,
	options?: Partial<PhoriaSsrRequestHandlerOptions>
): PhoriaRequestHandler {
```

…and the same for `createPhoriaDevSsrRequestHandler`, `createPhoriaCsrRequestHandler`, `createPhoriaDevCsrRequestHandler`. Export the type from `routing.ts` and re-export it from `src/server/main.ts`.

- [ ] **Step 3: Write the failing test for a decoupled `PhoriaIsland.create`**

`PhoriaIsland.create` currently takes `H3Event<EventHandlerRequest>`, which is both an API leak and the reason it has never been unit-tested. Create `packages/phoria-islands/src/server/phoria-island.test.ts`:

```ts
import { describe, expect, it } from "vitest"
import { PhoriaIsland } from "./phoria-island"

describe("PhoriaIsland.create", () => {
	it("throws when no component param is present", async () => {
		await expect(PhoriaIsland.create({ params: {}, readProps: async () => undefined })).rejects.toThrow(
			`No "component" was provided in the request path.`
		)
	})

	it("throws when the body is an array rather than an object", async () => {
		await expect(
			PhoriaIsland.create({ params: { component: "Counter" }, readProps: async () => [1, 2] })
		).rejects.toThrow("Props sent in body must be a JSON object.")
	})

	it("treats an absent body as null props", async () => {
		// registerFramework/registerComponents setup for a stub "Counter" goes here,
		// mirroring the existing register.test.ts fixtures.
		const island = await PhoriaIsland.create({
			params: { component: "Counter" },
			readProps: async () => undefined
		})

		expect(island.props).toBeNull()
	})
})
```

Run: `pnpm --filter @phoria/phoria test` → FAIL (`create` does not accept that shape).

- [ ] **Step 4: Change the signature**

In `phoria-island.ts`, replace the h3 imports with a narrow input contract:

```ts
/** The request data `PhoriaIsland.create` needs, independent of any HTTP library. */
interface PhoriaIslandRequest {
	params: Record<string, string | undefined>
	readProps: () => Promise<unknown>
}
```

and change the static method to `static async create(request: PhoriaIslandRequest)`, reading `request.params.component` and `await request.readProps()` in place of `getRouterParams(event)` and `readBody(event)`. The `typeof body !== "undefined"` guard is **load-bearing** — the .NET client sends no body at all when props are null (`PhoriaIslandSsr.cs:36`) — so keep it verbatim.

Export `PhoriaIslandRequest` from `main.ts`.

- [ ] **Step 5: Adapt at the h3 boundary**

In `routing.ts`'s render handler:

```ts
			const phoriaIsland = await PhoriaIsland.create({
				params: getRouterParams(event),
				readProps: () => readBody(event)
			})
```

- [ ] **Step 6: Verify the `.d.ts` no longer mentions h3 in those places**

```bash
pnpm --filter @phoria/phoria build
grep -n "h3" packages/phoria-islands/dist/server/phoria-island.d.ts && echo "STILL LEAKING" || echo "clean"
grep -n "EventHandler" packages/phoria-islands/dist/server/routing.d.ts
```

Expected: `phoria-island.d.ts` has no `h3` import. `routing.d.ts` now references the `PhoriaRequestHandler` alias — h3 remains as the alias's *definition*, which is acceptable and intentional; what matters is that consumers name the Phoria type, not the h3 one.

- [ ] **Step 7: Full verification + changeset + commit**

```bash
pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && pnpm test:browser
pnpm --filter framework-multiple build && pnpm --filter framework-multiple test:smoke
pnpm changeset   # MINOR for @phoria/phoria (breaking, pre-1.0):
                 # "PhoriaIsland.create now takes a PhoriaIslandRequest instead of an h3 H3Event.
                 #  Request handler factories return the new PhoriaRequestHandler type."
git commit -am "refactor(server)!: decouple the public API from h3 types"
```

---

## Task 11: Remaining dependency sweep

One task, one commit, for the bumps that carry no architectural decision. Each has a stated risk so a reviewer can judge them individually.

**Files:** `pnpm-workspace.yaml`, all 5 package `package.json`, `Directory.Packages.props`, both e2e `package.json`.

- [ ] **Step 1: Framework and type bumps (in-major, low risk)**

`react`/`react-dom` → `^19.2.8`; `@types/react` → `^19.2.17`; `@types/react-dom` → `^19.2.3`; `svelte` → `^5.56.8`; `vue` → `^3.5.40`. All within the existing major, so `peerDependencies` need no change beyond `svelte: "^5.46.4"` set in Task 7.

- [ ] **Step 2: Major bumps in direct dependencies (read each changelog first)**

| Package | From | To | Risk |
|---|---|---|---|
| `magic-string` | ^0.30.17 | ^1.1.0 | Used by all 3 framework plugins' `transform`. 1.0.0 was a `Map`-based perf rewrite plus a zero-length-range crash fix — no documented API break. Vite 8 itself is on v1. |
| `empathic` | ^1.0.0 | ^2.0.1 | Used by `appsettings.ts` and `dotnet-dev-certs`. Subpath-only exports (`empathic/find`, `/package`, `/resolve`). Verify the existing import specifiers still resolve. |
| `tinyexec` | ^0.3.2 | ^1.2.4 | `dotnet-dev-certs` only, one call site. |
| `std-env` | ^3.8.0 | ^4.2.0 | `dotnet-dev-certs` devDependency. |
| `cross-env` | ^7.0.3 | ^10.1.0 | Build scripts only; now requires Node >= 20 (satisfied). |
| `postcss-preset-env` | ^10.0.8 | ^11.3.2 | e2e apps only. Stage-0 features may change output; check the built CSS renders correctly. |
| `mime` | ^4.0.6 | ^4.1.0 | In-major. |
| `defu` | ^6.1.4 | ^6.1.7 | In-major. |
| `destr` | ^2.0.3 | ^2.0.5 | In-major. |
| `@rollup/pluginutils` | ^5.1.4 | ^5.4.0 | In-major. `createFilter`/`normalizePath` are rolldown-compatible. |
| `tsx` | ^4.19.2 | ^4.23.1 | In-major; dev script only. |
| `CliWrap` (NuGet) | 3.7.0 | 3.10.3 | Spawn/stream only. |

- [ ] **Step 3: Verify after each of the three riskiest**

After `magic-string`: `pnpm --filter @phoria/phoria-react test && pnpm --filter framework-multiple build` — then confirm `__phoriaComponentPath` is still appended: `grep -c "__phoriaComponentPath" e2e/framework-multiple/WebApp/ui/dist/phoria/ssr/entry-server.js`.

After `empathic`: `pnpm --filter @phoria/phoria test` (covers `appsettings.ts`) and `pnpm --filter @phoria/vite-plugin-dotnet-dev-certs test`.

After `postcss-preset-env`: `pnpm --filter framework-multiple build && pnpm --filter framework-multiple test:smoke`, and eyeball the page in a browser — CSS regressions are invisible to the smoke test.

- [ ] **Step 4: Full verification + changeset + commit**

```bash
pnpm lerna run build && pnpm lerna run lint && pnpm lerna run check && pnpm test && pnpm test:browser && dotnet test Phoria.sln
pnpm changeset   # patch all 5: "Update dependencies."
git commit -am "chore: update remaining dependencies"
```

---

## Task 12: CI, docs, and phase close-out

**Files:** `.github/workflows/ci.yml`, `.github/workflows/release.yml`, `AGENTS.md`, `docs/PROJECT.md`, `docs/MEMORY.md`.

- [ ] **Step 1: Bump the actions**

`actions/checkout@v4` → `@v7`; `actions/setup-node@v4` → `@v7`; `pnpm/action-setup@v3` → `@v6`; `actions/setup-dotnet@v4` → `@v6`; `changesets/action@v1` → pin to `@v1.9.0`. Apply in **both** workflows.

- [ ] **Step 2: Install both .NET runtimes**

The 10.0 SDK ships only the 10.0 runtime, but `Phoria.Tests` targets `net8.0` too, so the net8 leg needs an 8.0 runtime present. In every `setup-dotnet` step:

```yaml
      - name: Setup dotnet
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: |
            8.0.x
          global-json-file: "./global.json"
```

`global-json-file` supplies the 10.0.302 SDK; `dotnet-version: 8.0.x` adds the down-level runtime alongside it.

- [ ] **Step 3: Rename the misleading CI job and fix the `dotnet test` invocation**

The first job is named `lint` but runs build + lint + check + unit tests + dotnet tests. Rename it to `build-and-test`. And since `global.json` now selects MTP mode, the solution must be passed with `--solution`:

```yaml
      - name: Test dotnet
        run: dotnet test --solution Phoria.sln --configuration Release
```

- [ ] **Step 4: Verify CI locally before pushing**

Run the exact CI sequence in a clean checkout:

```bash
git clean -xdf -e node_modules
pnpm install --frozen-lockfile
pnpm lerna run build
pnpm lerna run lint
pnpm lerna run check
pnpm lerna run test
dotnet test --solution Phoria.sln --configuration Release
pnpm exec playwright install --with-deps chromium
pnpm lerna run test:browser
pnpm --filter framework-multiple build
pnpm --filter framework-multiple preview &
pnpm --filter framework-multiple test:smoke
```

- [ ] **Step 5: Update `AGENTS.md`**

- Prerequisites: Node v24.18.0, pnpm 11.17.0, .NET SDK 10.0.302.
- Test command: `dotnet test --solution Phoria.sln` and note the MTP runner.
- Gotchas: remove the `package.json`/Biome exclusion (if Task 3 step 4 passed); remove the "`.NET solution targets net8.0;net9.0`" line and state `net8.0;net10.0`; add "Vite 8 uses Rolldown/Oxc — `rollupOptions` is deprecated in favour of `rolldownOptions`"; add "`resolve.tsconfigPaths: true` replaces `vite-tsconfig-paths`"; add "all pnpm settings live in `pnpm-workspace.yaml`, not `package.json`/`.npmrc`".
- Peer dependencies: note that framework packages peer-depend on `@phoria/phoria` at `~0.4.0` and this must be reconciled at 1.0.0 (Phase 5).

- [ ] **Step 6: Update `docs/PROJECT.md`**

Correct the now-stale statements: "Vite 7" → Vite 8; "keep `net8.0;net9.0;net10.0`" → `net8.0;net10.0` with net9 dropped; resolve the open question about net8/net9 retirement (net9 retired now, net8 retained until its Nov 2026 EOL); note Node 24 and pnpm 11 in Constraints; move ".NET 10 memory pools" from Phase 1 to Phase 2 (see Self-review below).

- [ ] **Step 7: Extend the Phase 1 entry in `docs/MEMORY.md`**

A planning-time entry already exists (written when this plan was authored). **Append execution findings to it** rather than duplicating it — in particular: whether the Biome `package.json` exclusion could actually be removed, and whether Rolldown broke the ssr-manifest key contract and how it was fixed.

- [ ] **Step 8: File the deferred items as issues**

Open one GitHub issue per Phase 2 item listed in Task 6 step 8, plus one for the `@meeg/vite-plugin-inspect-config` release, and add them all to the v1 milestone. Do not leave them only in `MEMORY.md`.

- [ ] **Step 9: Commit**

```bash
git add .github AGENTS.md docs
git commit -m "chore: update CI and docs for the Phase 1 toolchain"
```

---

## Self-review notes

**Coverage against `PROJECT.md` Phase 1** ("Vite 7, React/Svelte/Vue latest, .NET 10 + memory pools"): Vite → Task 7; frameworks → Task 11; .NET 10 → Task 6. **`IMemoryPoolFactory` / memory pools is deliberately NOT in this plan.** `StreamPool` exposes `RecyclableMemoryStream` in the public API and its consumers depend on `GetReadOnlySequence()`, the `IBufferWriter<byte>` cast, and `Stream` semantics for `StreamContent` — none of which `MemoryPool<byte>` provides. It is a public-API refactor of `Phoria.IO`, not a dependency bump, and it belongs in Phase 2 alongside fixing the undisposed pools. `PROJECT.md` is amended accordingly in Task 12 step 6.

**Known risks:** Task 7 step 7 hazard 1 (Rolldown breaking the ssr-manifest key contract) is the single most likely place this plan needs unplanned work. That is precisely why Task 1 exists — the failure will be a red test, not a silent production regression.

**Scope honesty:** twelve tasks is large for one phase. Tasks 8, 9, and the Biome `package.json` re-evaluation in Task 3 are the most deferrable if the 1.0.0 date comes under pressure; Tasks 1, 2, 4, 6, and 7 are not.

**Version reference (captured 2026-07-27):** vite 8.1.5 · typescript 6.0.3 (7.0.2 exists but has no compiler API) · vitest 4.1.10 · @biomejs/biome 2.5.5 · lerna 9.0.7 · pnpm 11.17.0 · @changesets/cli 2.31.1 · node 24.18.0 (LTS) · .NET SDK 10.0.302 · xunit.v3 3.2.2 · h3 1.15.11 (v2 is 2.0.1-rc.26) · react 19.2.8 · svelte 5.56.8 · vue 3.5.40 · @vitejs/plugin-react 6.0.4 · @vitejs/plugin-vue 6.0.8 · @sveltejs/vite-plugin-svelte 7.2.0 · vite-plugin-dts 5.0.3 · magic-string 1.1.0. Re-verify with `npm view <pkg> version` before starting; anything more than a few weeks stale should be re-checked.
