# Convert E2E Apps to Standalone Examples — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert `e2e/framework-multiple` into a standalone example at `examples/framework-multiple`, delete `e2e/` (with-sidecar, with-workspace, framework-multiple source), and remove every reference to the e2e apps from the repo.

**Architecture:** The new example mirrors `examples/getting-started` exactly: an example root with .NET solution scaffolding + `AppHost/`, and a `WebApp/` that is itself a standalone pnpm workspace (own `pnpm-workspace.yaml`, committed `pnpm-lock.yaml`) referencing **published** phoria packages. The e2e apps leave the pnpm workspace; workspace/CI config that referenced them is cleaned up; both examples end in the registry state required by `pnpm examples:check`.

**Tech Stack:** pnpm workspaces + Turborepo + Changesets, Vite 8 (Rolldown), Vitest 4, Biome 2, .NET 10 + Aspire 13.4.6 AppHost, React 19 / Svelte 5 / Vue 3.

## Global Constraints

- **Examples are outside the pnpm workspace.** The root `pnpm-workspace.yaml` globs must not match `examples/**`. Each example owns `WebApp/pnpm-workspace.yaml` + committed `WebApp/pnpm-lock.yaml`.
- **Committed examples use registry ranges only** — no `workspace:*`, `link:`, or `file:` phoria refs; `.csproj` uses `<PackageReference Include="Phoria" />`; `Directory.Packages.props` pins `<PackageVersion Include="Phoria" Version="0.4.2" />`. Enforced by `pnpm examples:check` (run via `scripts/examples.js check`).
- **Package versions** (from repo `packages/*/package.json`): `@phoria/phoria` `^0.4.2`, `@phoria/phoria-react` `^0.4.2`, `@phoria/phoria-svelte` `^0.3.2`, `@phoria/phoria-vue` `^0.3.2`, `@phoria/vite-plugin-dotnet-dev-certs` `^0.2.1`, Phoria (.NET) `0.4.2`.
- **Example ports** must not collide with getting-started (`5373`/`6373` webapp, `5273` phoria): framework-multiple uses **webapp `http 5573` / `https 6573`**, **phoria server `5473`**.
- **Example formatting**: space indent (2), `trailingCommas: "all"`, lf — per `examples/getting-started/biome.jsonc` and `.editorconfig`. Do not use the repo root's tabs.
- **Known WIP limitation:** the published packages lag the `feature/server` source, so registry-state examples **may not build or run without error right now**. Structural checks (`examples:check`, `pnpm install`, formatting) must pass; full build/run is verified in **linked** state via `pnpm examples:link`.
- **Docs policy:** update live docs only (`AGENTS.md`, `docs/PROJECT.md`, `docs/ARCHITECTURE.md`, `TODO.md`, `examples/TODO.md`). Leave historical records (`docs/plans/`, `docs/superpowers/plans/`, `docs/superpowers/specs/`, `docs/MEMORY.md`) untouched.
- All shell commands run from the repo root unless a `cwd` is given. `git mv` is used for moves to preserve history.

---

## File Structure

**Created** (`examples/framework-multiple/`):
- `.editorconfig`, `.gitignore`, `.nvmrc` — copied from getting-started verbatim.
- `aspire.config.json` — new (points at `AppHost/AppHost.csproj`).
- `biome.jsonc` — copied from getting-started verbatim.
- `Directory.Build.props`, `nuget.config`, `global.json` — copied from getting-started verbatim.
- `Directory.Packages.props` — getting-started's + `<PackageVersion Include="Phoria" Version="0.4.2" />`.
- `FrameworkMultiple.slnx` — new (AppHost + WebApp only).
- `AppHost/{AppHost.csproj, appsettings.json, Properties/launchSettings.json}` — copied from getting-started verbatim.
- `AppHost/Program.cs` — getting-started's, with `webAppPort = 5573`.
- `WebApp/package.json`, `WebApp/pnpm-workspace.yaml` — new (registry ranges, standalone workspace).
- `WebApp/vite.config.ts`, `WebApp/tsconfig.json`, `WebApp/tsconfig.node.json`, `WebApp/vitest.e2e.config.ts` — rewritten (root paths now `ui/`).
- `WebApp/WebApp.csproj`, `WebApp/Program.cs`, `WebApp/appsettings*.json`, `WebApp/Properties/launchSettings.json` — rewritten (PackageReference, new ports, root `ui`).
- `WebApp/Components/{ReactCounterProps,ReactCounterTagHelper,ReactCounterViewComponent}.cs` — moved from `e2e/with-workspace/WebApp/Components/`.
- `WebApp/ui/tests/e2e/smoke.test.ts` — new.

**Moved** (`e2e/framework-multiple/` → `examples/framework-multiple/`):
- `WebApp/` wholesale (`Program.cs`, `Pages/`, `wwwroot/`, `ui/`, `Properties/`, `appsettings*.json`) via `git mv e2e/framework-multiple/WebApp examples/framework-multiple/WebApp`, then edited.
- Root JS files into `WebApp/`: `vite.config.ts`, `tsconfig.json`, `tsconfig.node.json`, `vitest.e2e.config.ts`, `svelte.config.js`, `postcss.config.cjs`.

