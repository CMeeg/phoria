# Aspire Health Check and Observability Noise Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make both example Aspire flows scheme-correct and remove repetitive `/hc` and OTLP exporter HTTP client diagnostics from normal logs without disabling telemetry.

**Architecture:** The AppHosts will select the `phoria-server` endpoint scheme from the merged `Phoria:Server:Https` setting. Phoria will isolate monitor health checks in a dedicated named `HttpClient` whose logging threshold follows `LogHealthChecks`; the examples will set the three OTLP exporter client categories to `Warning` by default, allowing users to opt into their Information-level request diagnostics with `Trace`. The Node observability guard will be shared between instrumentation registration and the preloaded-core-module patch.

**Tech Stack:** C#/.NET 8 and 10, ASP.NET Core logging, `IHttpClientFactory`, .NET Aspire 13.4, OpenTelemetry .NET 1.17.0, Node.js/TypeScript, Vitest, Biome, pnpm workspaces.

## Global Constraints

- Preserve Phoria's public observability API and the existing `IPhoriaServerHttpClientFactory` contract.
- Do not disable .NET HTTP client tracing or metrics instrumentation.
- Do not disable or alter OTLP exporters.
- Do not globally suppress all `System.Net.Http.HttpClient` logs.
- Default OTLP exporter diagnostic categories to `Warning`; users opt in to Information-level exporter request messages by setting the category to `Trace`.
- `/hc` diagnostic logging follows `PhoriaObservabilityOptions.LogHealthChecks`; Warning and Error messages remain available regardless.
- Apply AppHost and exporter logging changes to both `examples/framework-multiple` and `examples/getting-started`.
- Follow repository formatting: C# tabs, TypeScript tabs, no unnecessary comments, and Biome style for TypeScript.
- No new dependencies.
- Do not commit unrelated pre-existing working-tree changes.

---

### Task 1: Share the Node HTTP instrumentation guard

**Files:**
- Modify: `packages/phoria-opentelemetry/src/observability.ts`
- Test: `packages/phoria-opentelemetry/src/observability.instrumentation.test.ts`

**Interfaces:**
- Consumes: existing `tracingEnabled`, `metricsEnabled`, `HttpInstrumentation`, and post-`sdk.start()` `process.getBuiltinModule` patch.
- Produces: no public API change; one local `httpInstrumentationEnabled` boolean used by both internal branches.

- [ ] **Step 1: Write or update the focused regression test**

Keep the existing test that statically imports `node:http` and `node:https`, starts metrics observability, and asserts both `Server.prototype.emit` functions change. Do not add exporter mocks or change the public settings shape; the test is specifically for the preloaded-core-module patch.

- [ ] **Step 2: Run the focused test before implementation**

Run: `pnpm --filter @phoria/opentelemetry exec vitest run src/observability.instrumentation.test.ts`

Expected: the current test passes on the existing fix, establishing the behavior that the guard refactor must preserve.

- [ ] **Step 3: Replace the duplicated condition with one local constant**

In `createPhoriaObservability`, after reading the three signal flags, define:

```ts
const httpInstrumentationEnabled = tracingEnabled || metricsEnabled
```

Use `httpInstrumentationEnabled` for both:

```ts
if (httpInstrumentationEnabled) {
	config.instrumentations = [
		new HttpInstrumentation({
			ignoreIncomingRequestHook: (request) => request.url === "/hc"
		})
	]
}
```

and the existing post-start patch block:

```ts
if (httpInstrumentationEnabled) {
	process.getBuiltinModule("http")
	process.getBuiltinModule("https")
}
```

Preserve the existing comment explaining why the built-in modules are reloaded.

- [ ] **Step 4: Run focused validation**

Run: `pnpm --filter @phoria/opentelemetry exec vitest run src/observability.instrumentation.test.ts`

Expected: 1 test passes with both prototype assertions passing.

Run: `pnpm biome check packages/phoria-opentelemetry/src/observability.ts packages/phoria-opentelemetry/src/observability.instrumentation.test.ts`

