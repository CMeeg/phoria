# Health Check Layout and Middleware Test Seam Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove test-only friend-assembly access and reorganize the public health-check API without changing runtime behavior or endpoint contracts.

**Architecture:** Keep `PhoriaServerMiddleware` internal and test it through the public `UsePhoria()` application-builder extension using an ASP.NET Core request pipeline and test service provider. Keep the internal Node probe at `/hc`; keep the consumer-selected ASP.NET health route at `/health` in the examples. Move the health-check implementation under `Phoria.Diagnostics.HealthChecks` while retaining the registration extension in the root `Phoria` namespace.

**Tech Stack:** C# 13, .NET 8 + .NET 10, ASP.NET Core middleware and health checks, xUnit v3, Microsoft.Testing.Platform.

## Global Constraints

- `packages/Phoria/Phoria.csproj` targets `net8.0;net10.0` with nullable reference types and implicit usings enabled.
- No `InternalsVisibleTo` entry remains in `packages/Phoria/Phoria.csproj`.
- The Node-side monitor probe remains `/hc`; the public examples map the opt-in ASP.NET health check at `/health`.
- `PhoriaServerHealthCheck` namespace is `Phoria.Diagnostics.HealthChecks`.
- The registration extension class is `HealthChecksBuilderExtensions` in namespace `Phoria`.
- The extension method remains `AddPhoriaServerHealthCheck(this IHealthChecksBuilder builder, string name = "phoria-server")`.
- Runtime policy behavior and health-status mapping remain unchanged.
- Verification commands: `dotnet test --solution Phoria.sln --configuration Release`, `pnpm lint`, `pnpm examples:check`, and `git diff --check`.

---

### Task 1: Reorganize health-check API and tests

**Files:**
- Move: `packages/Phoria/Health/PhoriaServerHealthCheck.cs` to `packages/Phoria/Diagnostics/HealthChecks/PhoriaServerHealthCheck.cs`
- Move: `packages/Phoria/Health/PhoriaHealthChecksBuilderExtensions.cs` to `packages/Phoria/HealthChecksBuilderExtensions.cs`
- Move: `packages/Phoria.Tests/Health/PhoriaServerHealthCheckTests.cs` to `packages/Phoria.Tests/Diagnostics/HealthChecks/PhoriaServerHealthCheckTests.cs`

**Interfaces:**
- Consumes: existing `IPhoriaServerMonitor`, `PhoriaOptions`, `PhoriaServerUnavailableBehavior`, and `IHealthCheck` behavior.
- Produces: `Phoria.Diagnostics.HealthChecks.PhoriaServerHealthCheck` and `Phoria.HealthChecksBuilderExtensions.AddPhoriaServerHealthCheck`.

- [ ] **Step 1: Update the test namespace and imports first**

In the moved test file, change the namespace to:

```csharp
namespace Phoria.Tests.Diagnostics.HealthChecks;
```

Keep the test cases and assertions unchanged. The tests must continue to cover Healthy, Degrade, Fail, and Unknown-under-Fail mappings.

- [ ] **Step 2: Run the focused tests and verify the expected namespace failure**

Run:

```bash
dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --filter-class Phoria.Tests.Diagnostics.HealthChecks.PhoriaServerHealthCheckTests
```

Expected: compilation fails because the production namespace/file moves and extension rename have not yet been applied.

- [ ] **Step 3: Move and rename the production implementation**

Move `PhoriaServerHealthCheck` to the new path and change only its namespace:

```csharp
namespace Phoria.Diagnostics.HealthChecks;
```

Move the builder extension to the package root, import the new health-check namespace, and rename the class while retaining the method:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Phoria.Diagnostics.HealthChecks;

namespace Phoria;

public static class HealthChecksBuilderExtensions
{
	public static IHealthChecksBuilder AddPhoriaServerHealthCheck(
		this IHealthChecksBuilder builder,
		string name = "phoria-server")
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.AddCheck<PhoriaServerHealthCheck>(name);

