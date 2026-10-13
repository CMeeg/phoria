# E2E App Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Standardise the three e2e apps' scripts and Vitest configs, split the e2e smoke test out of the CI browser job into its own job across all three apps, and align each app's Node-process ownership model with how it is actually exercised.

**Architecture:** `with-sidecar` keeps the .NET-owned model for Preview/Production but becomes developer-owned in development (WebApp monitor-only). `framework-multiple` and `with-workspace` AppHosts always use `AddJavaScriptApp` — `dev:server` in Development, a new `preview:server` script otherwise — so the server command/arguments no longer live in `appsettings.Preview.json`. The obsolete `appsettings.root = "."` production override is removed from the FM/WW `server.ts`, which makes root resolution follow the spawn directory (and fixes Docker production asset resolution). `Cwd` is **not** added to `ProcessOptions` (the proposed option has no consumer once sidecar dev is developer-owned).

**Tech Stack:** TypeScript, Vite 8, Aspire 13.4.6 AppHosts, Vitest, GitHub Actions, .NET 10 WebApps.

## Global Constraints

- No changesets: no published-package changes (e2e apps are private; `Cwd` is dropped).
- Branch mechanics done by the user (feature branch → PR → `develop` → PR → `main`).
- CI order (verify at end): `pnpm build` → `pnpm lint` → `pnpm check` → `pnpm test` → `dotnet test` → `test-browser` → `test-e2e`.
- The Vite dev server resolves `root`/`cwd` from `process.cwd()` and only discovers `vite.config.ts` in the current working directory — every script must run from the directory that owns the config.
- Biome 2 formats `package.json` with `expand: always`; run `pnpm biome check --write` on edited `package.json` files to match repo style.
- `aspire stop --all` resolves through the DCP backchannel (never the config file); pass no `--apphost`.
- Each AppHost already ships a `Properties/launchSettings.json` `Development` profile — do not touch them.
- FM/WW `Program.cs` keep the Preview `options.Server.Process = null` override (AppHost owns Node in Preview); with-sidecar `Program.cs` stays `AddPhoria()` with no override.

---

### Task 1: Restructure with-sidecar (developer-owned Node in development)

**Files:**
- Move: `e2e/with-sidecar/WebApp/vite.config.ts` → `e2e/with-sidecar/vite.config.ts`
- Modify: `e2e/with-sidecar/WebApp/appsettings.json`, `WebApp/appsettings.Development.json`, `e2e/with-sidecar/package.json`, `e2e/with-sidecar/vitest.smoke.config.ts`, `e2e/with-sidecar/tests/smoke.test.ts`
- Create: `e2e/with-sidecar/WebApp/appsettings.Production.json`, `e2e/with-sidecar/vitest.e2e.config.ts` (renamed), `e2e/with-sidecar/tests/e2e/smoke.test.ts` (renamed), `e2e/with-sidecar/tsconfig.node.json` (optional, editor parity)

**Interfaces:**
- Produces: with-sidecar config lives at the app root; the WebApp spawns Node only in Preview/Production (from the content root, `ui/dist/server/server.js`); in development the developer starts `pnpm dev:server`.

- [ ] **Step 1: Move the Vite config to the app root**

