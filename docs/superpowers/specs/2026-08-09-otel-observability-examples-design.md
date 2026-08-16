# Design: OpenTelemetry observability (traces & metrics, with logging) for the examples

Date: 2026-08-09

## Problem

The examples already wire **logs** to OpenTelemetry on all three tiers (AppHost → WebApp → Phoria Server sidecar), but there is no **tracing** or **metrics** anywhere:

- The Aspire dashboard cannot show a distributed trace linking a .NET page request to the Phoria Server SSR/CSR calls made on its behalf.
- The Node-side `phoria-server` logs are exported with no tracer, so `traceId`/`spanId` on those log records are always undefined (TODO.md:21 — "not seeing any structured otel logs").
- Observability is not toggleable: `WebApp/Program.cs` adds `AddOpenTelemetry` logging unconditionally and the sidecar always exports logs, and the three signals cannot be switched independently.
- The `.NET` Phoria Server monitor logs the outcome of every `/hc` poll every 5 seconds (`PhoriaServerMonitor.cs:112`), which is noisy.

## Goals

1. Distributed tracing across the .NET WebApp and Phoria Server: within a page-request trace in the .NET host you can see where Phoria Server SSR/CSR requests happen, correlated via `traceparent`.
2. Request metrics from both runtimes: ASP.NET Core + HttpClient instrumentation on .NET; `node:http` server/client on Node (the underlying h3 server and the Vite dev server).
3. Logging, tracing and metrics are **mutually exclusive** — each can be turned on/off independently.
4. Logging remains an option in every environment.
5. A `logHealthChecks` setting (default off) suppresses the periodic `/hc` outcome log; the monitor only logs on **status change**.
6. Instrumentation stays opt-in: the published packages remain OTel-free except the new `@phoria/opentelemetry` package and a tiny `ActivitySource` in the Phoria .NET package. Hosts opt in via `AddSource("Phoria")`.

## Decisions

- A new optional published npm package **`@phoria/opentelemetry`** owns all Node-side instrumentation: `NodeSDK` setup, h3 span hooks, config parsing, and the OTel logger adapter. The examples depend on it.
- Signal switches live in a **`phoria:observability`** section in `appsettings.json` + per-environment overrides. Both runtimes read the same files (the .NET host via `builder.Configuration`, the Node package by walking the same appsettings files with the same discovery already used by `parsePhoriaAppSettings`).
- **`PhoriaObservabilityOptions`** (the shared config contract) lives in the Phoria .NET package and is bound in `AddPhoria()`.
- The Phoria .NET package emits a **`Phoria` `ActivitySource`** with a `phoria.ssr.render` span around each SSR POST, tagged `phoria.component`/`phoria.framework`. Additive only: no listener → `StartActivity` returns null → no behavior change.
- Node span enrichment is done by an **h3 `onBeforeResponse` hook** that classifies by URL path + the existing `x-phoria-island-framework` response header — no seam is needed in `@phoria/phoria` routing.
- `.NET` health-check traffic (`GET /hc` from `PhoriaServerMonitor`) and Node-side `/hc` are filtered out of both spans and metrics.

## Section 1 — Config contract: `phoria:observability`

`WebApp/appsettings.json` (base — all off) and per-environment overrides:

```jsonc
// appsettings.json
"phoria": {
  "observability": {
    "logging": false,
    "logHealthChecks": false,
    "tracing": { "enabled": false, "samplingRatio": 0.1 },
    "metrics": false
  }
}
```

| Environment | logging | logHealthChecks | tracing | metrics |
| --- | --- | --- | --- | --- |
| Development | true | false | enabled, samplingRatio 1.0 | true |
| Preview | true | false | enabled, samplingRatio 0.1 | true |
| Production | true | false | enabled, samplingRatio 0.1 | true |

Defaults rationale: base is opt-in so the feature is demonstrable per-environment; Development samples fully so traces appear reliably while developing; Preview/Production show the built-in sampler. `logHealthChecks` defaults off everywhere (status-change-only logging).

### .NET side

- New `PhoriaObservabilityOptions` record in `packages/Phoria` (namespace `Phoria`), `SectionName = "Phoria:Observability"` (case-insensitive, matches `phoria:observability`):