		return builder;
	}
}
```

- [ ] **Step 4: Run focused and full .NET tests**

Run:

```bash
dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --filter-class Phoria.Tests.Diagnostics.HealthChecks.PhoriaServerHealthCheckTests
dotnet test --solution Phoria.sln --configuration Release
```

Expected: focused health-check tests and the full solution pass on both target frameworks. No changes are needed to example calls because the extension method remains in namespace `Phoria`.

- [ ] **Step 5: Commit the health-check layout change**

```bash
git add packages/Phoria/Diagnostics/HealthChecks/PhoriaServerHealthCheck.cs packages/Phoria/HealthChecksBuilderExtensions.cs packages/Phoria.Tests/Diagnostics/HealthChecks/PhoriaServerHealthCheckTests.cs packages/Phoria/Health packages/Phoria.Tests/Health
git commit -m "refactor(phoria): reorganize health check APIs"
```

### Task 2: Remove friend access and test middleware through the public seam

**Files:**
- Modify: `packages/Phoria/Phoria.csproj`
- Modify: `packages/Phoria.Tests/Server/PhoriaServerMiddlewareTests.cs`

**Interfaces:**
- Consumes: public `Phoria.ApplicationBuilderExtensions.UsePhoria`, existing middleware dependencies, and the current middleware behavior.
- Produces: middleware tests that do not reference the internal `PhoriaServerMiddleware` type directly; no public API change.

- [ ] **Step 1: Replace direct middleware construction with a public-pipeline test helper**

Remove the direct `PhoriaServerMiddleware` return type and constructor calls from the test. Add the required ASP.NET Core builder and dependency-injection imports, then create a helper that builds the public pipeline:

```csharp
private static (RequestDelegate Pipeline, IServiceProvider Services) CreatePipeline(
	IPhoriaServerMonitor serverMonitor,
	IPhoriaServerHttpClientFactory httpClientFactory,
	IOptions<PhoriaOptions> options,
	RequestDelegate? next = null,
	IViteDevServerHmrProxy? hmrProxy = null)
{
	var services = new ServiceCollection();
	services.AddLogging();
	services.AddSingleton(serverMonitor);
	services.AddSingleton(httpClientFactory);
	services.AddSingleton(options);
	services.AddSingleton(hmrProxy ?? new StubHmrProxy());

	ServiceProvider serviceProvider = services.BuildServiceProvider();
	var application = new ApplicationBuilder(serviceProvider);
	application.UsePhoria();
	application.Run(next ?? (_ => Task.CompletedTask));

	return (application.Build(), serviceProvider);
}
```

Each test should deconstruct the tuple, set `context.RequestServices = services`, invoke `pipeline(context)`, and dispose the service provider after the test. Preserve the existing assertions for 503, fall-through, proxy body, and mid-proxy behavior.

- [ ] **Step 2: Run the middleware tests and verify the expected access failure is gone**

Run:

```bash
dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release --filter-class Phoria.Tests.Server.PhoriaServerMiddlewareTests
```

Expected: the middleware tests compile and pass through `UsePhoria()` without accessing the internal middleware type.

- [ ] **Step 3: Remove InternalsVisibleTo**

Delete the `ItemGroup` containing:

```xml
<InternalsVisibleTo Include="Phoria.Tests" />
```

from `packages/Phoria/Phoria.csproj`. Do not make `PhoriaServerMiddleware` public.

- [ ] **Step 4: Run all verification**

Run:

```bash
dotnet test --solution Phoria.sln --configuration Release
pnpm lint
pnpm examples:check
git diff --check
```

Expected: all .NET tests, lint, example consistency checks, and whitespace validation pass. Confirm `git grep InternalsVisibleTo -- packages/Phoria` returns no results.

- [ ] **Step 5: Commit the middleware test seam change**

```bash
git add packages/Phoria/Phoria.csproj packages/Phoria.Tests/Server/PhoriaServerMiddlewareTests.cs
git commit -m "test(phoria): test middleware through public pipeline"
```
