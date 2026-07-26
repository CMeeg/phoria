# Phase 0: Test Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Establish an automated test foundation for Phoria — Vitest unit tests for JS packages, Vitest browser-mode component tests, xUnit unit tests for the .NET package, and a minimal full-stack smoke test — all wired into `pnpm`/Lerna/Nx and CI.

**Architecture:** Each JS package gets its own co-located Vitest setup following the existing per-package `build`/`lint`/`check` pattern, aggregated by `lerna run test` + an Nx `test` target. Client-side island behavior is tested in a real browser via Vitest browser mode (Playwright provider). The .NET package gets a sibling `Phoria.Tests` xUnit project added to `Phoria.sln`. A minimal full-stack smoke test builds and previews the `framework-multiple` e2e app and asserts the SSR health check plus one rendered page. CI runs unit tests inline and the smoke test as a separate job.

**Tech Stack:** Vitest (unit + browser mode), `@vitest/browser` with Playwright provider, xUnit with built-in `Assert` (.NET), pnpm workspaces, Lerna, Nx, GitHub Actions.

> **Note on assertion library:** This plan uses xUnit's built-in `Assert` rather than FluentAssertions. FluentAssertions v8+ moved to a commercial (paid) license and v7 is the last free version; for an MIT-licensed open-source project we avoid the dependency entirely.

## Global Constraints

- Node.js v22.11.0 (`.nvmrc`); pnpm 9.15.0 (`packageManager`).
- .NET SDK 9.0.100 (rolls forward); `.NET` test project targets `net8.0;net9.0` to match `Phoria.csproj`.
- Shared JS dependency versions go through the pnpm **catalog** in `pnpm-workspace.yaml` (add new test deps there, reference as `catalog:`).
- Biome config lives at repo root (`biome.jsonc`): tabs, semicolons as-needed, no trailing commas, line width 120, lf. New `.ts`/`.tsx` files must pass `biome check`.
- **`package.json` files are excluded from Biome formatting** — do not re-enable; hand-format edits to match existing style (tabs).
- Central NuGet package management via `Directory.Packages.props` — add test package versions there, reference without version in the `.csproj`.
- CI order is `build` → `lint` → `check`; tests are added after `check`.
- No enforced coverage threshold in this phase.
- **JS test naming:** co-located with source. Unit tests `*.test.ts`; browser-mode component tests `*.browser.test.ts` (or `*.browser.test.tsx`). `describe` names the unit under test; `it("does X when Y")`.
- **.NET test naming:** project `Phoria.Tests` mirrors source namespace folders; one class per unit named `<ClassUnderTest>Tests`; methods `MethodName_Scenario_ExpectedBehaviour` (per Microsoft unit-testing best practices).

---

## File Structure

**New files (JS unit test harness):**
- `packages/phoria-islands/vitest.config.ts` — node-environment unit project.
- `packages/phoria-islands/src/**/*.test.ts` — co-located unit tests.
- Equivalent `vitest.config.ts` for `phoria-react`, `phoria-svelte`, `phoria-vue`, `vite-plugin-dotnet-dev-certs`.

**New files (JS browser-mode tests):**
- `packages/phoria-react/vitest.browser.config.ts` and `src/**/*.browser.test.tsx` (React island mount/hydrate).
- Shared browser-mode config pattern reused for `phoria-svelte`/`phoria-vue` only if a "go" is reached (kept minimal; React is the reference implementation in this phase).
- `packages/phoria-islands/src/client/directives.browser.test.ts` — directive behavior in a real browser.

**New files (.NET):**
- `packages/Phoria.Tests/Phoria.Tests.csproj`
- `packages/Phoria.Tests/Islands/PhoriaIslandPropsSerializerTests.cs`
- `packages/Phoria.Tests/Vite/ViteManifestTests.cs`
- `packages/Phoria.Tests/PhoriaOptionsExtensionsTests.cs` (or the relevant options unit)

**New files (smoke test):**
- `e2e/framework-multiple/tests/smoke.test.ts`
- `e2e/framework-multiple/vitest.smoke.config.ts`

**Modified files:**
- `pnpm-workspace.yaml` — add catalog entries for `vitest`, `@vitest/browser`, `playwright`, `@testing-library/*` as needed.
- `Directory.Packages.props` — add xUnit / test package versions.
- `Phoria.sln` — add `Phoria.Tests` project.
- Each JS `package.json` — add `test` (and `test:browser` where applicable) scripts.
- `e2e/framework-multiple/package.json` — add `test:smoke` script.
- `nx.json` — add `test` target default.
- `.github/workflows/ci.yml` — add unit-test step + separate smoke-test job.

---

## Task 1: Add Vitest to the core `phoria-islands` package with a first unit test

**Files:**
- Modify: `pnpm-workspace.yaml` (add `vitest` to catalog)
- Modify: `packages/phoria-islands/package.json` (add devDependency + `test` script)
- Create: `packages/phoria-islands/vitest.config.ts`
- Test: `packages/phoria-islands/src/server/appsettings.test.ts`