`git mv e2e/with-sidecar/WebApp/vite.config.ts e2e/with-sidecar/vite.config.ts`, then change the phoria plugin to pass the WebApp directory explicitly (it is now one level up from the plugin's default `cwd`):

```typescript
plugins: [dotenvDevCerts(), phoria({ cwd: "WebApp" }), phoriaReact(), inspectConfig()]
```

No other config changes: with-sidecar source uses no `~/*` alias, so `resolve.tsconfigPaths` is not needed (FM's config keeps it; it is not required here).

- [ ] **Step 2: Add `tsconfig.node.json` (optional, editor parity)**

Create `e2e/with-sidecar/tsconfig.node.json` mirroring `e2e/framework-multiple/tsconfig.node.json` with `"include": ["vite.config.ts", "WebApp/ui/src/server.ts"]`. Not wired into any script (the app `check` script runs `tsc` against `tsconfig.json` only).

- [ ] **Step 3: Consolidate the UI entry files**

`git mv e2e/with-sidecar/WebApp/ui/src/entry-client.tsx e2e/with-sidecar/WebApp/ui/src/entry-client.ts`

Inline `entry-server.tsx` into `entry-server.ts` (matches FM/WW, which have a single `entry-server.ts`):

```typescript
import "@phoria/phoria-react/server"
import "./components/register"
import type { PhoriaIsland } from "@phoria/phoria/server"

async function renderPhoriaIsland(island: PhoriaIsland) {
	return await island.render()
}

export { renderPhoriaIsland }
```

`git rm e2e/with-sidecar/WebApp/ui/src/entry-server.tsx`

Update `e2e/with-sidecar/WebApp/appsettings.json` `phoria.entry` → `"src/entry-client.ts"`.

- [ ] **Step 4: Split the process-ownership config across environments**

`e2e/with-sidecar/WebApp/appsettings.Development.json` — remove the `process` section (developer-owned Node; `PhoriaServerProcess` no-ops on a null `Server.Process`):

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

`appsettings.Preview.json` — unchanged (already spawns `node` with `["ui/dist/server/server.js"]` from the content root).

Create `appsettings.Production.json` (parallels the FM/WW production files):

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

- [ ] **Step 5: Standardise the package.json scripts**

Replace the scripts in `e2e/with-sidecar/package.json` with:

```json
"scripts": {
	"build": "concurrently \"pnpm:build:*\"",
	"build:islands": "vite build --app",
	"build:webapp": "dotnet build WebApp/WebApp.csproj --configuration Release",
	"check": "tsc",
	"dev": "aspire run",
	"dev:server": "tsx ./WebApp/ui/src/server.ts",
	"lint": "biome check",
	"preview": "aspire start --environment Preview",
	"stop": "aspire stop --all",
	"test:e2e": "vitest run --config vitest.e2e.config.ts"
}
```

Drops the `cd WebApp &&` prefixes (config now lives at the app root), merges `dev:aspire` into `dev`, renames `test:smoke` → `test:e2e`, and removes all `--apphost` flags.

- [ ] **Step 6: Rename the smoke test to an e2e test**

`git mv e2e/with-sidecar/vitest.smoke.config.ts e2e/with-sidecar/vitest.e2e.config.ts` — change `include` to `["tests/e2e/**/*.test.ts"]`.

`git mv e2e/with-sidecar/tests/smoke.test.ts e2e/with-sidecar/tests/e2e/smoke.test.ts` — rename the `describe` to `"with-sidecar e2e"`; assertions unchanged.

- [ ] **Step 7: Verify**

Run:

```bash
pnpm --filter with-sidecar build
pnpm --filter with-sidecar lint
pnpm --filter with-sidecar check
pnpm --filter with-sidecar preview
NODE_TLS_REJECT_UNAUTHORIZED=0 pnpm --filter with-sidecar test:e2e
pnpm --filter with-sidecar stop
```

Expected: build/lint/check clean; the e2e test passes against Preview — and now genuinely exercises SSR, because the WebApp spawns Node via `appsettings.Preview.json`. Manual dev spot-check (optional, not in CI): `pnpm --filter with-sidecar dev` (Aspire dashboard + WebApp) in one terminal, `pnpm --filter with-sidecar dev:server` in another; home page renders islands with HMR.

**Commit:** `chore(e2e): make with-sidecar node server developer-owned in development`

---

### Task 2: Standardise framework-multiple scripts + vitest rename

**Files:**
- Modify: `e2e/framework-multiple/package.json`, `e2e/framework-multiple/vitest.smoke.config.ts`, `e2e/framework-multiple/tests/smoke.test.ts`

**Interfaces:**
- Produces: FM scripts match the Task 1 shape; the smoke test is an e2e test under `tests/e2e/`.

- [ ] **Step 1: Standardise the package.json scripts**

Replace the scripts in `e2e/framework-multiple/package.json` with:

```json
"scripts": {
	"build": "concurrently \"pnpm:build:*\"",
	"build:islands": "vite build --app",
	"build:webapp": "dotnet build --configuration Release",
	"check": "tsc",
	"dev": "aspire run",
	"dev:server": "tsx ./WebApp/ui/src/server.ts",
	"lint": "biome check",
	"preview": "aspire start --environment Preview",
	"stop": "aspire stop --all",
	"test:e2e": "vitest run --config vitest.e2e.config.ts"
}
```

Merges `dev:aspire` into `dev` (the current `dev` script becomes `dev:server`), renames `test:smoke`, drops `--apphost`.

- [ ] **Step 2: Rename the smoke test to an e2e test**

`git mv vitest.smoke.config.ts vitest.e2e.config.ts` — `include` → `["tests/e2e/**/*.test.ts"]`.

`git mv tests/smoke.test.ts tests/e2e/smoke.test.ts` — rename the `describe` to `"framework-multiple e2e"`; assertions unchanged.

- [ ] **Step 3: Verify**

```bash
pnpm --filter framework-multiple build
pnpm --filter framework-multiple lint
pnpm --filter framework-multiple check
pnpm --filter framework-multiple preview
NODE_TLS_REJECT_UNAUTHORIZED=0 pnpm --filter framework-multiple test:e2e
pnpm --filter framework-multiple stop
```

Expected: clean build/lint/check; e2e passes against Preview (unchanged behaviour — FM Preview is still AppHost-owned Node).

**Commit:** `chore(e2e): standardise framework-multiple scripts`

---

### Task 3: Add with-workspace scripts + Vitest e2e infra

**Files:**
- Modify: `e2e/with-workspace/WebApp/package.json`
- Create: `e2e/with-workspace/WebApp/vitest.e2e.config.ts`, `e2e/with-workspace/WebApp/tests/e2e/smoke.test.ts`

**Interfaces:**
- Produces: WW gains `dev`/`dev:server`/`preview`/`stop`/`test:e2e` scripts (matching FM) and its own e2e test, closing the coverage gap.

- [ ] **Step 1: Standardise the package.json scripts**

WW's package.json lives at `e2e/with-workspace/WebApp/` (the AppHost is one level up). Replace the scripts with:

```json
"scripts": {
	"build": "concurrently \"pnpm:build:*\"",
	"build:islands": "vite build --app",
	"build:webapp": "dotnet build --configuration Release",
	"check": "tsc",
	"dev": "aspire run",
	"dev:server": "tsx ./ui/src/server.ts",
	"lint": "biome check",
	"preview": "aspire start --environment Preview",
	"stop": "aspire stop --all",
	"test:e2e": "vitest run --config vitest.e2e.config.ts"
}
```

Merges `dev:aspire` into `dev` (current `dev` → `dev:server`), drops `--apphost`, adds `test:e2e`. Do **not** move `package.json` or the committed `aspire.config.json` out of `WebApp/`.

- [ ] **Step 2: Add the Vitest devDependency**

Add `"vitest": "catalog:"` to `devDependencies` in `e2e/with-workspace/WebApp/package.json`, then run `pnpm install` at the repo root.

- [ ] **Step 3: Add the e2e config and test**

Create `e2e/with-workspace/WebApp/vitest.e2e.config.ts` (identical to FM's):

```typescript
import { defineConfig } from "vitest/config"

export default defineConfig({
	test: {
		environment: "node",
		include: ["tests/e2e/**/*.test.ts"],
		testTimeout: 30_000,
		hookTimeout: 60_000,
		env: {
			// The Aspire-hosted Web App redirects HTTP to its HTTPS endpoint using a dev certificate.
			NODE_TLS_REJECT_UNAUTHORIZED: "0"
		}
	}
})
```

Create `e2e/with-workspace/WebApp/tests/e2e/smoke.test.ts` (FM assertions, `describe` = `"with-workspace e2e"`, webAppUrl default `http://localhost:5247`).

- [ ] **Step 4: Verify**

```bash
pnpm --filter with-workspace build
pnpm --filter with-workspace lint
pnpm --filter with-workspace check
```

Expected: clean. (The e2e run is verified in Task 4, which changes how WW's Node process is started.)

**Commit:** `chore(e2e): add vitest e2e infra to with-workspace`

---

### Task 4: AppHosts always use AddJavaScriptApp (framework-multiple + with-workspace)

**Files:**
- Modify: `e2e/framework-multiple/package.json`, `e2e/with-workspace/WebApp/package.json`
- Modify: `e2e/framework-multiple/Phoria.AppHost/Program.cs`, `e2e/with-workspace/Phoria.AppHost/Program.cs`
- Modify: `e2e/framework-multiple/WebApp/appsettings.Preview.json`, `e2e/with-workspace/WebApp/appsettings.Preview.json`
- Modify: `e2e/framework-multiple/WebApp/ui/src/server.ts`, `e2e/with-workspace/WebApp/ui/src/server.ts`

**Interfaces:**
- Produces: both AppHosts start the Node process via `AddJavaScriptApp` in every environment — `dev:server` in Development, `preview:server` in Preview — so the server command/arguments disappear from `appsettings.Preview.json`, and root resolution follows the script's working directory.

- [ ] **Step 1: Add the `preview:server` scripts**

FM (`e2e/framework-multiple/package.json`, runs from the workspace root where `vite.config.ts` lives):

```json
"preview:server": "node WebApp/ui/dist/server/server.js"
```

WW (`e2e/with-workspace/WebApp/package.json`, runs from the WebApp directory):

```json
"preview:server": "node ./ui/dist/server/server.js"
```

- [ ] **Step 2: Rewrite the framework-multiple AppHost**

Replace the dev/non-dev `nodeCommand`/`nodeArguments`/`nodeWorkingDirectory` branching and the `AddExecutable` call in `e2e/framework-multiple/Phoria.AppHost/Program.cs` (lines 17-45) with:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var webAppDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "WebApp"));
var workspaceRoot = Path.GetFullPath(Path.Combine(webAppDirectory, ".."));
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

var webAppConfiguration = new ConfigurationBuilder()
	.SetBasePath(webAppDirectory)
	.AddJsonFile("appsettings.json", optional: false)
	.AddJsonFile($"appsettings.{environment}.json", optional: true)
	.Build();

var port = int.TryParse(webAppConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : 5173;

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: 5247, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

// The Vite dev server resolves the root from `process.cwd()` and only discovers the vite config in the current
// directory, so both scripts must run from the workspace root where `vite.config.ts` lives.
builder.AddJavaScriptApp("phoria-server", workspaceRoot)
	.WithRunScript(isDevelopment ? "dev:server" : "preview:server")
	.WithPnpm(install: false)
	.WithHttpEndpoint(port: port, name: "http", isProxied: false)
	.WithEnvironment("NODE_ENV", isDevelopment ? "development" : "production")
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.Build().Run();
```

Keep the `webAppConfiguration` read — it supplies the Node port.

- [ ] **Step 3: Rewrite the with-workspace AppHost**

Same replacement in `e2e/with-workspace/Phoria.AppHost/Program.cs`, with two differences: there is no `workspaceRoot` (the WebApp directory is the script directory), and the comment reads "…from the WebApp directory where `vite.config.ts` lives". Use `AddJavaScriptApp("phoria-server", webAppDirectory)`.

- [ ] **Step 4: Simplify the Preview appsettings files**

Both `e2e/framework-multiple/WebApp/appsettings.Preview.json` and `e2e/with-workspace/WebApp/appsettings.Preview.json` become:

```json
{
	"detailedErrors": true,
	"phoria": {
		"server": {
			"port": 5173
		}
	}
}
```

(The `process` section moves out of .NET config and into the AppHost `preview:server` script.)

- [ ] **Step 5: Remove the production root override**

In `e2e/framework-multiple/WebApp/ui/src/server.ts` (lines 61-63) and `e2e/with-workspace/WebApp/ui/src/server.ts` (lines 56-58), delete:

```typescript
if (isProduction) {
	appsettings.root = "."
}
```

Why: the override assumed the Node process runs from the `ui` directory. With `preview:server`, FM runs from the workspace root and resolves `root: "WebApp/ui"`; WW runs from the WebApp directory and resolves `root: "ui"`. Side effect: Docker production (FM spawned by `PhoriaServerProcess` from `/app`) now resolves `/app/WebApp/ui/dist/...` instead of the non-existent `/app/dist/...`. with-sidecar `server.ts` already has no override and is untouched.

- [ ] **Step 6: Verify**

```bash
pnpm --filter framework-multiple build
pnpm --filter framework-multiple preview
NODE_TLS_REJECT_UNAUTHORIZED=0 pnpm --filter framework-multiple test:e2e
pnpm --filter framework-multiple stop
pnpm --filter with-workspace build
pnpm --filter with-workspace preview
NODE_TLS_REJECT_UNAUTHORIZED=0 pnpm --filter with-workspace test:e2e
pnpm --filter with-workspace stop
```

Expected: both e2e suites pass against Preview with Node started by `AddJavaScriptApp` + `preview:server`. Dev spot-check (optional): `pnpm --filter framework-multiple dev` — Vite dev server + HMR via `dev:server`.

**Commit:** `chore(e2e): use AddJavaScriptApp for AppHost node processes`

---

### Task 5: Align with-sidecar server logging

**Files:**
- Modify: `e2e/with-sidecar/WebApp/ui/src/server.ts`

**Interfaces:**
- Produces: with-sidecar's server logging matches FM/WW (precomputed OTLP flag, `error.cause` attribute).

- [ ] **Step 1: Precompute the OTLP flag**

Replace the inline `process.env.OTEL_EXPORTER_OTLP_ENDPOINT ?` ternary (lines 17-23) with:

```typescript
const hasOtlpEndpoint = Boolean(process.env.OTEL_EXPORTER_OTLP_ENDPOINT)
const loggerProvider = new LoggerProvider({
	processors: [
		new SimpleLogRecordProcessor({
			exporter: hasOtlpEndpoint ? new OTLPLogExporter() : new ConsoleLogRecordExporter()
		})
	]
})
```

- [ ] **Step 2: Emit `error.cause`**

Add `"error.cause": err.cause === undefined ? undefined : String(err.cause)` to the `server.error` log attributes (line 71), matching FM/WW.

- [ ] **Step 3: Verify**

```bash
pnpm --filter with-sidecar lint
pnpm --filter with-sidecar check
```

Expected: clean.

**Commit:** `chore(e2e): align with-sidecar server logging`

---

### Task 6: Split e2e tests into their own CI job

**Files:**
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: the `test-browser` job covers only browser-mode component tests; a new `test-e2e` job (gated on both `build-and-test` and `test-browser`) builds and previews all three apps and runs their e2e suites.

- [ ] **Step 1: Trim the `test-browser` job**

Rename `test-browser` to `Browser tests`. Delete the `Build e2e app`, `Install Aspire CLI`, and `Start preview and run smoke test` steps (lines 98-110).

- [ ] **Step 2: Add the `test-e2e` job**

Add after `test-browser`:

```yaml
  test-e2e:
    name: E2E tests
    runs-on: ubuntu-latest
    timeout-minutes: 30
    needs: [build-and-test, test-browser]
    steps:
      - name: Checkout
        uses: actions/checkout@v7

      - name: Setup pnpm
        uses: pnpm/action-setup@v6

      - name: Setup Node
        uses: actions/setup-node@v7
        with:
          node-version-file: ".nvmrc"
          cache: "pnpm"

      - name: Setup dotnet
        uses: actions/setup-dotnet@v6
        with:
          dotnet-version: |
            8.0.x
          global-json-file: "./global.json"

      - name: Install dependencies
        run: pnpm install

      - name: Build packages
        run: pnpm build

      - name: Install Aspire CLI
        run: |
          curl -fsSL https://aspire.dev/install.sh | bash -s -- --version "13.4.6" --skip-path
          echo "$HOME/.aspire/bin" >> "$GITHUB_PATH"

      - name: Run e2e tests
        run: |
          for app in framework-multiple with-sidecar with-workspace; do
            pnpm --filter "$app" build
            pnpm --filter "$app" preview
            NODE_TLS_REJECT_UNAUTHORIZED=0 pnpm --filter "$app" test:e2e
            pnpm --filter "$app" stop || true
          done
```

- [ ] **Step 3: Sanity-check the workflow YAML**

Run: `python3 -c "import yaml,sys; yaml.safe_load(open('.github/workflows/ci.yml'))"`
Expected: no output, exit 0.

**Commit:** `ci: split e2e tests into their own job`

---

### Task 7: Update docs

**Files:**
- Modify: `AGENTS.md`, `docs/guides/getting-started.md`, `docs/MEMORY.md`, `.changeset/config.json`

**Interfaces:**
- Produces: docs describe the standardised scripts, the corrected ownership models, and the e2e-ignoring changeset config.

- [ ] **Step 1: Update `AGENTS.md`**

- Line 78 heading `E2E smoke test` → `E2E test`; line 81 `pnpm --filter framework-multiple test:smoke` → `test:e2e`.
- E2E Apps section (lines 134-154): update the script list (`build`, `dev` = `aspire run`, `dev:server` = tsx server, `preview`, `stop` = `aspire stop --all`, `test:e2e`, `lint`/`check`; remove `dev:aspire`).
- Rewrite the spawn-directory bullets: FM/WW Node is owned by the AppHost via `AddJavaScriptApp` — `dev:server`/`preview:server` from the workspace root (FM) or WebApp directory (WW); with-sidecar in development is developer-owned (`pnpm dev:server` from the app root, WebApp monitor-only with no `Phoria:Server:Process`), while Preview/Production the WebApp spawns `node ui/dist/server/server.js` from the content root.
- Remove the `appsettings.root = "."` bullet and the `--apphost` paragraph (scripts now resolve via the committed `aspire.config.json`, and `aspire stop` uses `--all`).

- [ ] **Step 2: Update `docs/guides/getting-started.md`**

- Line 564: `pnpm dev:aspire` → `pnpm dev`.
- Lines 573 and 582: two-terminal workflow `pnpm dev` → `pnpm dev:server` (both the "Terminal 1" command and the debugger note's reference).
- Lines 596-601: the preview paragraph — the AppHost adds the compiled server with `AddJavaScriptApp` and `WithRunScript("preview:server")` instead of `AddExecutable`; the server command/arguments no longer live in `appsettings.Preview.json`.

- [ ] **Step 3: Add a `docs/MEMORY.md` entry**

Append `## 2026-08-04 — E2E app polish` covering: with-sidecar dev Node is developer-owned (WebApp monitor-only), Preview/Production WebApp-owned; AppHosts always use `AddJavaScriptApp` via `dev:server`/`preview:server`; the `Cwd` option was dropped as unused; removing the production `root = "."` override fixes Docker production asset resolution; `--apphost` dropped in favour of `aspire.config.json` + `aspire stop --all`; smoke → e2e rename; CI split into a `test-e2e` job covering all three apps.

- [ ] **Step 4: Update `.changeset/config.json`**

`ignore` → `["framework-multiple", "with-sidecar", "with-workspace"]`.

- [ ] **Step 5: Verify**

Run: `pnpm biome check` and `pnpm check` (AGENTS.md/docs are prose; no functional verification).

**Commit:** `docs: document e2e script changes`

---

## Open Questions / Risks

- **`WithPnpm(install: false)`**: if `AddJavaScriptApp` rejects `install: false` at runtime (unlikely per Aspire docs), fall back to `WithPnpm()` — the workspace already has `node_modules`.
- **`preview:server` root resolution** is the load-bearing assumption of Task 4; the Task 4 Preview verification is the proof point.
- **`aspire stop --all`** in the CI loop: `|| true` keeps a teardown failure from masking a passing suite; per AGENTS.md, Aspire 13.4.6 may leave DCP resources running after non-interactive SIGINT, which is exactly why `stop` is explicit.
