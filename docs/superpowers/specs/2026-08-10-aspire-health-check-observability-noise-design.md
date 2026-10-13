# Aspire Health Check and Observability Noise Design

## Goal

Make the example Aspire flows work in both Development and Preview, remove repetitive HTTP client diagnostic messages from normal Aspire logs, and preserve all OpenTelemetry telemetry. Health-check diagnostics remain controlled by Phoria's existing `logHealthChecks` setting.

## Runtime Wiring

Both example AppHosts load the environment-specific WebApp configuration and read `Phoria:Server:Https` alongside the configured port. The `phoria-server` resource registers an HTTPS endpoint when the setting is true and an HTTP endpoint when it is false, while retaining the existing port, non-proxied behavior, health check path, OTLP exporter, environment variables, and `WaitFor` relationship.

Development therefore continues to use HTTPS from `appsettings.Development.json`, while Preview's HTTP-only production server uses an HTTP endpoint and its `/hc` health check succeeds. The same correction applies to `examples/getting-started` because it has identical AppHost wiring.

## Phoria Health-Check Client

`AddPhoria()` registers a dedicated named `HttpClient` for the server monitor's `/hc` requests. The existing Phoria server client remains responsible for SSR, middleware, and other application requests; the monitor must use only the health-check client.

The health-check client has a stable logging category derived from its dedicated client name. Its Information-level lifecycle messages are allowed only when `PhoriaObservabilityOptions.LogHealthChecks` is enabled; Warning and Error messages remain available regardless. This makes the `/hc` filter a Phoria library concern and prevents it from suppressing unrelated application requests.

The monitor's existing status-transition logging behavior remains unchanged. This change controls the underlying `HttpClient` lifecycle diagnostics, not health-check execution or status tracking.

## OTLP Exporter Diagnostics

The examples configure the named .NET exporter clients `OtlpLogExporter`, `OtlpMetricExporter`, and `OtlpTraceExporter` at `Warning` by default. Their Information-level HTTP lifecycle messages for `/v1/logs`, `/v1/metrics`, and `/v1/traces` are therefore hidden from normal Aspire Console and Structured Logs views.

Users can opt into an individual exporter's HTTP diagnostics by overriding its category to `Trace`. The exporter registrations and transport remain unchanged, so logs, metrics, and traces continue to be exported to Aspire regardless of diagnostic log filtering. Unrelated `System.Net.Http.HttpClient` categories are not changed.

## Node Observability Cleanup

`createPhoriaObservability` will define one local `httpInstrumentationEnabled` value from `tracingEnabled || metricsEnabled` and use it both when registering `HttpInstrumentation` and when reloading preloaded `node:http`/`node:https` after `sdk.start()`. This removes duplicated guard logic without changing behavior or the public API.

## Testing and Verification

- Add Phoria tests for the dedicated health-check client and monitor behavior with health-check logging disabled and enabled.
- Add example tests or a small testable logging configuration seam covering the exporter category defaults, Trace opt-in, and preservation of unrelated HTTP client categories.
- Verify both AppHosts select the expected endpoint scheme and that Development and Preview health checks pass.
- Verify Preview starts the WebApp instead of remaining blocked on `phoria-server`.
- Verify Aspire receives logs, metrics, and traces after filtering is applied.
- Verify normal runs omit `/hc` and OTLP exporter HTTP lifecycle messages, while Trace-level configuration exposes the exporter diagnostics.

## Scope and Non-Goals

- Do not disable .NET HTTP client tracing or metrics instrumentation.
- Do not disable or alter OTLP exporters.
- Do not globally suppress all `System.Net.Http.HttpClient` logs.
- Do not change Phoria's public observability API.
- Apply the example AppHost and logging changes consistently to both committed examples.