**Interfaces:**
- Consumes: `getEnvAppsettingsFileName` is not exported. Test the exported `parsePhoriaAppSettings` and `getPhoriaAppSettings` from `packages/phoria-islands/src/server/appsettings.ts`.
- Produces: a working `pnpm --filter @phoria/phoria test` command and `vitest.config.ts` pattern reused by later tasks.

- [ ] **Step 1: Add `vitest` to the pnpm catalog**

In `pnpm-workspace.yaml`, under `catalog:`, add (keep alphabetical, tabs):

```yaml
  vitest: ^3.2.7
```

> Version note: `3.2.7` is the current established Vitest major. Before running, confirm the latest 3.x with `npm view vitest@3 version` and use that; keep `@vitest/browser` on the exact same version as `vitest`.

- [ ] **Step 2: Add the dependency and `test` script to the package**

In `packages/phoria-islands/package.json`, add to `devDependencies` (tabs, alphabetical):

```json
		"vitest": "catalog:"
```

And add to `scripts`:

```json
		"test": "vitest run"
```

- [ ] **Step 3: Create the Vitest config**

Create `packages/phoria-islands/vitest.config.ts`:

```ts
import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths()],
	test: {
		environment: "node",
		include: ["src/**/*.test.ts"]
	}
})
```

- [ ] **Step 4: Write the failing test**

Create `packages/phoria-islands/src/server/appsettings.test.ts`:

```ts
import { describe, expect, it } from "vitest"
import { parsePhoriaAppSettings } from "./appsettings"

describe("parsePhoriaAppSettings", () => {
	it("throws when `entry` is missing", async () => {
		await expect(
			parsePhoriaAppSettings({ cwd: "/nonexistent-path-for-test", inlineSettings: { ssrEntry: "ssr.ts" } })
		).rejects.toThrow("`entry` is required")
	})

	it("applies default root and base when not provided", async () => {
		const settings = await parsePhoriaAppSettings({
			cwd: "/nonexistent-path-for-test",
			inlineSettings: { entry: "entry.ts", ssrEntry: "ssr.ts" }
		})

		expect(settings.root).toBe("ui")
		expect(settings.base).toBe("/ui")
		expect(settings.server.port).toBe(5173)
	})
})
```

- [ ] **Step 5: Install and run the test to verify it passes**

Run: `pnpm install && pnpm --filter @phoria/phoria test`
Expected: PASS (2 tests). If `parseAppSettings` reads a real `appsettings.json` up the tree from `/nonexistent-path-for-test`, `up()` returns undefined and inline/defaults apply — confirm both tests pass. If the harness environment surfaces a real appsettings file, change `cwd` to `path.join(os.tmpdir(), "phoria-test-nonexistent")`.

- [ ] **Step 6: Lint and typecheck**

Run: `pnpm --filter @phoria/phoria lint && pnpm --filter @phoria/phoria check`
Expected: PASS. Fix any Biome/tsc issues in the new files.

- [ ] **Step 7: Commit**

```bash
git add pnpm-workspace.yaml pnpm-lock.yaml packages/phoria-islands/package.json packages/phoria-islands/vitest.config.ts packages/phoria-islands/src/server/appsettings.test.ts
git commit -m "test: add vitest to phoria-islands with appsettings unit tests"
```

---

## Task 2: Unit-test the component/framework registry

**Files:**
- Test: `packages/phoria-islands/src/register.test.ts`

**Interfaces:**
- Consumes: `registerCsrService`, `registerComponent`, `getComponent`, `getFrameworks` from `packages/phoria-islands/src/register.ts`. Note the registry uses **module-level singletons** (`frameworkRegistry`, `componentRegistry`) — state persists across tests in a file. Use unique framework/component names per test to avoid cross-test coupling, or `vi.resetModules()` in `beforeEach` with dynamic `import()`.
- Produces: coverage of registration invariants relied on by all framework packages.

- [ ] **Step 1: Write the failing test**

Create `packages/phoria-islands/src/register.test.ts`:

```ts
import { beforeEach, describe, expect, it, vi } from "vitest"

describe("register", () => {
	beforeEach(() => {
		vi.resetModules()
	})

	it("throws when registering a component for an unregistered framework", async () => {
		const { registerComponent } = await import("./register")

		expect(() =>
			registerComponent("Widget", { framework: "unknown", loader: async () => ({ default: {} }) })
		).toThrow('the "unknown" framework has not been registered')
	})

	it("registers a component after its framework is registered and looks it up case-insensitively", async () => {
		const { registerCsrService, registerComponent, getComponent } = await import("./register")

		registerCsrService("React", { mount: async () => {} })
		registerComponent("Widget", { framework: "React", loader: async () => ({ default: {} }) })

		const entry = getComponent("widget")

		expect(entry?.name).toBe("Widget")
		expect(entry?.framework).toBe("React")
	})

	it("normalises framework names to lowercase in getFrameworks", async () => {
		const { registerCsrService, getFrameworks } = await import("./register")

		registerCsrService("Vue", { mount: async () => {} })

		expect(getFrameworks()).toContain("vue")
	})
})
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `pnpm --filter @phoria/phoria test`
Expected: PASS (3 tests + prior tests).

- [ ] **Step 3: Lint and typecheck**

Run: `pnpm --filter @phoria/phoria lint && pnpm --filter @phoria/phoria check`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add packages/phoria-islands/src/register.test.ts
git commit -m "test: cover component and framework registry in phoria-islands"
```