Expected: no diagnostics.

- [ ] **Step 5: Commit**

```bash
git add packages/phoria-opentelemetry/src/observability.ts packages/phoria-opentelemetry/src/observability.instrumentation.test.ts
git commit -m "refactor(opentelemetry): share http instrumentation guard"
```

### Task 2: Isolate Phoria health-check HTTP logging

**Files:**
- Modify: `packages/Phoria/ServiceCollectionExtensions.cs`
- Modify: `packages/Phoria/Server/PhoriaServerHttpClientFactory.cs`
- Modify: `packages/Phoria/Server/PhoriaServerMonitor.cs`
- Test: `packages/Phoria.Tests/Server/PhoriaServerMonitorTests.cs`
- Test: `packages/Phoria.Tests/PhoriaServiceCollectionTests.cs`

**Interfaces:**
- Consumes: public `IPhoriaServerHttpClientFactory`, `PhoriaOptions`, `PhoriaObservabilityOptions`, and the existing `PhoriaServerMonitor` public constructor behavior.
- Produces: internal `IPhoriaServerHealthCheckHttpClientFactory`; named client `PhoriaServerHealthCheckHttpClient`; an `IConfigureOptions<LoggerFilterOptions>` registration that sets the dedicated category threshold from `LogHealthChecks`.

- [ ] **Step 1: Add failing tests for the logging threshold**

Add service-registration tests that build a minimal service collection with `AddPhoria()`, `AddLogging()`, and `PhoriaObservabilityOptions` configured both ways. Inspect `ILoggerFactory` with these exact categories:

```csharp
const string healthCheckCategory =
	"System.Net.Http.HttpClient.PhoriaServerHealthCheckHttpClient.LogicalHandler";
```

Assert that `LogLevel.Information` is disabled when `LogHealthChecks` is false and enabled when it is true. Assert that `LogLevel.Warning` remains enabled in both cases. Also assert the existing `PhoriaServerHttpClient` category remains independently registered for application SSR/middleware traffic.

Add or adapt monitor tests so the monitor receives the health-check client factory, while existing public-constructor tests continue to compile and exercise the existing public `IPhoriaServerHttpClientFactory` path.

- [ ] **Step 2: Run the focused .NET tests to verify failure**

Run: `dotnet test packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --no-restore`

Expected: the new registration tests fail because the dedicated client, factory, and filter do not yet exist. Existing monitor tests should compile or identify the exact constructor seam that needs the compatibility overload.

- [ ] **Step 3: Register a dedicated health-check client in `AddPhoria()`**

Keep the existing public `IPhoriaServerHttpClientFactory` interface unchanged. Add an internal `IPhoriaServerHealthCheckHttpClientFactory` and have the internal `PhoriaServerHttpClientFactory` implement both interfaces.

Add the named client constant:

```csharp
internal const string HealthCheckHttpClientName = "PhoriaServerHealthCheckHttpClient";
```

Register the health-check named client with the same base handler and development certificate behavior as the existing Phoria client. The monitor-only factory method must set `BaseAddress` from `PhoriaOptions.GetServerUrl()` and the monitor must use this method exclusively for `GET /hc`. Keep the existing client for `PhoriaServerMiddleware` and `PhoriaIslandSsr`.

Register the shared concrete factory once, expose it through both interfaces, and construct the monitor through an explicit DI factory so the internal health-check constructor is selected. Retain the existing public `PhoriaServerMonitor` constructor as a compatibility path for external consumers and current tests; it may delegate through the existing public client interface.

- [ ] **Step 4: Add the Phoria-owned logging filter**

Create an internal `IConfigureOptions<LoggerFilterOptions>` implementation in the Phoria logging/server area. It must add a rule for the category prefix:

```text
System.Net.Http.HttpClient.PhoriaServerHealthCheckHttpClient
```

