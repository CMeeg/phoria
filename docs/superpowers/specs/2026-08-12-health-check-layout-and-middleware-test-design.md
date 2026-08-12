# Health Check Layout and Middleware Test Design

## Scope

Refine the configurable Phoria Server unavailable-policy implementation without changing its runtime policy semantics. The internal Phoria Server probe remains `/hc`; consumer-facing ASP.NET health endpoints remain opt-in and are conventionally mapped by consumers at `/health`.

## Middleware Test Access

Remove the `InternalsVisibleTo` entry added for the middleware unit tests. Keep `PhoriaServerMiddleware` internal because it is an implementation detail and is already exposed through the public `UsePhoria()` application-builder extension.

Rewrite the middleware tests to construct an ASP.NET Core `ApplicationBuilder` with a test service provider, register the monitor, HTTP client factory, options, logger, and HMR proxy dependencies, add the public `UsePhoria()` middleware, and invoke the resulting `RequestDelegate`. Test behavior remains the same: Fail and Degrade handling for unhealthy requests, healthy HTTP proxying, and mid-proxy failures. This tests the stable public seam without expanding the package API or granting friend-assembly access.

## Health Check Layout

Move `PhoriaServerHealthCheck` to `packages/Phoria/Diagnostics/HealthChecks/PhoriaServerHealthCheck.cs` with namespace `Phoria.Diagnostics.HealthChecks`. The class remains a public `IHealthCheck` implementation and retains its existing status mapping and dependency contract.

Move `PhoriaHealthChecksBuilderExtensions` to `packages/Phoria/HealthChecksBuilderExtensions.cs`, rename it to `HealthChecksBuilderExtensions`, and keep namespace `Phoria`. Its public extension method remains `AddPhoriaServerHealthCheck`, with the same default name, null validation, and registration behavior. The extension imports `Phoria.Diagnostics.HealthChecks`.

Move the health-check tests to `packages/Phoria.Tests/Diagnostics/HealthChecks/PhoriaServerHealthCheckTests.cs` with namespace `Phoria.Tests.Diagnostics.HealthChecks`. Test coverage and behavior remain unchanged; only references and namespace/file organization change.

## Endpoint Contract

The Node-side monitor probe remains `/hc` because it is an internal Phoria protocol endpoint. The ASP.NET health check is a separate opt-in integration endpoint whose consumer-facing route is chosen by the application, with `/health` used by the examples. This avoids route ownership conflicts and preserves compatibility with the existing Node server protocol.

## Verification

- `dotnet test --solution Phoria.sln --configuration Release`
- `pnpm lint`
- `pnpm examples:check`
- `git diff --check`