---

## Task 3: Unit-test `importComponent` module loading

**Files:**
- Test: `packages/phoria-islands/src/phoria-island.test.ts`

**Interfaces:**
- Consumes: `importComponent` from `packages/phoria-islands/src/phoria-island.ts`. It accepts a `PhoriaIslandComponentEntry` whose `loader` is either a default-export function or a `{ module, component }` pair.
- Produces: coverage of the default-vs-named export resolution and the missing-default error path.

- [ ] **Step 1: Write the failing test**

Create `packages/phoria-islands/src/phoria-island.test.ts`:

```ts
import { describe, expect, it } from "vitest"
import { importComponent } from "./phoria-island"

describe("importComponent", () => {
	it("resolves the default export loader", async () => {
		const result = await importComponent({
			name: "Widget",
			framework: "react",
			loader: async () => ({ default: "component", __phoriaComponentPath: "/widget.tsx" })
		})

		expect(result.component).toBe("component")
		expect(result.componentName).toBe("Widget")
		expect(result.componentPath).toBe("/widget.tsx")
	})

	it("throws when the default export is missing", async () => {
		await expect(
			importComponent({ name: "Widget", framework: "react", loader: async () => ({}) })
		).rejects.toThrow("must be exposed as the default export")
	})

	it("resolves a named export via the module/component loader pair", async () => {
		const result = await importComponent({
			name: "Widget",
			framework: "react",
			loader: {
				module: async () => ({ Named: "named-component", __phoriaComponentPath: "/w.tsx" }),
				component: (m) => m.Named
			}
		})

		expect(result.component).toBe("named-component")
		expect(result.componentPath).toBe("/w.tsx")
	})
})
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `pnpm --filter @phoria/phoria test`
Expected: PASS.

- [ ] **Step 3: Lint, typecheck, commit**

Run: `pnpm --filter @phoria/phoria lint && pnpm --filter @phoria/phoria check`

```bash
git add packages/phoria-islands/src/phoria-island.test.ts
git commit -m "test: cover importComponent module loading in phoria-islands"
```

---

## Task 4: Add Vitest to the remaining JS packages (harness only)

**Files:**
- Modify: `packages/phoria-react/package.json`, `packages/phoria-svelte/package.json`, `packages/phoria-vue/package.json`, `packages/vite-plugin-dotnet-dev-certs/package.json` (add `vitest` devDep + `test` script)
- Create: `vitest.config.ts` in each of those four packages
- Test: `packages/vite-plugin-dotnet-dev-certs/src/plugin.test.ts` (one real assertion so the harness is proven)

**Interfaces:**
- Consumes: exported members of `packages/vite-plugin-dotnet-dev-certs/src/plugin.ts` (inspect exports before writing the assertion).
- Produces: `pnpm --filter <pkg> test` works for every JS package; enables `lerna run test` in Task 9.

- [ ] **Step 1: Add `test` script + `vitest` devDep to each of the four `package.json` files**

In each, add to `scripts`:

```json
		"test": "vitest run"
```

and to `devDependencies`:

```json
		"vitest": "catalog:"
```

- [ ] **Step 2: Create `vitest.config.ts` in each of the four packages**

Same content as Task 1 Step 3 (node environment, `tsconfigPaths`, `include: ["src/**/*.test.ts"]`).

- [ ] **Step 3: Inspect exports and write one real test for the dev-certs plugin**

Read `packages/vite-plugin-dotnet-dev-certs/src/plugin.ts`, identify a pure exported unit (e.g. the plugin factory returning `{ name }` or an options resolver). Create `packages/vite-plugin-dotnet-dev-certs/src/plugin.test.ts` asserting the plugin object's `name` (and any pure option-defaulting logic). Example skeleton (adjust to real export name):

```ts
import { describe, expect, it } from "vitest"
import dotnetDevCerts from "./plugin"