**Deleted** (`e2e/`): `framework-multiple/` (Phoria.AppHost, sln, package.json, aspire.config.json, azure.yaml, infra/, tests/, Dockerfile), `with-sidecar/`, `with-workspace/`.

**Modified:**
- `pnpm-workspace.yaml` — remove e2e globs + `injectWorkspacePackages`.
- `.changeset/config.json` — remove `ignore`.
- `turbo.json` — remove `build:islands` + `preview` tasks.
- `.github/workflows/ci.yml` — remove `test-e2e` job.
- `scripts/examples.js` — add svelte/vue to `jsPackages`.
- `pnpm-lock.yaml` — regenerated.
- `examples/getting-started/*` — registry-state conversion (via `pnpm examples:bump` + slnx fix).
- Docs: `AGENTS.md`, `docs/PROJECT.md`, `docs/ARCHITECTURE.md`, `TODO.md`, `examples/TODO.md`.

---

### Task 1: Create the `examples/framework-multiple` skeleton (example root + AppHost)

**Files:**
- Create: `examples/framework-multiple/.editorconfig`, `.gitignore`, `.nvmrc`, `aspire.config.json`, `biome.jsonc`, `Directory.Build.props`, `Directory.Packages.props`, `FrameworkMultiple.slnx`, `global.json`, `nuget.config`
- Create: `examples/framework-multiple/AppHost/AppHost.csproj`, `appsettings.json`, `Properties/launchSettings.json`, `Program.cs`

**Depends on:** nothing. **Provides:** the example scaffold later tasks fill in.

- [ ] **Step 1: Create the directory and copy boilerplate from getting-started**

```bash
mkdir -p examples/framework-multiple/AppHost/Properties
cp examples/getting-started/.editorconfig examples/framework-multiple/.editorconfig
cp examples/getting-started/.gitignore examples/framework-multiple/.gitignore
cp examples/getting-started/.nvmrc examples/framework-multiple/.nvmrc
cp examples/getting-started/biome.jsonc examples/framework-multiple/biome.jsonc
cp examples/getting-started/Directory.Build.props examples/framework-multiple/Directory.Build.props
cp examples/getting-started/global.json examples/framework-multiple/global.json
cp examples/getting-started/nuget.config examples/framework-multiple/nuget.config
cp examples/getting-started/AppHost/AppHost.csproj examples/framework-multiple/AppHost/AppHost.csproj
cp examples/getting-started/AppHost/appsettings.json examples/framework-multiple/AppHost/appsettings.json
cp examples/getting-started/AppHost/Properties/launchSettings.json examples/framework-multiple/AppHost/Properties/launchSettings.json
```

- [ ] **Step 2: Create `Directory.Packages.props`**

`examples/framework-multiple/Directory.Packages.props`:
```xml
<Project>

	<PropertyGroup>
		<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
	</PropertyGroup>

	<ItemGroup>
		<PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.17.0" />
		<PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.17.0" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
		<PackageVersion Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="8.0.29" />
	</ItemGroup>

	<ItemGroup Condition="'$(TargetFramework)' == 'net10.0'">
		<PackageVersion Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" Version="10.0.10" />
	</ItemGroup>

	<ItemGroup>
		<PackageVersion Include="Phoria" Version="0.4.2" />
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Create `aspire.config.json`**

`examples/framework-multiple/aspire.config.json`:
```json
{
	"appHost": {
		"path": "AppHost/AppHost.csproj"
	}
}
```

- [ ] **Step 4: Create `FrameworkMultiple.slnx`**

`examples/framework-multiple/FrameworkMultiple.slnx`:
```xml
<Solution>
	<Project Path="AppHost/AppHost.csproj" />
	<Project Path="WebApp/WebApp.csproj" />
</Solution>
```

- [ ] **Step 5: Create `AppHost/Program.cs`** (getting-started's, webapp port 5573)

`examples/framework-multiple/AppHost/Program.cs`:
```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var workspaceRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, ".."));
var webAppDirectory = Path.GetFullPath(Path.Combine(workspaceRoot, "WebApp"));
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

var webAppConfiguration = new ConfigurationBuilder()
	.SetBasePath(webAppDirectory)
	.AddJsonFile("appsettings.json", optional: false)
	.AddJsonFile($"appsettings.{environment}.json", optional: true)
	.Build();

var webAppPort = 5573;

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: webAppPort, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(OtlpProtocol.HttpProtobuf);

// The Vite dev server resolves the root from `process.cwd()` and only discovers the vite config in the current
// directory, so package.json scripts must run from the same directory where `vite.config.ts` lives