```csharp
public sealed class PhoriaObservabilityOptions
{
    public const string SectionName = "Phoria:Observability";
    public bool Logging { get; set; }
    public bool Metrics { get; set; }
    public bool LogHealthChecks { get; set; }
    public PhoriaObservabilityTracingOptions Tracing { get; set; } = new();
}

public sealed class PhoriaObservabilityTracingOptions
{
    public bool Enabled { get; set; }
    public double SamplingRatio { get; set; } = 0.1;
}
```

- `ServiceCollectionExtensions.AddPhoria()` additionally binds it (alongside the existing `PhoriaOptions` bind at `ServiceCollectionExtensions.cs:17-19`) so `PhoriaServerMonitor` can consume `LogHealthChecks` via DI:

```csharp
services.AddOptions<PhoriaObservabilityOptions>().BindConfiguration(PhoriaObservabilityOptions.SectionName);
```

- The example `Program.cs` reads it for wiring: `var obs = builder.Configuration.GetSection(PhoriaObservabilityOptions.SectionName).Get<PhoriaObservabilityOptions>() ?? new();`

### Node side

- `@phoria/opentelemetry` exposes `parsePhoriaObservabilityAppSettings({ cwd, environment })` mirroring `parsePhoriaAppSettings` (used at `examples/*/WebApp/ui/src/server.ts:60` with `{ environment: dotnetEnv, cwd: __dirname }`): load `appsettings.json` then `appsettings.{environment}.json` from `cwd`, merge, map to the same shape. Environment resolved as `DOTNET_ENVIRONMENT ?? ASPNETCORE_ENVIRONMENT ?? NODE_ENV ?? "Development"`.

## Section 2 — New package `@phoria/opentelemetry`

New workspace package `packages/phoria-opentelemetry` (`@phoria/opentelemetry`), server-only, single entry. Follows `phoria-islands` conventions (Vite build, Biome, `tsc`, Vitest), no `cross-env NODE_ENV=production` needed (no framework/client imports). All `@opentelemetry/*` packages are **dependencies** via the catalog at `^0.221.0` (matching the versions the examples already use), so pnpm dedupes them to a single physical store copy — a single `@opentelemetry/api` instance without forcing consumers to install peers.

### Exports

- `parsePhoriaObservabilityAppSettings(...)` → `{ logging, logHealthChecks?, tracing: { enabled, samplingRatio }, metrics }` (Section 1).
- `createPhoriaLogger(settings)` → object structurally compatible with `PhoriaLogger` (`packages/phoria-islands/src/server/routing.ts:33`, `info`/`warn`/`error` with `(message, data?)`). Emits OTel log records (severity + `event` attribute) when `logging` is enabled; falls back to `console.*` otherwise.
- `createPhoriaObservability(settings)` → `{ shutdown }`. Called **before** `createApp()`/`listen()`:
  - **tracing enabled**: `NodeSDK` with `OTLPTraceExporter`, sampler `parentBased(traceIdRatioBased(samplingRatio))` so SSR requests follow the .NET `traceparent` decision, `HttpInstrumentation` registered.
  - **tracing disabled, metrics enabled**: `NodeSDK` with `tracingEnabled: false` (no tracer provider, no spans) but `HttpInstrumentation` still registered so `http.*` metrics flow.
  - **metrics enabled**: `PeriodicExportingMetricReader` + `OTLPMetricExporter` (NodeSDK `metricReader`). Metrics come from `HttpInstrumentation` (`http.server.request.duration`, `http.client.request.duration`).
  - **logging enabled**: `LoggerProvider` (from `@opentelemetry/sdk-logs`) with OTLP log exporter + global provider (replaces the hand-rolled provider in `server.ts`).
  - `HttpInstrumentation` options: `ignoreIncomingRequestHook: (req) => req.url === "/hc"` (drops health-check spans + metrics).
  - Guard against double-initialization (module-level init flag).