describe("dotnetDevCerts plugin", () => {
	it("returns a vite plugin with the expected name", () => {
		const plugin = dotnetDevCerts()
		expect(plugin.name).toBeTypeOf("string")
	})
})
```

For `phoria-react`/`phoria-svelte`/`phoria-vue`, no node-unit test is required yet (their meaningful tests are browser-mode in Task 5). Add a placeholder-free minimal test only if a pure exported unit exists; otherwise leave the harness ready and covered by Task 5. Do NOT commit empty test files.

- [ ] **Step 4: Install and run all package tests**

Run: `pnpm install && pnpm --filter @phoria/vite-plugin-dotnet-dev-certs test`
Expected: PASS.

- [ ] **Step 5: Lint, typecheck, commit**

Run: `pnpm --filter @phoria/vite-plugin-dotnet-dev-certs lint && pnpm --filter @phoria/vite-plugin-dotnet-dev-certs check`

```bash
git add packages/phoria-react/package.json packages/phoria-svelte/package.json packages/phoria-vue/package.json packages/vite-plugin-dotnet-dev-certs/package.json packages/phoria-react/vitest.config.ts packages/phoria-svelte/vitest.config.ts packages/phoria-vue/vitest.config.ts packages/vite-plugin-dotnet-dev-certs/vitest.config.ts packages/vite-plugin-dotnet-dev-certs/src/plugin.test.ts pnpm-lock.yaml
git commit -m "test: add vitest harness to remaining js packages"
```

---

## Task 5: Add Vitest browser mode and test client directives + a React island

**Files:**
- Modify: `pnpm-workspace.yaml` (catalog: `@vitest/browser`, `playwright`)
- Modify: `packages/phoria-islands/package.json`, `packages/phoria-react/package.json` (add browser deps + `test:browser` script)
- Create: `packages/phoria-islands/vitest.browser.config.ts`
- Create: `packages/phoria-react/vitest.browser.config.ts`
- Test: `packages/phoria-islands/src/client/directives.browser.test.ts`
- Test: `packages/phoria-react/src/client/csr.browser.test.tsx`

**Interfaces:**
- Consumes: `visible`, `media`, `idle` from `packages/phoria-islands/src/client/directives.ts` (each is `(mount, { element, value }) => Promise<void>`); the React CSR `service.mount(island, component, props, options)` from `packages/phoria-react/src/client/csr.ts`.
- Produces: real-browser coverage of directive behavior and React CSR mount/hydrate; the browser-config pattern reusable for Svelte/Vue post-phase.

- [ ] **Step 1: Add browser-mode deps to the catalog**

In `pnpm-workspace.yaml` `catalog:` add:

```yaml
  "@vitest/browser": ^3.2.7
  playwright: ^1.62.0
```

> `@vitest/browser` MUST match the `vitest` version exactly. Confirm the current Playwright with `npm view playwright version`.

- [ ] **Step 2: Add deps + `test:browser` script to the two packages**

In `packages/phoria-islands/package.json` and `packages/phoria-react/package.json`, add to `devDependencies`:

```json
		"@vitest/browser": "catalog:",
		"playwright": "catalog:"
```

and to `scripts`:

```json
		"test:browser": "vitest run --config vitest.browser.config.ts"
```

- [ ] **Step 3: Create the browser Vitest config (islands)**

Create `packages/phoria-islands/vitest.browser.config.ts`:

```ts
import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths()],
	test: {
		include: ["src/**/*.browser.test.ts", "src/**/*.browser.test.tsx"],
		browser: {
			enabled: true,
			provider: "playwright",
			headless: true,
			instances: [{ browser: "chromium" }]
		}
	}
})
```

- [ ] **Step 4: Write the failing directive browser test**

Create `packages/phoria-islands/src/client/directives.browser.test.ts`:

```ts
import { describe, expect, it, vi } from "vitest"
import { media, visible } from "./directives"

describe("visible directive", () => {
	it("calls mount when the element intersects the viewport", async () => {
		const element = document.createElement("div")
		document.body.appendChild(element)

		const mount = vi.fn(async () => {})

		await visible(mount, { element: element as never, component: "Widget", value: null })

		await vi.waitFor(() => expect(mount).toHaveBeenCalledTimes(1), { timeout: 2000 })

		element.remove()
	})
})

describe("media directive", () => {
	it("throws when no query is provided", async () => {
		const mount = vi.fn(async () => {})

		await expect(media(mount, { element: {} as never, component: "Widget", value: null })).rejects.toThrow(
			'No "query" specified'
		)
	})

	it("mounts immediately when the media query already matches", async () => {
		const mount = vi.fn(async () => {})

		await media(mount, { element: {} as never, component: "Widget", value: "all" })

		await vi.waitFor(() => expect(mount).toHaveBeenCalledTimes(1), { timeout: 2000 })
	})
})
```

- [ ] **Step 5: Install Playwright browsers and run the directive browser tests**

Run: `pnpm install && pnpm exec playwright install chromium && pnpm --filter @phoria/phoria test:browser`
Expected: PASS. `visible` uses a real `IntersectionObserver` and `media` a real `matchMedia`, both available in the browser environment.

- [ ] **Step 6: Create the browser config + React island mount test**

Create `packages/phoria-react/vitest.browser.config.ts` (same as Step 3 but for this package; include `@vitejs/plugin-react` if JSX transform is needed):

```ts
import react from "@vitejs/plugin-react"
import tsconfigPaths from "vite-tsconfig-paths"
import { defineConfig } from "vitest/config"

export default defineConfig({
	plugins: [tsconfigPaths(), react()],
	test: {
		include: ["src/**/*.browser.test.tsx"],
		browser: {
			enabled: true,
			provider: "playwright",
			headless: true,
			instances: [{ browser: "chromium" }]
		}
	}
})
```

Create `packages/phoria-react/src/client/csr.browser.test.tsx`. First read `packages/phoria-react/src/client/csr.ts` to confirm the exported `service` shape and the `PhoriaIslandComponentEntry` fields it expects, then:

```tsx
import { createElement } from "react"
import { describe, expect, it } from "vitest"
import { service } from "./csr"