var phoriaServerPort = int.TryParse(webAppConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : 5173;

builder.AddJavaScriptApp("phoria-server", webAppDirectory)
	.WithRunScript(isDevelopment ? "dev:server" : "preview:server")
	.WithPnpm(install: false)
	.WithHttpEndpoint(port: phoriaServerPort, name: "http", isProxied: false)
	.WithEnvironment("NODE_ENV", isDevelopment ? "development" : "production")
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(OtlpProtocol.HttpProtobuf);

builder.Build().Run();
```

- [ ] **Step 6: Commit**

```bash
git add examples/framework-multiple
git commit -m "chore(examples): scaffold framework-multiple example root and AppHost"
```

---

### Task 2: Move the WebApp and rewire the JS workspace

**Files:**
- Create: `examples/framework-multiple/WebApp/package.json`, `WebApp/pnpm-workspace.yaml`
- Move: `e2e/framework-multiple/WebApp` → `examples/framework-multiple/WebApp`
- Move: `e2e/framework-multiple/{vite.config.ts,tsconfig.json,tsconfig.node.json,vitest.e2e.config.ts,svelte.config.js,postcss.config.cjs}` → `examples/framework-multiple/WebApp/`
- Modify: `WebApp/vite.config.ts`, `WebApp/tsconfig.json`, `WebApp/tsconfig.node.json`, `WebApp/vitest.e2e.config.ts` (rewrite, paths now `ui/`)
- Create: `WebApp/ui/tests/e2e/smoke.test.ts`

**Depends on:** Task 1. **Provides:** a standalone installable JS workspace.

- [ ] **Step 1: Move the WebApp and JS root files**

```bash
git mv e2e/framework-multiple/WebApp examples/framework-multiple/WebApp
for f in vite.config.ts tsconfig.json tsconfig.node.json vitest.e2e.config.ts svelte.config.js postcss.config.cjs; do
	git mv "e2e/framework-multiple/$f" "examples/framework-multiple/WebApp/$f"
done
```

- [ ] **Step 2: Create `WebApp/package.json`** (registry ranges; all `catalog:` values made literal)

`examples/framework-multiple/WebApp/package.json`:
```json
{
	"name": "framework-multiple",
	"private": true,
	"version": "0.0.0",
	"type": "module",
	"files": [
		"ui/dist"
	],
	"scripts": {
		"build": "concurrently \"pnpm:build:*\"",
		"build:islands": "vite build --app",
		"build:webapp": "dotnet build WebApp.csproj --configuration Release",
		"check": "tsc",
		"dev": "aspire run",
		"dev:server": "tsx ./ui/src/server.ts",
		"lint": "biome check",
		"preview": "aspire start --environment Preview",
		"preview:server": "node ui/dist/server/server.js",
		"stop": "aspire stop --all",
		"test:e2e": "vitest run --config vitest.e2e.config.ts"
	},
	"dependencies": {
		"@opentelemetry/api-logs": "^0.221.0",
		"@opentelemetry/exporter-logs-otlp-http": "^0.221.0",
		"@opentelemetry/sdk-logs": "^0.221.0",
		"@phoria/phoria": "^0.4.2",
		"@phoria/phoria-react": "^0.4.2",
		"@phoria/phoria-svelte": "^0.3.2",
		"@phoria/phoria-vue": "^0.3.2",
		"h3": "^1.15.11",
		"listhen": "^1.10.1",
		"react": "^19.2.8",
		"react-dom": "^19.2.8",
		"svelte": "^5.56.8",
		"vue": "^3.5.40"
	},
	"devDependencies": {
		"@biomejs/biome": "^2.5.5",
		"@meeg/vite-plugin-inspect-config": "~0.3.0",
		"@phoria/vite-plugin-dotnet-dev-certs": "^0.2.1",
		"@sveltejs/vite-plugin-svelte": "^7.2.0",
		"@types/node": "^24.13.3",
		"@types/react": "^19.2.17",
		"@types/react-dom": "^19.2.3",
		"@vitejs/plugin-react": "^6.0.4",
		"@vitejs/plugin-vue": "^6.0.8",
		"concurrently": "^10.0.4",
		"postcss-preset-env": "^11.3.2",
		"tsx": "^4.23.1",
		"typescript": "~6.0.3",
		"vite": "^8.1.5",
		"vitest": "^4.1.10"
	}
}
```

- [ ] **Step 3: Create `WebApp/pnpm-workspace.yaml`** (shadows the root workspace)

`examples/framework-multiple/WebApp/pnpm-workspace.yaml`:
```yaml
# Standalone workspace for the framework-multiple example.
#
# This file shadows the repository-root pnpm-workspace.yaml (pnpm uses the
# nearest one), so the example installs in isolation with its own lockfile
# instead of joining the phoria monorepo. It also carries the build-script
# allowlist that pnpm 11 previously read from package.json.
allowBuilds:
  "@biomejs/biome": true
  "@parcel/watcher": true
  esbuild: true
```

- [ ] **Step 4: Rewrite `WebApp/vite.config.ts`** (drop `cwd`, drop explicit `publicDir` — `ui/` is the Vite root)

`examples/framework-multiple/WebApp/vite.config.ts`:
```ts
import { inspectConfig } from "@meeg/vite-plugin-inspect-config"
import { phoria } from "@phoria/phoria/vite"
import { phoriaReact } from "@phoria/phoria-react/vite"
import { phoriaSvelte } from "@phoria/phoria-svelte/vite"
import { phoriaVue } from "@phoria/phoria-vue/vite"
import { dotnetDevCerts } from "@phoria/vite-plugin-dotnet-dev-certs"
import { defineConfig } from "vite"

export default defineConfig({
	resolve: {
		tsconfigPaths: true
	},
	plugins: [dotnetDevCerts(), phoria(), phoriaReact(), phoriaSvelte(), phoriaVue(), inspectConfig()]
})
```

- [ ] **Step 5: Rewrite `WebApp/tsconfig.json`** (paths `./ui/src/*`, includes `ui/src`)

`examples/framework-multiple/WebApp/tsconfig.json`:
```json
{
	"compilerOptions": {
		"allowImportingTsExtensions": true,
		"esModuleInterop": true,
		"isolatedModules": true,
		"jsx": "react-jsx",
		"lib": ["ES2022", "DOM", "DOM.Iterable"],
		"module": "ESNext",
		"moduleDetection": "force",
		"moduleResolution": "bundler",
		"noEmit": true,
		"noFallthroughCasesInSwitch": true,
		"noUncheckedSideEffectImports": true,
		"noUnusedLocals": true,
		"noUnusedParameters": true,
		"paths": {
			"~/*": ["./ui/src/*"]
		},
		"resolveJsonModule": true,
		"skipLibCheck": true,
		"sourceMap": true,
		"strict": true,
		"target": "ES2022",
		"types": ["node", "vite/client"],
		"tsBuildInfoFile": "./node_modules/.tmp/tsconfig.app.tsbuildinfo",
		"useDefineForClassFields": true,
		"verbatimModuleSyntax": true
	},
	"include": ["ui/src/**/*.ts", "ui/src/**/*.d.ts", "ui/src/**/*.tsx", "ui/src/**/*.vue", "ui/src/**/*.svelte"]
}
```

- [ ] **Step 6: Rewrite `WebApp/tsconfig.node.json`** (include `ui/src/server.ts`)

`examples/framework-multiple/WebApp/tsconfig.node.json`:
```json
{
	"compilerOptions": {
		"allowImportingTsExtensions": true,
		"esModuleInterop": true,
		"isolatedModules": true,
		"lib": ["ES2022"],
		"module": "NodeNext",
		"moduleResolution": "NodeNext",
		"noEmit": true,
		"noFallthroughCasesInSwitch": true,
		"noUncheckedSideEffectImports": true,
		"noUnusedLocals": true,
		"noUnusedParameters": true,
		"resolveJsonModule": true,
		"skipLibCheck": true,
		"strict": true,
		"target": "ES2022",
		"types": ["node"],
		"tsBuildInfoFile": "./node_modules/.tmp/tsconfig.node.tsbuildinfo"
	},
	"include": ["vite.config.ts", "ui/src/server.ts"]
}
```

- [ ] **Step 7: Rewrite `WebApp/vitest.e2e.config.ts`** (include `ui/tests/e2e`)

`examples/framework-multiple/WebApp/vitest.e2e.config.ts`:
```ts
import { defineConfig } from "vitest/config"

