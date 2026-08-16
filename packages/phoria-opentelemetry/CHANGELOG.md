# @phoria/opentelemetry

## 0.2.0-beta.0

### Minor Changes

- 6803d37: Add opt-in OpenTelemetry tracing and metrics alongside structured logging for Phoria hosts and example Phoria Server sidecars, including SSR span correlation, request metrics, configurable signal gates, and health-check log de-duplication.

### Patch Changes

- 6803d37: Fix Phoria Server request metrics (`http.server.request.duration`) missing in production by forcing the HTTP/HTTPS instrumentation patch to apply to the already-loaded core modules.