describe("react csr service", () => {
	it("renders a component into the island element", async () => {
		const island = document.createElement("div")
		document.body.appendChild(island)

		const Hello = (props: { name: string }) => createElement("span", null, `Hello ${props.name}`)

		await service.mount(
			island,
			{ name: "Hello", framework: "react", loader: async () => ({ default: Hello }) },
			{ name: "World" },
			{ mode: "render" }
		)

		await vi.waitFor(() => expect(island.textContent).toContain("Hello World"))

		island.remove()
	})
})
```

If `service.mount`'s exact signature or the render/await timing differs, adjust to the real API confirmed from `csr.ts` — do not guess; read the file.

- [ ] **Step 7: Run the React browser test**

Run: `pnpm --filter @phoria/phoria-react test:browser`
Expected: PASS.

- [ ] **Step 8: Lint, typecheck, commit**

Run: `pnpm --filter @phoria/phoria lint && pnpm --filter @phoria/phoria-react lint`

```bash
git add pnpm-workspace.yaml pnpm-lock.yaml packages/phoria-islands/package.json packages/phoria-react/package.json packages/phoria-islands/vitest.browser.config.ts packages/phoria-react/vitest.browser.config.ts packages/phoria-islands/src/client/directives.browser.test.ts packages/phoria-react/src/client/csr.browser.test.tsx
git commit -m "test: add vitest browser-mode tests for directives and react csr"
```

---

## Task 6: Scaffold the `Phoria.Tests` xUnit project

**Files:**
- Modify: `Directory.Packages.props` (add test package versions)
- Create: `packages/Phoria.Tests/Phoria.Tests.csproj`
- Modify: `Phoria.sln` (add the project)
- Test: `packages/Phoria.Tests/Islands/PhoriaIslandPropsSerializerTests.cs`

**Interfaces:**
- Consumes: `SystemTextJsonPropsSerializer` / `IPhoriaIslandPropsSerializer` from `packages/Phoria/Islands/PhoriaIslandPropsSerializer.cs` (`Serialize(object props) => string`).
- Produces: a `dotnet test` target for the solution; reference project for Tasks 7–8.

- [ ] **Step 1: Add test package versions to central package management**

In `Directory.Packages.props`, add `PackageVersion` entries (confirm current versions with `dotnet package search <id>` or nuget.org before pinning):

```xml
		<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
		<PackageVersion Include="xunit" Version="2.9.3" />
		<PackageVersion Include="xunit.runner.visualstudio" Version="3.1.5" />
```

- [ ] **Step 2: Create the test project file**

Create `packages/Phoria.Tests/Phoria.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFrameworks>net8.0;net9.0</TargetFrameworks>
		<IsPackable>false</IsPackable>
		<Nullable>enable</Nullable>
		<ImplicitUsings>enable</ImplicitUsings>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Microsoft.NET.Test.Sdk" />
		<PackageReference Include="xunit" />
		<PackageReference Include="xunit.runner.visualstudio" />
	</ItemGroup>

	<ItemGroup>
		<ProjectReference Include="..\Phoria\Phoria.csproj" />
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Write the first failing test**

Create `packages/Phoria.Tests/Islands/PhoriaIslandPropsSerializerTests.cs`:

```csharp
using System.Text.Json;
using Phoria.Islands;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandPropsSerializerTests
{
	[Fact]
	public void Serialize_WithCamelCaseOptions_ProducesCamelCaseJson()
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};
		var serializer = new SystemTextJsonPropsSerializer(options);

		var result = serializer.Serialize(new { FirstName = "Ada" });

		Assert.Equal("{\"firstName\":\"Ada\"}", result);
	}

	[Fact]
	public void Serialize_WithConfigureAction_AppliesConfiguredOptions()
	{
		var serializer = new SystemTextJsonPropsSerializer(o => o.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

		var result = serializer.Serialize(new { LastName = "Lovelace" });

		Assert.Equal("{\"lastName\":\"Lovelace\"}", result);
	}
}
```

- [ ] **Step 4: Add the project to the solution**

Run: `dotnet sln Phoria.sln add packages/Phoria.Tests/Phoria.Tests.csproj`
Expected: "Project ... added to the solution."

- [ ] **Step 5: Run the tests**

Run: `dotnet test Phoria.sln`
Expected: PASS (2 tests). Fix any build errors.

- [ ] **Step 6: Commit**

```bash
git add Directory.Packages.props Phoria.sln "packages/Phoria.Tests/Phoria.Tests.csproj" "packages/Phoria.Tests/Islands/PhoriaIslandPropsSerializerTests.cs"
git commit -m "test: scaffold Phoria.Tests xunit project with props serializer tests"
```

---

## Task 7: Unit-test `ViteManifest`

**Files:**
- Test: `packages/Phoria.Tests/Vite/ViteManifestTests.cs`

**Interfaces:**
- Consumes: `ViteManifest`, `IViteManifest`, `ViteChunk`, `IViteChunk` from `packages/Phoria/Vite/`. Read `packages/Phoria/Vite/ViteChunk.cs` first to confirm the `ViteChunk` constructor/property shape before writing the test.
- Produces: coverage of manifest lookup, `ContainsKey`, `Keys`, and enumeration.

- [ ] **Step 1: Confirm `ViteChunk` shape**