export default defineConfig({
	test: {
		environment: "node",
		include: ["ui/tests/e2e/**/*.test.ts"],
		testTimeout: 30_000,
		hookTimeout: 60_000,
		env: {
			// The Aspire-hosted Web App redirects HTTP to its HTTPS endpoint using a dev certificate.
			NODE_TLS_REJECT_UNAUTHORIZED: "0"
		}
	}
})
```

- [ ] **Step 8: Create `WebApp/ui/tests/e2e/smoke.test.ts`** (new port + framework/filter/factory assertions)

`examples/framework-multiple/WebApp/ui/tests/e2e/smoke.test.ts`:
```ts
import { describe, expect, it } from "vitest"

const webAppUrl = process.env.PHORIA_WEBAPP_URL ?? "http://localhost:5573"

async function getHtml(path = "") {
	const response = await fetch(`${webAppUrl}${path}`)
	expect(response.status).toBe(200)
	return await response.text()
}

describe("framework-multiple e2e", () => {
	it("serves the home page with rendered island markup", async () => {
		const html = await getHtml()
		expect(html).toContain("phoria-island")
	})

	it("emits modulepreload directives for server-rendered islands", async () => {
		const html = await getHtml()
		expect(html).toMatch(/<link\s+rel="modulepreload"\s+crossorigin\s+href="\/ui\/assets\/[^"]+\.js">/)
	})

	it("server-renders all three framework counters", async () => {
		const html = await getHtml()
		expect(html).toContain("react-counter")
		expect(html).toContain("vue-counter")
		expect(html).toContain("svelte-counter")
	})

	it("renders the factory-based islands (ViewComponent and TagHelper)", async () => {
		const html = await getHtml()
		expect(html).toContain("count is 9")
		expect(html).toContain("count is 19")
	})

	it("filters islands by the ?framework= query parameter", async () => {
		const html = await getHtml("?framework=react")
		expect(html).toContain("react-counter")
		expect(html).not.toContain("vue-counter")
		expect(html).not.toContain("svelte-counter")
	})
})
```

Note: the `count is 9` / `count is 19` assertions depend on SSR inlining island content into the `<phoria-island>` element. If SSR output differs, adjust these to the actual rendered markup (e.g. assert on `StartAt` values or the presence of the `react-counter` class three times on the `?framework=react` page).

- [ ] **Step 9: Install the standalone workspace and format moved files**

```bash
pnpm install
pnpm exec biome check --write .
```

Run in `examples/framework-multiple/WebApp` (workdir). Expected: install resolves the registry ranges (WIP caveat applies only to build/run, not install); Biome reformats the moved TS/JSON/CSS/Svelte/Vue files to space-indent + trailing commas per the example config.

- [ ] **Step 10: Verify install + lint**

Run: `pnpm lint` and `pnpm check` in `examples/framework-multiple/WebApp`.
Expected: `lint` passes (formatting is now correct). `check` (`tsc`) may report errors against the published package types — per the Global Constraints WIP note, **type errors against published `@phoria/*` types are expected**; note them and continue. Record `pnpm install` lockfile as the committed lockfile (it is `WebApp/pnpm-lock.yaml`, untracked until `git add`).

- [ ] **Step 11: Commit**

```bash
git add examples/framework-multiple
git commit -m "chore(examples): convert framework-multiple app to a standalone example JS workspace"
```

---

### Task 3: Rewire the WebApp .NET side and fold in the factory samples

**Files:**
- Modify: `examples/framework-multiple/WebApp/WebApp.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Preview.json`, `appsettings.Production.json`, `Properties/launchSettings.json`, `Pages/Index.cshtml`, `Pages/_ViewImports.cshtml`
- Move: `e2e/with-workspace/WebApp/Components/{ReactCounterProps.cs,ReactCounterTagHelper.cs,ReactCounterViewComponent.cs}` → `examples/framework-multiple/WebApp/Components/`

**Depends on:** Task 2 (files in place). **Provides:** the runnable WebApp (registry Phoria package).

- [ ] **Step 1: Move the factory samples in**

```bash
mkdir -p examples/framework-multiple/WebApp/Components
git mv e2e/with-workspace/WebApp/Components/ReactCounterProps.cs examples/framework-multiple/WebApp/Components/ReactCounterProps.cs
git mv e2e/with-workspace/WebApp/Components/ReactCounterTagHelper.cs examples/framework-multiple/WebApp/Components/ReactCounterTagHelper.cs
git mv e2e/with-workspace/WebApp/Components/ReactCounterViewComponent.cs examples/framework-multiple/WebApp/Components/ReactCounterViewComponent.cs
```

(All three files are copied verbatim — they reference only `Phoria.Islands` and `Microsoft.AspNetCore.*`.)

- [ ] **Step 2: Rewrite `WebApp/WebApp.csproj`** (PackageReference; match getting-started's Content include)

`examples/framework-multiple/WebApp/WebApp.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<TargetFramework>net10.0</TargetFramework>
		<AnalysisLevel>latest-recommended</AnalysisLevel>
	</PropertyGroup>

	<ItemGroup>
		<PackageReference Include="Phoria" />
		<PackageReference Include="Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation" />
		<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
		<PackageReference Include="OpenTelemetry.Extensions.Hosting" />
	</ItemGroup>

	<PropertyGroup>
		<DefaultItemExcludes>$(DefaultItemExcludes);package.json</DefaultItemExcludes>
	</PropertyGroup>

	<ItemGroup>
		<Content Include="package.json" CopyToOutputDirectory="Never" />
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Rewrite `WebApp/Program.cs`** (align to getting-started: drop ResponseCompression and the Preview `Server.Process = null` override)

`examples/framework-multiple/WebApp/Program.cs`:
```cs
using OpenTelemetry.Logs;
using Phoria;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(options =>
{
	options.IncludeFormattedMessage = true;
	options.IncludeScopes = true;
	options.ParseStateValues = true;

	if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
	{
		options.AddOtlpExporter();
	}
});

IMvcBuilder mvcBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
	mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddPhoria();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

if (app.Environment.IsDevelopment())
{
	app.UseWebSockets();
}

app.UsePhoria();

app.Run();
```

- [ ] **Step 4: Rewrite the appsettings files**

`examples/framework-multiple/WebApp/appsettings.json`:
```json
{
	"Logging": {
		"LogLevel": {
			"Default": "Information",
			"Microsoft.AspNetCore": "Warning",
			"System.Net.Http.HttpClient.Phoria.Vite.DevServer.DevHttpClient": "Warning"
		}
	},
	"allowedHosts": "*",
	"phoria": {
		"root": "ui",
		"entry": "src/entry-client.ts",
		"ssrEntry": "src/entry-server.ts",
		"server": {
			"port": 5473
		}
	}
}
```

`examples/framework-multiple/WebApp/appsettings.Development.json`:
```json
{
	"detailedErrors": true,
	"phoria": {
		"server": {
			"https": true
		}
	}
}
```

`examples/framework-multiple/WebApp/appsettings.Preview.json`:
```json
{
	"phoria": {
		"server": {
			"https": false
		}
	}
}
```

`examples/framework-multiple/WebApp/appsettings.Production.json`:
```json
{
	"phoria": {
		"server": {
			"process": {
				"command": "node",
				"arguments": ["ui/dist/server/server.js"]
			}
		}
	}
}
```

- [ ] **Step 5: Rewrite `WebApp/Properties/launchSettings.json`** (new ports; both env vars set, matching getting-started)

`examples/framework-multiple/WebApp/Properties/launchSettings.json`:
```json
{
	"$schema": "http://json.schemastore.org/launchsettings.json",
	"profiles": {
		"Development": {
			"commandName": "Project",
			"dotnetRunMessages": true,
			"launchBrowser": true,
			"applicationUrl": "https://localhost:6573;http://localhost:5573",
			"environmentVariables": {
				"ASPNETCORE_ENVIRONMENT": "Development",
				"DOTNET_ENVIRONMENT": "Development"
			}
		}
	}
}
```

- [ ] **Step 6: Update `Pages/_ViewImports.cshtml`** (register the WebApp TagHelpers for the factory demo)

`examples/framework-multiple/WebApp/Pages/_ViewImports.cshtml`:
```cshtml
@using Phoria.Islands
@using WebApp
@namespace WebApp.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, Phoria
@addTagHelper *, WebApp
```

- [ ] **Step 7: Update `Pages/Index.cshtml`** (add the ViewComponent + custom TagHelper usages to the React block)

In `examples/framework-multiple/WebApp/Pages/Index.cshtml`, replace the React block (lines `@if (Model.ShowReactComponent) { ... }`) with:
```cshtml
@if (Model.ShowReactComponent)
{
	<div class="card">
		<phoria-island component="ReactCounter" client="Client.Load" props="new { StartAt = 5 }"></phoria-island>

		<vc:react-counter start-at="9" />

		<react-counter start-at="19" />
	</div>
}
```

The `?framework=` query filter is **already present** in the moved `Index.cshtml.cs` (byte-identical to with-workspace's) — no fold-in needed there.

- [ ] **Step 8: Verify the .NET side builds against the published Phoria package**

Run: `dotnet restore examples/framework-multiple/WebApp/WebApp.csproj` then `dotnet build examples/framework-multiple/WebApp/WebApp.csproj --configuration Release`.
Expected: restore resolves `Phoria` 0.4.2 from nuget.org (per the example's `nuget.config`); the build compiles. If the published package's API differs from the source-era code, record the errors under the WIP caveat — do **not** fix by pointing at the source package.

- [ ] **Step 9: Commit**

```bash
git add examples/framework-multiple
git commit -m "chore(examples): rewire framework-multiple WebApp to the published Phoria package"
```

---

### Task 4: Update the repo workspace, CI, and example tooling

**Files:**
- Modify: `pnpm-workspace.yaml`, `.changeset/config.json`, `turbo.json`, `.github/workflows/ci.yml`, `scripts/examples.js`
- Modify: `pnpm-lock.yaml` (regenerated)

**Depends on:** Tasks 2–3. **Provides:** clean workspace/CI; `examples:check` covers the new framework packages.

- [ ] **Step 1: Update `pnpm-workspace.yaml`** — remove the three e2e globs and `injectWorkspacePackages`

`pnpm-workspace.yaml` becomes:
```yaml
packages:
  - "packages/*"
```
Delete the `injectWorkspacePackages: true` line and its two-line comment (they existed only for the e2e Dockerfiles' `pnpm deploy`). Keep `allowBuilds`, `catalog`, `peerDependencyRules`, `minimumReleaseAgeExclude` unchanged.

- [ ] **Step 2: Update `.changeset/config.json`** — remove the `ignore` array

Replace line 8 `"ignore": ["framework-multiple", "with-sidecar", "with-workspace"],` with nothing (delete the key). All other keys stay.

- [ ] **Step 3: Update `turbo.json`** — remove the dead `build:islands` and `preview` tasks

Delete the `"build:islands": { ... },` block (with its `dependsOn`/`cache`/`outputs`) and the `"preview": { "dependsOn": ["build"], "persistent": true },` block. These existed only for the e2e apps, which no longer participate in the turbo graph.

- [ ] **Step 4: Update `.github/workflows/ci.yml`** — remove the `test-e2e` job

Delete the entire `test-e2e:` job (lines ~101–146), including the Aspire CLI install step. The `build-and-test` and `test-browser` jobs stay. (A future examples e2e job is tracked in `examples/TODO.md`.)

- [ ] **Step 5: Update `scripts/examples.js`** — extend `jsPackages` so link/sync/check/bump cover the framework packages

In `scripts/examples.js`, change the `jsPackages` map from:
```js
const jsPackages = {
	"@phoria/phoria": "packages/phoria-islands",
	"@phoria/phoria-react": "packages/phoria-react",
	"@phoria/vite-plugin-dotnet-dev-certs": "packages/vite-plugin-dotnet-dev-certs"
}
```
to:
```js
const jsPackages = {
	"@phoria/phoria": "packages/phoria-islands",
	"@phoria/phoria-react": "packages/phoria-react",
	"@phoria/phoria-svelte": "packages/phoria-svelte",
	"@phoria/phoria-vue": "packages/phoria-vue",
	"@phoria/vite-plugin-dotnet-dev-certs": "packages/vite-plugin-dotnet-dev-certs"
}
```
This is required so `check` rejects `link:`/`file:` refs on `phoria-svelte`/`phoria-vue` in the new example, and so `link`/`sync`/`bump` rewrite them.

- [ ] **Step 6: Regenerate the root lockfile and verify the tooling**

Run: `pnpm install` at the repo root.
Expected: workspace packages in `pnpm-lock.yaml` shrink to `packages/*` only (the e2e entries at lines 121/206/270 are gone). Then run `node scripts/examples.js check` — it should still list `examples/getting-started` (currently failing on its linked state; that is resolved in Task 6).

- [ ] **Step 7: Commit**

```bash
git add pnpm-workspace.yaml .changeset/config.json turbo.json .github/workflows/ci.yml scripts/examples.js pnpm-lock.yaml
git commit -m "chore: remove e2e apps from workspace, CI, and example tooling"
```

---

### Task 5: Delete the `e2e/` directory

**Files:**
- Delete: `e2e/` (framework-multiple leftovers: `Phoria.AppHost/`, `aspire.config.json`, `azure.yaml`, `FrameworkMultiple.sln`, `infra/`, `package.json`, `tests/`, `WebApp/Dockerfile`, `node_modules/`, etc.; `with-sidecar/`; `with-workspace/`)

**Depends on:** Tasks 1–3 (content moved). **Provides:** removal of the e2e apps.

- [ ] **Step 1: Remove the tracked e2e files**

```bash
git rm -r e2e
```

- [ ] **Step 2: Remove the untracked build artifacts** (the repo `.gitignore` covers `node_modules/`, `.turbo/`, `bin/`, `obj/`, `dist/`, `.vite-config/`)

```bash
rm -rf e2e
```

- [ ] **Step 3: Verify nothing references the e2e paths**

```bash
rg -n "e2e/framework-multiple|e2e/with-sidecar|e2e/with-workspace|framework-multiple|with-sidecar|with-workspace" --glob '!node_modules/**' --glob '!.git/**' --glob '!docs/**' --glob '!pnpm-lock.yaml'
```
Expected: matches only inside `examples/framework-multiple/` (its package name and paths) and historical `docs/plans`/`docs/superpowers`/`docs/MEMORY.md` (left untouched by policy). Update anything else that surfaces.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "chore: remove e2e apps"
```

---

### Task 6: Bring `examples/getting-started` to registry state

**Files:**
- Modify: `examples/getting-started/WebApp/package.json`, `WebApp/WebApp.csproj`, `Directory.Packages.props`, `WebApp/pnpm-lock.yaml` (regenerated)
- Modify: `examples/getting-started/GettingStarted.slnx`

**Depends on:** Task 4 Step 5 (jsPackages). **Provides:** all examples pass `examples:check`.

- [ ] **Step 1: Run the bump tool**

Run: `pnpm examples:bump` at the repo root.
Expected: `scripts/examples.js bump` rewrites `examples/getting-started/WebApp/package.json` phoria refs to registry ranges (`@phoria/phoria` `^0.4.2`, `@phoria/phoria-react` `^0.4.2`, `@phoria/vite-plugin-dotnet-dev-certs` `^0.2.1`), converts the csproj `ProjectReference` to `<PackageReference Include="Phoria" />`, adds `<PackageVersion Include="Phoria" Version="0.4.2" />` to `Directory.Packages.props`, and runs `pnpm install` to regenerate the example lockfile. It does **not** touch the `.slnx`.

- [ ] **Step 2: Fix `GettingStarted.slnx`** — drop the source-project reference

`examples/getting-started/GettingStarted.slnx` becomes:
```xml
<Solution>
  <Project Path="AppHost/AppHost.csproj" />
  <Project Path="WebApp/WebApp.csproj" />
</Solution>
```

- [ ] **Step 3: Verify all examples pass `examples:check`**

Run: `pnpm examples:check`.
Expected: both `examples/getting-started` and `examples/framework-multiple` print `OK` (registry refs, PackageReference, numeric Phoria PackageVersion, no `link:`/`file:`).

- [ ] **Step 4: Commit**

```bash
git add examples/getting-started
git commit -m "chore(examples): restore getting-started to committed registry state"
```

---

### Task 7: Update live docs

**Files:**
- Modify: `AGENTS.md`, `docs/PROJECT.md`, `docs/ARCHITECTURE.md`, `TODO.md`, `examples/TODO.md`

**Depends on:** all previous tasks. **Provides:** docs that match the new repo layout.

- [ ] **Step 1: Update `AGENTS.md`**

- **Repository Structure**: replace the `e2e/` block (lines 24–27) with the examples listing, e.g.:
  ```
  examples/
    getting-started/      Single-framework example (React)
    framework-multiple/   Multi-framework example (React + Svelte + Vue)
  ```
- **Commands**: replace the "E2E test (requires a preview build running)" block (lines ~78–81) with example-test guidance: the example's `test:e2e` runs from `examples/<name>/WebApp` after `pnpm examples:link` + build (see the Examples section).
- **Gotchas**:
  - Line ~111 comment example: replace "e.g. the Preview `Server.Process = null` override in the e2e apps" with an example that still exists (e.g. "the `process.cwd()` constraint comment in the AppHost `Program.cs`").
  - Line ~117: reword "not the e2e apps" → "not the example apps" (keep the build-via-csproj/package.json guidance).
- **Replace the entire `## E2E Apps` section (lines ~135–152)** with example guidance: examples are standalone user-facing apps outside the pnpm workspace; each ships `WebApp/pnpm-workspace.yaml` + committed lockfile; `pnpm examples:link` for local dev, `pnpm examples:sync`/`examples:bump` for the committed registry state; `aspire run`/`start`/`stop` resolve the example-root `aspire.config.json` from the `WebApp/` script cwd; the AppHost owns Node via `AddJavaScriptApp` in dev (`dev:server`) and preview (`preview:server`), while Production is WebApp-owned (`appsettings.Production.json` `Phoria:Server:Process`).
- **Examples section (lines ~155–161)**: fold the framework-multiple specifics in; keep the "committed examples reference published packages" + `examples:check` enforcement text.

- [ ] **Step 2: Update `docs/PROJECT.md`**

- Line 36: `Establish a **test foundation** (unit + e2e)` → `(unit + integration)`.
- Line 49: `Playwright e2e tests exist` → `Playwright browser tests exist`; drop the "e2e" wording.
- Line 66: `Playwright (e2e apps)` → `Playwright (browser tests)`.
- Lines 72–73 and 131–132: reword "hardened e2e shutdown paths (Node/Vite sidecar signal handling, `run-p` replacement...)" to past-tense/generic ("hardened shutdown paths for the Node/Vite sidecar..."), since the e2e apps no longer exist.
- Lines 181–183: reword the "E2E AppHosts currently use an OTLP HTTP exporter..." open question to generic "Aspire AppHosts in the examples use an OTLP HTTP exporter...".
- Leave completed-phase narrative intact otherwise.

- [ ] **Step 3: Update `docs/ARCHITECTURE.md`**

- Line 28: reword "development/preview e2e apps; the sidecar app instead configures `PhoriaServerProcess` to own Node from the .NET host." → describe the current model generically: "development/preview examples own Node via the Aspire AppHost (`AddJavaScriptApp`); in Production the .NET host owns Node via `Phoria:Server:Process`." Read the surrounding paragraph first to match its flow.

- [ ] **Step 4: Update `TODO.md`**

Remove or retarget the e2e-specific entries:
- Delete the "`aspire run` / `aspire start` issues" review section (the three e2e apps no longer exist).
- Delete the error-trace blocks referencing `e2e/framework-multiple` paths (lines ~104–133, ~145–177) — the Svelte `state_referenced_locally` warning and the broken-logo note still apply, retarget them to `examples/framework-multiple/WebApp/ui/src/components/Counter/Counter.svelte` and the example's `/img/phoria.svg`.
- Line 4 "Add Playwright to e2e apps" — reword to "Add Playwright to example e2e suites" or drop (the examples run Vitest e2e today).
- Line 139 "I need to test the various modes of the e2e apps" → retarget to the example's modes (Dev Aspire / Dev non-Aspire / Preview Aspire / Production Docker).

- [ ] **Step 5: Update `examples/TODO.md`**

- Mark the first Deferred item (convert `e2e/...` to standalone examples) as done.
- Update the second Deferred item ("Add examples to CI") to note: the e2e CI job was removed in this change; a future job should link (`pnpm examples:link`) + run each example's `test:e2e`.
- Add a Deferred item: "Bring back Azure deployment scaffolding (`azure.yaml` + `infra/`) and a standalone `WebApp/Dockerfile` for `framework-multiple`."

- [ ] **Step 6: Commit**

```bash
git add AGENTS.md docs/PROJECT.md docs/ARCHITECTURE.md TODO.md examples/TODO.md
git commit -m "docs: remove e2e app references"
```

---

### Task 8: Full verification

**Files:** none (verification only).

**Depends on:** all tasks.

- [ ] **Step 1: Root build/lint/check/test suite**

Run from the repo root:
```bash
pnpm build
pnpm lint
pnpm check
pnpm test
pnpm test:browser
dotnet test --solution Phoria.sln --configuration Release
```
Expected: all green. `build`/`lint`/`check`/`test`/`test:browser` no longer include any e2e apps (turbo graph is packages-only); `dotnet test` is unchanged (the solution never included the e2e apps).

- [ ] **Step 2: Examples structural checks**

Run: `pnpm examples:check` — both examples must report `OK`.

- [ ] **Step 3: Linked-state smoke test of the new example** (authoritative build/run, per the WIP caveat)

```bash
pnpm examples:link
pnpm install --frozen-lockfile   # after link, from examples/framework-multiple/WebApp
pnpm lint
pnpm check
pnpm build
```

Run in `examples/framework-multiple/WebApp`. Expected: the example builds against the freshly built in-repo packages (link refs) — this is the real verification that the converted app works (islands + webapp + AppHost). If `dotnet restore`/build need the source Phoria project, `examples:link` already swapped the csproj to a `ProjectReference`.

Optionally, with Aspire CLI available: `pnpm preview` then `pnpm test:e2e` from `examples/framework-multiple/WebApp`, then `pnpm stop`.

- [ ] **Step 4: Restore the committed registry state**

Run: `pnpm examples:sync` — restores `package.json` specs, the csproj ref, and the committed lockfiles from HEAD. Then `git status` must show no example diffs.

- [ ] **Step 5: Final review gate**

Run `git status`, review `git diff HEAD~1 --stat` (or the full branch diff), and confirm:
- `e2e/` is gone; `examples/framework-multiple/` is complete.
- No live doc references the e2e apps.
- Both examples pass `examples:check`.
- Root CI/workflow files contain no e2e job.