Use `LogLevel.Information` when `PhoriaObservabilityOptions.LogHealthChecks` is true and `LogLevel.Warning` otherwise. Do not add a rule for the general `System.Net.Http.HttpClient` prefix and do not alter exporter categories here.

Register the configurator from `AddPhoria()` after binding `PhoriaObservabilityOptions`.

- [ ] **Step 5: Run focused tests to verify the implementation**

Run: `dotnet test packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --no-restore`

Expected: all Phoria tests pass on both target frameworks, including disabled/enabled health-check logging threshold tests and existing monitor behavior tests.

- [ ] **Step 6: Commit**

```bash
git add packages/Phoria packages/Phoria.Tests
git commit -m "fix(phoria): isolate health check client logging"
```

### Task 3: Fix example endpoint schemes and exporter diagnostic defaults

**Files:**
- Modify: `examples/framework-multiple/AppHost/Program.cs`
- Modify: `examples/getting-started/AppHost/Program.cs`
- Modify: `examples/framework-multiple/WebApp/appsettings.json`
- Modify: `examples/getting-started/WebApp/appsettings.json`

**Interfaces:**
- Consumes: merged environment-specific `webAppConfiguration`, `Phoria:Server:Port`, and `Phoria:Server:Https`.
- Produces: scheme-correct Aspire resources; default category thresholds for `OtlpLogExporter`, `OtlpMetricExporter`, and `OtlpTraceExporter`.

- [ ] **Step 1: Record the endpoint-selection acceptance cases**

The implementation and runtime verification must satisfy these exact cases; no new AppHost helper or duplicate configuration source is needed:

```text
Development: Phoria:Server:Https=true  -> WithHttpsEndpoint(...)
Preview:     Phoria:Server:Https=false -> WithHttpEndpoint(...)
```

- [ ] **Step 2: Implement environment-aware endpoint registration**

Read the boolean setting from the already merged configuration:

```csharp
var phoriaServerHttps = bool.TryParse(
	webAppConfiguration["Phoria:Server:Https"],
	out var configuredHttps) && configuredHttps;
```

Register the `phoria-server` endpoint with `WithHttpsEndpoint` when `phoriaServerHttps` is true and `WithHttpEndpoint` when false. Preserve the configured port, `isProxied: false`, `WithHttpHealthCheck("/hc")`, OTLP exporter, environment variables, and `WaitFor(phoriaServer)`.

- [ ] **Step 3: Set exporter lifecycle log defaults**

In both WebApp `appsettings.json` files, add these exact `Logging:LogLevel` rules:

```json
"System.Net.Http.HttpClient.OtlpLogExporter": "Warning",
"System.Net.Http.HttpClient.OtlpMetricExporter": "Warning",
"System.Net.Http.HttpClient.OtlpTraceExporter": "Warning"
```

These rules suppress Information-level `Start processing`, `Sending`, `Received`, and `End processing` messages for `/v1/logs`, `/v1/metrics`, and `/v1/traces` by default. They must not change exporter registrations or telemetry collection. Users can opt into a signal's HTTP diagnostics by overriding that category to `Trace` in an environment-specific appsettings file.

- [ ] **Step 4: Run example configuration validation**

Run these commands from the repository root:

```bash
dotnet build examples/framework-multiple/AppHost/AppHost.csproj --configuration Release
dotnet build examples/framework-multiple/WebApp/WebApp.csproj --configuration Release
dotnet build examples/getting-started/AppHost/AppHost.csproj --configuration Release
dotnet build examples/getting-started/WebApp/WebApp.csproj --configuration Release
```

Expected: both AppHost and WebApp projects build with the updated endpoint and logging configuration and no scheme-selection compile errors.

- [ ] **Step 5: Commit**

```bash
git add examples/framework-multiple/AppHost/Program.cs examples/framework-multiple/WebApp/appsettings.json examples/getting-started/AppHost/Program.cs examples/getting-started/WebApp/appsettings.json
git commit -m "fix(examples): align Aspire endpoints and filter exporter logs"
```