Read `packages/Phoria/Vite/ViteChunk.cs` to determine how to construct a `ViteChunk` (constructor args or object initializer) and its identifying property (e.g. `File`).

- [ ] **Step 2: Write the failing test**

Create `packages/Phoria.Tests/Vite/ViteManifestTests.cs` (adjust `ViteChunk` construction to the real shape found in Step 1):

```csharp
using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Vite;

public class ViteManifestTests
{
	private static ViteManifest CreateManifest()
	{
		var chunks = new Dictionary<string, ViteChunk>
		{
			// Replace with the real ViteChunk construction confirmed in Step 1
			["main.ts"] = new ViteChunk()
		};

		return new ViteManifest(chunks);
	}

	[Fact]
	public void Indexer_WithKnownKey_ReturnsChunk()
	{
		var manifest = CreateManifest();

		Assert.NotNull(((IViteManifest)manifest)["main.ts"]);
	}

	[Fact]
	public void Indexer_WithUnknownKey_ReturnsNull()
	{
		var manifest = CreateManifest();

		Assert.Null(((IViteManifest)manifest)["missing.ts"]);
	}

	[Fact]
	public void ContainsKey_WithKnownKey_ReturnsTrue()
	{
		IViteManifest manifest = CreateManifest();

		Assert.True(manifest.ContainsKey("main.ts"));
	}

	[Fact]
	public void Keys_ReturnsAllChunkKeys()
	{
		IViteManifest manifest = CreateManifest();

		Assert.Contains("main.ts", manifest.Keys);
	}
}
```

- [ ] **Step 3: Run the tests**

Run: `dotnet test Phoria.sln`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add "packages/Phoria.Tests/Vite/ViteManifestTests.cs"
git commit -m "test: cover ViteManifest lookup behaviour"
```

---

## Task 8: Unit-test a Phoria options unit

**Files:**
- Test: `packages/Phoria.Tests/PhoriaOptionsExtensionsTests.cs`

**Interfaces:**
- Consumes: read `packages/Phoria/PhoriaOptions.cs` and `packages/Phoria/PhoriaOptionsExtensions.cs` to find a pure, testable unit (e.g. default values or an options-transform/URL-helper). Choose the unit that is deterministic and dependency-free.
- Produces: coverage of options defaults/derivation that the appsettings defaults must stay in sync with (see `appsettings.ts` comment "Defaults here must be in sync with `Phoria/PhoriaOptions.cs`").

- [ ] **Step 1: Identify the unit and write the failing test**

Read the two files, then create `packages/Phoria.Tests/PhoriaOptionsExtensionsTests.cs` asserting concrete default values or derivation. Example (replace with real property names/defaults confirmed from source — e.g. assert `PhoriaOptions` default `Root == "ui"`, `Base == "/ui"`, `SsrBase == "/ssr"` to mirror the JS defaults):

```csharp
using Phoria;
using Xunit;

namespace Phoria.Tests;

public class PhoriaOptionsTests
{
	[Fact]
	public void DefaultOptions_MatchJavaScriptDefaults()
	{
		var options = new PhoriaOptions();

		Assert.Equal("ui", options.Root);
		Assert.Equal("/ui", options.Base);
		Assert.Equal("/ssr", options.SsrBase);
	}
}
```

If the real property names/defaults differ, use the actual ones from `PhoriaOptions.cs`. If no dependency-free unit exists, test `PhoriaIslandUrlHelper` (in `packages/Phoria/Islands/PhoriaIslandUrlHelper.cs`) instead — read it and assert a pure URL-combination method. Name the file/class after whichever unit is chosen.

- [ ] **Step 2: Run the tests**

Run: `dotnet test Phoria.sln`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add packages/Phoria.Tests/
git commit -m "test: cover phoria options defaults"
```

---

## Task 9: Aggregate JS tests via Lerna and Nx

**Files:**
- Modify: `nx.json` (add `test` target default)
- Modify: `package.json` (root — replace stub `test` script)

**Interfaces:**
- Consumes: the per-package `test` scripts from Tasks 1 and 4.
- Produces: `pnpm test` running all JS package tests; `test:browser` aggregation.

- [ ] **Step 1: Add a `test` target default to Nx**

In `nx.json`, add under `targetDefaults`:

```json
		"test": {
			"cache": true,
			"dependsOn": ["^build"]
		},
		"test:browser": {
			"cache": true,
			"dependsOn": ["^build"]
		}
```

- [ ] **Step 2: Replace the root `test` stub**

In `package.json` (root), replace:

```json
		"test": "echo \"Error: no test specified\" && exit 1",
```

with:

```json
		"test": "lerna run test",
		"test:browser": "lerna run test:browser",
```

- [ ] **Step 3: Build then run all tests through Lerna**

Run: `pnpm lerna run build && pnpm test`
Expected: all package `test` scripts run and PASS. Packages without a `test` script are skipped by Lerna.

- [ ] **Step 4: Run browser tests through Lerna**

Run: `pnpm exec playwright install chromium && pnpm test:browser`
Expected: browser tests in `phoria-islands` and `phoria-react` PASS.

- [ ] **Step 5: Commit**

```bash
git add nx.json package.json
git commit -m "test: aggregate package tests via lerna and nx test targets"
```