- `createPhoriaRequestSpanHook({ base, ssrBase })` → `{ onRequest, onBeforeResponse }` for h3 `createApp()`:
  - `onRequest(event)` → stash `trace.getActiveSpan()` on `event.context.phoriaSpan` (this runs inside the `node:http` span's context, so the reference is reliable regardless of whether the span is still active later).
  - `onBeforeResponse(event)` → no-op if no stashed span (unsampled request):
    - `POST {ssrBase}/render/{component}` → `updateName("phoria-server.ssr.render")`, `setAttribute("phoria.component", component)` (parsed from URL), and `phoria.framework` from the `x-phoria-island-framework` response header (already set by `createPhoriaSsrRouter` for both dev and prod — `routing.ts:130`).
    - `GET {base}/**` → `updateName("phoria-server.csr.asset")`.
    - Everything else stays a plain HTTP span.

### Catalog additions

Add to `pnpm-workspace.yaml` catalog (all `^0.221.0`): `@opentelemetry/api`, `@opentelemetry/exporter-metrics-otlp-http`, `@opentelemetry/exporter-trace-otlp-http`, `@opentelemetry/instrumentation`, `@opentelemetry/instrumentation-http`, `@opentelemetry/sdk-metrics`, `@opentelemetry/sdk-node`, `@opentelemetry/sdk-trace-base`. (api-logs / sdk-logs / exporter-logs-otlp-http already present.)

## Section 3 — Phoria .NET package changes

### 3a. `PhoriaActivitySource`

New `packages/Phoria/Diagnostics/PhoriaActivitySource.cs`:

```csharp
public static class PhoriaActivitySource
{
    public const string Name = "Phoria";
    public static readonly ActivitySource Instance = new(Name, typeof(PhoriaActivitySource).Assembly.GetName().Version?.ToString());
}
```

No OTel package dependency (System.Diagnostics.DiagnosticSource ships via `FrameworkReference Microsoft.AspNetCore.App`). `packages/Phoria/Phoria.csproj` unchanged.

### 3b. `PhoriaIslandSsr` activity

`RenderIsland` (`packages/Phoria/Islands/PhoriaIslandSsr.cs:23-77`) wraps the POST (`PhoriaIslandSsr.cs:42`):

```csharp
using Activity? activity = PhoriaActivitySource.Instance.StartActivity("phoria.ssr.render", ActivityKind.Client);
activity?.SetTag("phoria.component", island.ComponentName);
// ... after headers read (PhoriaIslandSsr.cs:50-55):
activity?.SetTag("phoria.framework", island.Framework);
```

Null-safe: with no listener the whole block is a no-op. The HttpClient child span (`PhoriaServerHttpClient`, `PhoriaServerHttpClientFactory.cs:15`) carries the HTTP details and propagates `traceparent` automatically via .NET HttpClient instrumentation. Tests are unaffected (`StartActivity` returns null).

### 3c. `PhoriaServerMonitor` health-check logging gate

`logHealthChecks` gates the per-poll outcome logs in `CheckHealth` (`PhoriaServerMonitor.cs:89-140`). Constructor gains `IOptions<PhoriaObservabilityOptions>`; the flag is captured as `bool logHealthChecks`.

- Healthy path (`PhoriaServerMonitor.cs:110-119`): log only when `logHealthChecks` is true **or** the previous `ServerStatus.Health` was not `Healthy` (i.e. a transition to healthy).
- Unhealthy path (`LogServerIsUnhealthy`, `PhoriaServerMonitor.cs:142-152`): return early when `logHealthChecks` is false **and** `ServerStatus.Health` is already `Unhealthy` (e.g. repeated startup failures after the first `LogServerIsNotReadyYet`, or repeated failures after `LogServerIsUnhealthy`).
- Existing `LogServerIsHealthy`/`LogServerIsUnhealthy`/`LogServerIsNotReadyYet` messages and EventIds (central `EventId.cs` registry) are unchanged; only the cadence of emissions changes.

Test impact: `PhoriaServerMonitorTests.cs` constructs `new PhoriaServerMonitor(logger, Options.Create(options), stubFactory)` at lines 19, 39, 58, 75, 107, 129, 151, 176 — each gains `Options.Create(new PhoriaObservabilityOptions())`. Add tests: healthy-then-healthy emits once with gate off; healthy→unhealthy→healthy emits on each transition.

## Section 4 — Example wiring (both `getting-started` and `framework-multiple`)

### 4a. .NET host (`WebApp/WebApp.csproj` + `Directory.Packages.props`)

Add to each example's `Directory.Packages.props` (alongside existing `OpenTelemetry.*` at `1.17.0`):

- `OpenTelemetry.Instrumentation.AspNetCore` `1.17.0`
- `OpenTelemetry.Instrumentation.Http` `1.17.0`

`WebApp.csproj` gains the two `PackageReference` items (unversioned; CPM).

### 4b. `WebApp/Program.cs`

Replace the unconditional logging block (lines 7-17) with three mutually exclusive gated blocks, each OTLP exporter still gated on `OTEL_EXPORTER_OTLP_ENDPOINT`:

```csharp
PhoriaObservabilityOptions observability = builder.Configuration
    .GetSection(PhoriaObservabilityOptions.SectionName)
    .Get<PhoriaObservabilityOptions>() ?? new();

if (observability.Logging)
{
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
}

if (observability.Tracing.Enabled)
{
    builder.Services.AddOpenTelemetry().WithTracing(tracing =>
    {
        tracing
            .AddSource(PhoriaActivitySource.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation(o => o.FilterHttpRequestMessage =
                request => request.RequestUri?.AbsolutePath != "/hc")
            .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(observability.Tracing.SamplingRatio)));
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
        {
            tracing.AddOtlpExporter();
        }
    });
}

if (observability.Metrics)
{
    builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
        {
            metrics.AddOtlpExporter();
        }
    });
}
```

(Exact option names verified against the Aspire ServiceDefaults pattern: `ConfigureOpenTelemetry` uses `AddAspNetCoreInstrumentation`/`AddHttpClientInstrumentation`/`AddRuntimeInstrumentation` + `AddSource` + `SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(ratio)))`. Runtime instrumentation is intentionally omitted here to keep the example's package list to the two instrumentation packages; `AddRuntimeInstrumentation` can be added back via `OpenTelemetry.Instrumentation.Runtime`.)

### 4c. `ui/src/server.ts`

Adopt the package (both examples):

```ts
import { createPhoriaLogger, createPhoriaObservability, createPhoriaRequestSpanHook, parsePhoriaObservabilityAppSettings } from "@phoria/opentelemetry"

const observabilitySettings = parsePhoriaObservabilityAppSettings({ cwd: __dirname, environment: dotnetEnv })
const phoriaLogger = createPhoriaLogger(observabilitySettings)
const observability = await createPhoriaObservability(observabilitySettings)

const app = createApp({ ...createPhoriaRequestSpanHook({ base: appsettings.base, ssrBase: appsettings.ssrBase }) })
```

- Delete the hand-rolled provider/`log`/`phoriaLogger` block (`getting-started`: lines 20-54; `framework-multiple`: lines 20-54).
- `shutdown` calls `await observability.shutdown()` (flush + close log/trace/metric providers) in addition to `listener.close()` (`getting-started`: 103-116; `framework-multiple`: 143-164).
- `ui/package.json` adds `@phoria/opentelemetry`; OTel runtime deps already present at `^0.221.0`.

## Section 5 — Trace shape (goal state)

```
HTTP GET /                                 (AspNetCore instr. — aspnetcore.request.duration)
└─ phoria.ssr.render                       (new "Phoria" ActivitySource, kind Client)
   └─ HTTP POST /render/Island1            (HttpClient instr. via PhoriaServerHttpClient — traceparent propagated)
      └─ HTTP POST /render/Island1         (node:http instr. — renamed by hook to phoria-server.ssr.render)
         tags: phoria.component=Island1, phoria.framework=react
```

Client assets: `HTTP GET /ui/phoria/...` → renamed `phoria-server.csr.asset`. `/hc` filtered on both sides. Logs: both runtimes via OTLP, now carrying real `traceId`/`spanId` when tracing is on.

## Verification

- `pnpm build && pnpm lint && pnpm check && pnpm test`; `dotnet test --solution Phoria.sln --configuration Release`.
- `pnpm examples:link` + build, then run `getting-started` under `aspire run`: dashboard shows (a) a distributed trace per page request spanning WebApp → phoria-server SSR, (b) `http.*` metrics for both services, (c) OTel logs with trace context.
- Toggle matrix in `appsettings.Development.json`: disable each signal and confirm the others still work and the disabled one disappears.
- Confirm `/hc` (every 5s) appears in neither spans nor metrics, and with `logHealthChecks: false` the monitor logs once per status change only.
- `pnpm examples:check` passes after `examples:sync`; `pnpm examples:bump` rewrites refs post-publish.

## Rollout

- Changesets: Phoria (.NET) minor (ActivitySource + options + monitor gate); `@phoria/opentelemetry` new package.
- Register the new package: workspace glob `packages/*` already covers it; turbo picks it up via `^build`; add to `pnpm-workspace.yaml` catalog as in Section 2.
- `TODO.md`: close lines 21 and 29.
- Docs: `docs/PROJECT.md`, `docs/MEMORY.md`, `docs/ARCHITECTURE.md` (observability section for the example server.ts).