### Task 4: Add release metadata

**Files:**
- Create: `.changeset/quiet-otlp-requests.md`

**Interfaces:**
- Produces: a patch release entry for the published `phoria-dotnet` package.

- [ ] **Step 1: Create the changeset**

Create `.changeset/quiet-otlp-requests.md` with this exact content:

```md
---
phoria-dotnet: patch
---

Fix Phoria health-check client diagnostics and example Aspire endpoint schemes while preserving OpenTelemetry log, metric, and trace export.
```

- [ ] **Step 2: Commit**

```bash
git add .changeset/quiet-otlp-requests.md
git commit -m "chore: changeset for Aspire health check logging fix"
```

### Task 5: Build and verify runtime behavior

**Files:**
- No source files; build outputs and runtime evidence only.

**Interfaces:**
- Consumes: all changes from Tasks 1-4 and the linked example packages.
- Produces: verified Development and Preview behavior; no commits.

- [ ] **Step 1: Run package and repository validation**

Run:

```bash
pnpm build
pnpm biome check packages/phoria-opentelemetry/src
pnpm --filter @phoria/opentelemetry check
pnpm test
dotnet test --solution Phoria.sln --configuration Release
```

Expected: all commands pass. The OpenTelemetry regression test remains green, all JS package tests pass, and both .NET target frameworks pass.

- [ ] **Step 2: Refresh linked examples**

Run from the repository root:

```bash
pnpm examples:refresh
```

Expected: both examples use fresh local package `dist` output.

- [ ] **Step 3: Verify Development**

From `examples/framework-multiple/WebApp`, run `pnpm dev`. Confirm in Aspire:

- `phoria-server` and `webapp` are Running.
- The HTTPS `/hc` health check succeeds.
- A real page renders and islands hydrate.
- Aspire receives `http.server.request.duration` data and logs/metrics/traces continue arriving.
- Normal Console and Structured Logs do not show repeated `/hc`, `/v1/logs`, `/v1/metrics`, or `/v1/traces` HTTP lifecycle messages.

Run `pnpm stop` after verification.

- [ ] **Step 4: Verify Preview**

From `examples/framework-multiple/WebApp`, run `pnpm preview`. Confirm in Aspire:

- `phoria-server` is Running and healthy over HTTP.
- `webapp` leaves `Waiting` and becomes Running.
- A real page loads through the WebApp, not only directly against the Node server.
- Aspire receives logs, metrics, and traces, including `http.server.request.duration` data.
- Default logs omit the repetitive health-check and OTLP exporter HTTP lifecycle messages.

Run `pnpm stop` after verification.

- [ ] **Step 5: Verify opt-in diagnostics**

Temporarily override one exporter category, for example:

```json
"System.Net.Http.HttpClient.OtlpMetricExporter": "Trace"
```

Run the Development flow long enough for an export and confirm the `/v1/metrics` Information lifecycle messages appear while the exporter still succeeds. Revert the temporary override before finishing. Also set `phoria.observability.logHealthChecks` to true in a temporary environment config and confirm the dedicated `/hc` client diagnostics become visible without enabling unrelated client categories.

- [ ] **Step 6: Tear down and inspect the worktree**

Run `pnpm stop` from the example WebApp directory, confirm no Aspire/DCP/Node processes remain, then run `git status --short` and verify only intended task files are changed. Do not commit build outputs or runtime evidence.

## Self-Review Checklist

- Scheme selection is driven by the same `Phoria:Server:Https` setting the Node server uses.
- `/hc` filtering is isolated to a Phoria-owned health-check client and follows `LogHealthChecks`.
- OTLP exporter diagnostics default to `Warning` and are opt-in at `Trace`; exporter telemetry remains enabled.
- No broad `System.Net.Http.HttpClient` filter suppresses unrelated requests.
- The Node instrumentation guard has one source of truth.
- Both examples receive identical behavior.
- All test, build, and runtime verification commands have concrete expected outcomes.