---

## Task 10: Add a minimal full-stack smoke test

**Files:**
- Modify: `e2e/framework-multiple/package.json` (add `test:smoke` script + vitest/playwright devDeps)
- Create: `e2e/framework-multiple/vitest.smoke.config.ts`
- Create: `e2e/framework-multiple/tests/smoke.test.ts`

**Interfaces:**
- Consumes: the built + previewed `framework-multiple` app. The Preview profile serves the .NET app at `http://localhost:5247` (from `WebApp/Properties/launchSettings.json`) and the vite server via `preview:server`. The SSR health endpoint is at `{ssrBase}/hc` → default `/ssr/hc` on the vite server; the .NET app renders islands on its pages.
- Produces: a smoke test proving the production build renders islands end-to-end.

- [ ] **Step 1: Confirm the preview wiring and health route**

Read `e2e/framework-multiple/package.json` `preview:*` scripts and `e2e/framework-multiple/WebApp/ui/src/server.ts`. Confirm: (a) the vite server port in production preview, (b) that `/ssr/hc` is the health route (from `routing.ts` `createPhoriaSsrRouter` — `/hc` at router root, SSR render under `ssrBase`), and (c) the .NET app URL `http://localhost:5247`. Record the exact URLs to assert against.

- [ ] **Step 2: Add devDeps + script to the e2e package**

In `e2e/framework-multiple/package.json`, add to `devDependencies`:

```json
		"vitest": "catalog:"
```

and to `scripts`:

```json
		"test:smoke": "vitest run --config vitest.smoke.config.ts"
```

- [ ] **Step 3: Create the smoke vitest config**

Create `e2e/framework-multiple/vitest.smoke.config.ts`:

```ts
import { defineConfig } from "vitest/config"

export default defineConfig({
	test: {
		environment: "node",
		include: ["tests/**/*.test.ts"],
		testTimeout: 30_000,
		hookTimeout: 60_000
	}
})
```

- [ ] **Step 4: Write the smoke test**

Create `e2e/framework-multiple/tests/smoke.test.ts`. This test assumes the app has been built and preview servers are already running (started by the CI job / developer before invoking — see Step 5). It only asserts HTTP responses:

```ts
import { describe, expect, it } from "vitest"

const webAppUrl = process.env.PHORIA_WEBAPP_URL ?? "http://localhost:5247"

describe("framework-multiple smoke", () => {
	it("serves the home page with rendered island markup", async () => {
		const response = await fetch(webAppUrl)

		expect(response.status).toBe(200)

		const html = await response.text()

		// The index page renders islands; assert the custom element wrapper is present
		expect(html).toContain("phoria-island")
	})
})
```

If Step 1 reveals a more specific rendered marker (a component's text, or a `data-` attribute), assert that instead of / in addition to `phoria-island`. Read `WebApp/Pages/Index.cshtml` to pick a stable assertion string.

- [ ] **Step 5: Run the smoke test locally against a preview build**

Run (in `e2e/framework-multiple`):

```bash
pnpm build
pnpm preview &
# wait for the .NET app and vite server to be listening, then:
pnpm test:smoke
```

Expected: PASS. Stop the preview processes afterwards. If startup timing is flaky, add a short poll/retry loop in the test's first `it` using `vi.waitFor` around the `fetch`.

- [ ] **Step 6: Commit**

```bash
git add "e2e/framework-multiple/package.json" "e2e/framework-multiple/vitest.smoke.config.ts" "e2e/framework-multiple/tests/smoke.test.ts" pnpm-lock.yaml
git commit -m "test: add full-stack smoke test for framework-multiple e2e app"
```

---

## Task 11: Wire tests into CI

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Consumes: `pnpm test` (Task 9), `dotnet test` (Task 6), `pnpm test:browser` (Task 5), the e2e `test:smoke` (Task 10).
- Produces: CI that fails on any test regression; browser + smoke tests isolated in their own job.

- [ ] **Step 1: Add unit test steps to the existing `lint` job**

In `.github/workflows/ci.yml`, after the "Check source code" step, add:

```yaml
      - name: Test packages (unit)
        run: pnpm lerna run test

      - name: Test dotnet
        run: dotnet test Phoria.sln --configuration Release
```

- [ ] **Step 2: Add a separate browser + smoke test job**

Add a new job (sibling to `lint`) in `ci.yml`:

```yaml
  test-browser:
    name: Browser & smoke tests
    runs-on: ubuntu-latest
    timeout-minutes: 15
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup pnpm
        uses: pnpm/action-setup@v3

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version-file: ".nvmrc"
          cache: "pnpm"

      - name: Setup dotnet
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: "./global.json"

      - name: Install dependencies
        run: pnpm install

      - name: Build packages
        run: pnpm lerna run build

      - name: Install Playwright browsers
        run: pnpm exec playwright install --with-deps chromium

      - name: Browser-mode component tests
        run: pnpm lerna run test:browser
```

- [ ] **Step 3: Add smoke test steps to the browser job**

Append to the `test-browser` job steps (build + preview the e2e app, run smoke test, tear down). Use the exact commands confirmed in Task 10 Step 1:

```yaml
      - name: Build e2e app
        run: pnpm --filter framework-multiple build

      - name: Start preview and run smoke test
        run: |
          pnpm --filter framework-multiple preview &
          PREVIEW_PID=$!
          npx wait-on http://localhost:5247 --timeout 60000
          pnpm --filter framework-multiple test:smoke
          kill $PREVIEW_PID
```

If `wait-on` is not desired as an npx dependency, replace with a short bash `curl --retry` loop polling `http://localhost:5247`.

- [ ] **Step 4: Validate the workflow YAML**

Run: `pnpm dlx yaml-lint .github/workflows/ci.yml` (or visually confirm indentation). Expected: valid YAML.

- [ ] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: run unit, browser, and smoke tests"
```

---

## Task 12: Update project docs to reflect the test foundation

**Files:**
- Modify: `AGENTS.md` (Commands section — add Test)
- Modify: `docs/MEMORY.md` (append dated entry)

**Interfaces:**
- Consumes: the finalized `test` / `test:browser` / `dotnet test` / `test:smoke` commands.
- Produces: accurate contributor docs.

- [ ] **Step 1: Add a Test subsection to `AGENTS.md` Commands**

After the "Type Check" section, add:

```markdown
### Test

```bash
pnpm test            # Vitest unit tests across JS packages (via Lerna)
pnpm test:browser    # Vitest browser-mode component tests (Playwright provider)
dotnet test Phoria.sln  # xUnit tests for the Phoria .NET package
```

E2E smoke test (requires a preview build running):

```bash
pnpm --filter framework-multiple test:smoke
```
```

- [ ] **Step 2: Update the CI Order note in `AGENTS.md`**

Change the CI Order line to reflect tests running after check:

```markdown
The CI pipeline runs: `build` → `lint` → `check` → `test`. Always build before linting, type-checking, or testing.
```

- [ ] **Step 3: Append a MEMORY entry**

Add to `docs/MEMORY.md`:

```markdown
## 2026-07-26 — Phase 0 test foundation

- Chose Vitest (unit + browser mode via Playwright provider) for JS, xUnit with built-in `Assert` for .NET — one runner per ecosystem, browser mode avoids a separate Playwright toolchain for component-focused tests.
- Deliberately avoided FluentAssertions: v8+ is commercially licensed, incompatible with an MIT OSS project; xUnit's built-in asserts are sufficient.
- Co-located JS tests (`*.test.ts` / `*.browser.test.ts`); `Phoria.Tests` mirrors source namespaces with `Method_Scenario_ExpectedBehaviour` naming.
- Added a minimal full-stack smoke test (build + preview framework-multiple, assert rendered island) to cover the .NET↔vite SSR path not exercised by browser-mode component tests.
- Browser + smoke tests run in a separate CI job (need browsers + dotnet runtime); no enforced coverage threshold this phase.
```

- [ ] **Step 4: Commit**

```bash
git add AGENTS.md docs/MEMORY.md
git commit -m "docs: document the test foundation commands and decisions"
```

---

## Self-Review

**Spec coverage (Phase 0 scope from `docs/PROJECT.md`):**
- Vitest unit tests (JS packages) → Tasks 1–4, 9. ✓
- Vitest browser-mode component tests → Task 5. ✓
- xUnit unit tests (.NET) → Tasks 6–8. ✓
- Playwright-under-Vitest for component tests (decision revision) → Task 5. ✓
- Minimal full-stack smoke test → Task 10. ✓
- Wire root `test` + Nx/Lerna → Task 9. ✓
- CI (unit inline + browser/smoke separate job) → Task 11. ✓
- Priority JS targets (island registration/hydration, ssr entries, appsettings/routing) → Tasks 1–5 (appsettings, register, importComponent, directives, csr). Routing handlers are integration-shaped and covered via the smoke test rather than unit tests — intentional.
- Priority .NET targets (PropsSerializer, ManifestReader, ComponentFactory, HtmlContent, Options) → Tasks 6–8 cover PropsSerializer, ViteManifest, Options; ComponentFactory/HtmlContent/ManifestReader deferred (require ASP.NET test host / file-provider fixtures) — noted as follow-ups below, acceptable for a foundation phase.
- Naming conventions (Decision 6) → Global Constraints + applied throughout. ✓

**Follow-ups intentionally deferred (not Phase 0 blockers):**
- .NET tests requiring an ASP.NET `TagHelper`/`HttpContext` or `IFileProvider` fixture: `PhoriaIslandTagHelper`, `PhoriaIslandHtmlContent`, `ViteManifestReader`. Add once the harness exists.
- Svelte/Vue browser-mode CSR tests (React is the reference in this phase).

**Placeholder scan:** No "TBD"/"TODO" left as implementation instructions; every code step has concrete content. Steps that require reading a source file before finalizing an assertion (Tasks 4, 5, 7, 8, 10) explicitly say which file and what to confirm — this is verification, not a placeholder.

**Type/name consistency:** `test` / `test:browser` / `test:smoke` script names are consistent across `package.json`, `nx.json`, Lerna aggregation, and CI. Config file names (`vitest.config.ts`, `vitest.browser.config.ts`, `vitest.smoke.config.ts`) are referenced consistently.
