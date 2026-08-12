# Configurable Phoria Server unavailable policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the WebApp's behavior when the Phoria Server is unavailable a consumer choice (`Degrade` or `Fail`), with a fail-fast startup timeout, bounded process restarts, an opt-in health check, and the latent correctness fixes while unhealthy.

**Architecture:** All changes are .NET-only in the `Phoria` package. New options and a policy enum default to today's behavior (pure opt-in). The monitor gains a startup timeout, the component factory/tag helper/middleware branch on the policy, the process supervisor bounds restarts, a new `Health/` folder exposes `PhoriaServerHealthCheck`, and the entry tag helper suppresses asset output while unhealthy. Both examples wire the health check and production/preview config, with an e2e `/health` assertion.

**Tech Stack:** C# 13 / .NET 8 + 10, ASP.NET Core (TagHelpers, middleware, `IHealthCheck`), xUnit v3 / Microsoft.Testing.Platform, Vitest for example e2e, Changesets (`phoria-dotnet` minor).

## Global Constraints

- `packages/Phoria/Phoria.csproj` targets `net8.0;net10.0`; nullable reference types enabled; language version 13.0; implicit usings enabled.
- Options are bound from `phoria:server:startupTimeout`, `phoria:server:unavailableBehavior`, `phoria:server:process:maxRestartAttempts`. No JS-side mirror — the Node `appsettings.ts` parser ignores unknown keys.
- Defaults MUST preserve current behavior: `StartupTimeout = 0` (wait indefinitely), `UnavailableBehavior = Degrade`, `MaxRestartAttempts = 0` (unlimited).
- The policy enum lives in `PhoriaServerStatus.cs` next to `PhoriaServerHealth`/`PhoriaServerMode`.
- New EventIds: `Islands.ServerUnhealthySuppressingComponent = 1105`, `Islands.EntryTagsSuppressedWhileUnhealthy = 1106`, `Server.ServerStartupTimeout = 1218`, `Server.ServerRestartLimitExceeded = 1219`, `Server.MiddlewareServerUnavailable = 1220`.
- No new NuGet dependencies: `IHealthCheck`/`IHealthChecksBuilder`/`AddCheck<T>` ship in the `Microsoft.AspNetCore.App` shared framework (already referenced).
- Commit message style follows repo history: `feat(phoria): ...`, `test(phoria): ...`, `fix(phoria): ...`, `docs: ...`, `chore(examples): ...`.
- Verification for every .NET task: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`.

---

### Task 1: New options, policy enum, and EventIds

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerStatus.cs`
- Modify: `packages/Phoria/PhoriaOptions.cs`
- Modify: `packages/Phoria/Logging/EventId.cs`
- Test: `packages/Phoria.Tests/PhoriaOptionsTests.cs`

**Interfaces:**
- Produces: `PhoriaServerUnavailableBehavior` enum (`Degrade`, `Fail`); `PhoriaServerOptions.StartupTimeout` (int seconds, default 0); `PhoriaServerOptions.UnavailableBehavior` (default `Degrade`); `PhoriaServerOptions.ProcessOptions.MaxRestartAttempts` (int, default 0); EventId constants listed in Global Constraints. Later tasks consume all of these.

- [ ] **Step 1: Write the failing tests**

Add to `packages/Phoria.Tests/PhoriaOptionsTests.cs` (add `using Phoria.Server;` at the top):

```csharp
[Fact]
public void DefaultServerOptions_MatchJavaScriptDefaults()
{
	var options = new PhoriaOptions();

	Assert.Equal("localhost", options.Server.Host);
	Assert.Equal((ushort)5173, options.Server.Port);
	Assert.False(options.Server.Https);
	Assert.Equal(5, options.Server.HealthCheckTimeout);
	Assert.Equal(5, options.Server.HealthCheckInterval);
	Assert.Equal(0, options.Server.StartupTimeout);
	Assert.Equal(PhoriaServerUnavailableBehavior.Degrade, options.Server.UnavailableBehavior);
}

[Fact]
public void DefaultProcessOptions_RestartUnlimitedByDefault()
{
	var options = new PhoriaOptions
	{
		Server = new PhoriaServerOptions
		{
			Process = new PhoriaServerOptions.ProcessOptions { Command = "node" }
		}
	};

	Assert.Equal(0, options.Server.Process.MaxRestartAttempts);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: compile errors — `PhoriaServerUnavailableBehavior` and the two new properties don't exist yet.

- [ ] **Step 3: Add the enum**

In `packages/Phoria/Server/PhoriaServerStatus.cs`, after the `PhoriaServerMode` enum:

```csharp
public enum PhoriaServerUnavailableBehavior
{
	Degrade,
	Fail
}
```

- [ ] **Step 4: Add the options**

In `packages/Phoria/PhoriaOptions.cs`, add `using Phoria.Server;` to the top, and add to `PhoriaServerOptions` (after `HealthCheckInterval`) and to `ProcessOptions` (after `HealthCheckInterval`):

```csharp
	/// <summary>
	/// The maximum number of seconds to wait for the Phoria server to become healthy at startup.
	/// Default is 0, which waits indefinitely.
	/// </summary>
	public int StartupTimeout { get; set; }

	/// <summary>
	/// The behavior to apply when the Phoria server is unavailable.
	/// Default is <see cref="PhoriaServerUnavailableBehavior.Degrade"/>.
	/// </summary>
	public PhoriaServerUnavailableBehavior UnavailableBehavior { get; set; } = PhoriaServerUnavailableBehavior.Degrade;
```

```csharp
		/// <summary>
		/// The maximum number of times the server process is restarted when it exits while unhealthy.
		/// Default is 0, which restarts indefinitely.
		/// </summary>
		public int MaxRestartAttempts { get; set; }
```

- [ ] **Step 5: Add the EventIds**

In `packages/Phoria/Logging/EventId.cs`, extend the `Islands` and `Server` classes:

```csharp
	public static class Islands
	{
		public const int EntryAttributeMissing = 1101;
		public const int ViteManifestKeyNotFound = 1102;
		public const int ManifestEntryDoesntHaveCssChunks = 1103;
		public const int ServerUnhealthyDegradingToClient = 1104;
		public const int ServerUnhealthySuppressingComponent = 1105;
		public const int EntryTagsSuppressedWhileUnhealthy = 1106;
	}
```

```csharp
	public static class Server
	{
		// ... existing 1201-1217 entries unchanged ...
		public const int ServerStartupTimeout = 1218;
		public const int ServerRestartLimitExceeded = 1219;
		public const int MiddlewareServerUnavailable = 1220;
	}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS (all existing + the two new tests).

- [ ] **Step 7: Commit**

```bash
git add packages/Phoria/Server/PhoriaServerStatus.cs packages/Phoria/PhoriaOptions.cs packages/Phoria/Logging/EventId.cs packages/Phoria.Tests/PhoriaOptionsTests.cs
git commit -m "feat(phoria): add unavailable behavior policy options and event ids"
```

---

### Task 2: Monitor startup timeout, preserve last-known state, and monitor service restructure

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerMonitor.cs`
- Modify: `packages/Phoria/Server/PhoriaServerMonitorService.cs`
- Test: `packages/Phoria.Tests/Server/PhoriaServerMonitorTests.cs`

**Interfaces:**
- Consumes: `PhoriaOptions.Server.StartupTimeout`; `EventId.Server.ServerStartupTimeout`.
- Produces: `PhoriaServerMonitor.StartMonitoring` throws `TimeoutException` when `StartupTimeout > 0` elapses before the first healthy check; `PhoriaServerMonitor.ServerStatus.Mode`/`Frameworks` preserved across an unhealthy transition; `PhoriaServerMonitorService.ExecuteAsync` stops the monitor cleanly then rethrows on any non-cancellation failure.

- [ ] **Step 1: Write the failing tests**

Add to `packages/Phoria.Tests/Server/PhoriaServerMonitorTests.cs`:

```csharp
[Fact]
public async Task StartMonitoring_ThrowsTimeoutException_WhenStartupTimeoutElapses()
{
	var options = new PhoriaOptions();
	options.Server.HealthCheckInterval = 1;
	options.Server.StartupTimeout = 1;
	var logger = new ListLogger();
	var monitor = new PhoriaServerMonitor(
		logger,
		Options.Create(options),
		new StubHttpClientFactory(HttpStatusCode.ServiceUnavailable),
		Options.Create(new PhoriaObservabilityOptions()));
	using var cancellation = new CancellationTokenSource();

	await Assert.ThrowsAsync<TimeoutException>(
		() => monitor.StartMonitoring(cancellation.Token)
			.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

	Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("startup timeout", StringComparison.OrdinalIgnoreCase));
	await monitor.StopMonitoring();
}

[Fact]
public async Task Monitor_PreservesLastKnownHealthyModeAndFrameworksWhenUnhealthy()
{
	var options = new PhoriaOptions();
	options.Server.HealthCheckInterval = 1;
	var monitor = new PhoriaServerMonitor(
		NullLogger<PhoriaServerMonitor>.Instance,
		Options.Create(options),
		new ScriptedHttpClientFactory(i => i == 1
			? HealthyResponse("production", ["react"])
			: UnhealthyResponse()),
		Options.Create(new PhoriaObservabilityOptions()));
	using var cancellation = new CancellationTokenSource();
	Task startTask = monitor.StartMonitoring(cancellation.Token);

	await startTask.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
	Assert.Equal(PhoriaServerHealth.Healthy, monitor.ServerStatus.Health);
	Assert.Equal(PhoriaServerMode.Production, monitor.ServerStatus.Mode);

	await WaitUntilAsync(() => monitor.ServerStatus.Health == PhoriaServerHealth.Unhealthy, TimeSpan.FromSeconds(3));

	Assert.Equal(PhoriaServerHealth.Unhealthy, monitor.ServerStatus.Health);
	Assert.Equal(PhoriaServerMode.Production, monitor.ServerStatus.Mode);
	Assert.Equal(["react"], monitor.ServerStatus.Frameworks);
	cancellation.Cancel();
	await monitor.StopMonitoring();
}
```

Update the `HealthyResponse` helper (replace the existing parameterless one) so the second test can control `mode`/`frameworks`:

```csharp
private static HttpResponseMessage HealthyResponse(string mode = "development", string[]? frameworks = null)
{
	string frameworksJson = string.Join(",", (frameworks ?? []).Select(f => $"\"{f}\""));
	return new(HttpStatusCode.OK)
	{
		Content = new StringContent($"{{\"mode\":\"{mode}\",\"frameworks\":[{frameworksJson}]}}")
	};
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: the new timeout test FAILS (no timeout thrown) and the preserve test FAILS (`Mode` resets to `Development`). The existing `Monitor_RecoversAndAllowsIsomorphicSsrAfterHealthyPoll` and `Monitor_LogsHealthyOnce...` tests still pass (no unhealthy-with-prior-healthy transition that asserts `Mode`).

- [ ] **Step 3: Apply the startup timeout**

In `packages/Phoria/Server/PhoriaServerMonitor.cs`, change both `await firstHealthy.Task.WaitAsync(...)` call sites in `StartMonitoring` to use a private helper:

```csharp
	public async Task StartMonitoring(CancellationToken cancellationToken)
	{
		if (monitoringTask != null)
		{
			await WaitForFirstHealthy(cancellationToken);
			return;
		}

		semaphore = new(1, 1);
		firstHealthy = CreateFirstHealthySource();
		monitoringCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		monitoringTask = MonitorAsync(monitoringCancellation.Token);

		await WaitForFirstHealthy(cancellationToken);
	}

	private async Task WaitForFirstHealthy(CancellationToken cancellationToken)
	{
		if (options.Server.StartupTimeout > 0)
		{
			try
			{
				await firstHealthy.Task.WaitAsync(
					TimeSpan.FromSeconds(options.Server.StartupTimeout),
					cancellationToken);
			}
			catch (TimeoutException)
			{
				logger.LogServerStartupTimeout(ServerStatus.Url, options.Server.StartupTimeout);
				throw;
			}
		}
		else
		{
			await firstHealthy.Task.WaitAsync(cancellationToken);
		}
	}
```

- [ ] **Step 4: Preserve last-known healthy state**

In the same file, replace `CreateUnhealthyServerStatus`:

```csharp
	private PhoriaServerStatus CreateUnhealthyServerStatus()
	{
		PhoriaServerStatus last = ServerStatus;
		return new()
		{
			Health = PhoriaServerHealth.Unhealthy,
			Mode = last.Mode,
			Frameworks = last.Frameworks,
			Url = options.GetServerUrl()
		};
	}
```

- [ ] **Step 5: Add the log message**

Add to `PhoriaServerMonitorLogMessages` in the same file:

```csharp
	[LoggerMessage(
		EventId = EventId.Server.ServerStartupTimeout,
		Message = "Phoria server at {Url} did not become healthy within the {Seconds} second startup timeout.",
		Level = LogLevel.Error)]
	internal static partial void LogServerStartupTimeout(
		this ILogger logger,
		string url,
		int seconds);
```

- [ ] **Step 6: Restructure the monitor service**

Replace `packages/Phoria/Server/PhoriaServerMonitorService.cs` `ExecuteAsync`:

```csharp
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// StartMonitoring blocks until the server passes its first health check.

		try
		{
			await serverMonitor.StartMonitoring(stoppingToken);
			await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
		}
		catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
		{
			await serverMonitor.StopMonitoring();
		}
		catch
		{
			// e.g. the startup timeout: stop the monitor cleanly (cancels the poll loop and
			// disposes the timer/semaphore), then let the exception stop the host.
			await serverMonitor.StopMonitoring();
			throw;
		}
	}
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — both new tests, plus all existing monitor tests unchanged.

- [ ] **Step 8: Commit**

```bash
git add packages/Phoria/Server/PhoriaServerMonitor.cs packages/Phoria/Server/PhoriaServerMonitorService.cs packages/Phoria.Tests/Server/PhoriaServerMonitorTests.cs
git commit -m "feat(phoria): add monitor startup timeout and preserve last-known healthy state"
```

---

### Task 3: Component factory Degrade/Fail policy

**Files:**
- Modify: `packages/Phoria/Islands/PhoriaIslandComponentFactory.cs`
- Test: `packages/Phoria.Tests/Islands/PhoriaIslandSsrLifecycleTests.cs`

**Interfaces:**
- Consumes: `PhoriaOptions.Server.UnavailableBehavior`; `EventId.Islands.ServerUnhealthySuppressingComponent`.
- Produces: under `Fail` + unhealthy, both `Isomorphic` and `ServerOnly` islands throw `PhoriaIslandComponentException`; under `Degrade`, `Isomorphic` degrades to `ClientOnly` (unchanged) and `ServerOnly` logs `LogServerUnhealthySuppressingComponent` before throwing; `ClientOnly` unaffected in both.

- [ ] **Step 1: Write the failing tests**

Add to `packages/Phoria.Tests/Islands/PhoriaIslandSsrLifecycleTests.cs` (add `using Microsoft.Extensions.Logging;` at the top):

```csharp
[Fact]
public async Task ComponentFactory_FailPolicy_ThrowsForIsomorphicIslandWhenServerIsUnhealthy()
{
	var factory = new PhoriaIslandComponentFactory(
		new UnhealthyServerMonitor(),
		new PhoriaIslandScopedContext(),
		new TrackingSsr(),
		Options.Create(new PhoriaOptions
		{
			Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
		}),
		NullLogger<PhoriaIslandComponentFactory>.Instance);

	await Assert.ThrowsAsync<PhoriaIslandComponentException>(
		() => factory.CreateAsync("Example", null, new PhoriaIslandClientLoadDirective()));
}

[Fact]
public async Task ComponentFactory_DegradePolicy_LogsWarningBeforeThrowingForServerOnlyIsland()
{
	var logger = new ListLogger<PhoriaIslandComponentFactory>();
	var factory = new PhoriaIslandComponentFactory(
		new UnhealthyServerMonitor(),
		new PhoriaIslandScopedContext(),
		new TrackingSsr(),
		Options.Create(new PhoriaOptions()),
		logger);

	await Assert.ThrowsAsync<PhoriaIslandComponentException>(
		() => factory.CreateAsync("Example", null, null));

	Assert.Contains(logger.Messages, message => message.Contains("suppressing", StringComparison.Ordinal));
}
```

Add this nested `ListLogger<T>` class next to the other stubs at the bottom of the test class (before `HealthyServerMonitor`):

```csharp
	private sealed class ListLogger<T> : ILogger<T>
	{
		private readonly List<string> messages = [];

		public IReadOnlyList<string> Messages => messages;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			messages.Add(formatter(state, exception));
		}
	}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: `ComponentFactory_FailPolicy_ThrowsForIsomorphicIslandWhenServerIsUnhealthy` FAILS (currently degrades instead of throwing); `ComponentFactory_DegradePolicy_LogsWarningBeforeThrowingForServerOnlyIsland` FAILS (no log yet). Existing tests pass.

- [ ] **Step 3: Make the factory policy-aware**

In `packages/Phoria/Islands/PhoriaIslandComponentFactory.cs`, replace the unhealthy block inside `CreateAsync` (lines 47-58):

```csharp
		if (serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			bool isFail = options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail;

			if (renderMode == PhoriaIslandRenderMode.Isomorphic)
			{
				if (isFail)
				{
					throw new PhoriaIslandComponentException($"Cannot render component '{component}' on the server because the server is not healthy. Server status is '{serverMonitor.ServerStatus.Health}'.");
				}

				logger.LogServerUnhealthyDegradingToClient(component);
				renderMode = PhoriaIslandRenderMode.ClientOnly;
			}
			else if (renderMode == PhoriaIslandRenderMode.ServerOnly)
			{
				if (!isFail)
				{
					logger.LogServerUnhealthySuppressingComponent(component);
				}

				throw new PhoriaIslandComponentException($"Cannot render component '{component}' on the server because the server is not healthy. Server status is '{serverMonitor.ServerStatus.Health}'.");
			}
		}
```

- [ ] **Step 4: Add the log message**

Add to `PhoriaIslandComponentFactoryLogMessages` in the same file:

```csharp
	[LoggerMessage(
		EventId = EventId.Islands.ServerUnhealthySuppressingComponent,
		Message = "Phoria server is unhealthy; suppressing server-only component {Component}.",
		Level = LogLevel.Warning)]
	internal static partial void LogServerUnhealthySuppressingComponent(
		this ILogger logger,
		string component);
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — both new tests plus all existing factory/SSR lifecycle tests.

- [ ] **Step 6: Commit**

```bash
git add packages/Phoria/Islands/PhoriaIslandComponentFactory.cs packages/Phoria.Tests/Islands/PhoriaIslandSsrLifecycleTests.cs
git commit -m "feat(phoria): apply unavailable behavior policy in island component factory"
```

---

### Task 4: Policy-aware island tag helper

**Files:**
- Modify: `packages/Phoria/Islands/PhoriaIslandTagHelper.cs`
- Create: `packages/Phoria.Tests/Islands/PhoriaIslandTagHelperTests.cs`

**Interfaces:**
- Consumes: `PhoriaOptions.Server.UnavailableBehavior`.
- Produces: `PhoriaIslandTagHelper` constructor becomes `(IPhoriaIslandComponentFactory, IOptions<PhoriaOptions>)`; on `PhoriaIslandComponentException` it suppresses output under `Degrade` and rethrows under `Fail`. This resolves the `TODO: Log or throw exception?` (the factory now logs before throwing under `Degrade`).

- [ ] **Step 1: Write the failing test**

Create `packages/Phoria.Tests/Islands/PhoriaIslandTagHelperTests.cs`:

```csharp
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandTagHelperTests
{
	[Fact]
	public async Task Process_ComponentExceptionInDegradeMode_SuppressesOutput()
	{
		var tagHelper = new PhoriaIslandTagHelper(
			new ThrowingComponentFactory(),
			Options.Create(new PhoriaOptions()));

		TagHelperOutput output = CreateTagHelperOutput();

		await tagHelper.ProcessAsync(CreateTagHelperContext(), output);

		Assert.Null(output.TagName);
		Assert.Empty(output.Content.GetContent());
	}

	[Fact]
	public async Task Process_ComponentExceptionInFailMode_Rethrows()
	{
		var tagHelper = new PhoriaIslandTagHelper(
			new ThrowingComponentFactory(),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}));

		await Assert.ThrowsAsync<PhoriaIslandComponentException>(
			() => tagHelper.ProcessAsync(CreateTagHelperContext(), CreateTagHelperOutput()));
	}

	private static TagHelperContext CreateTagHelperContext() =>
		new(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

	private static TagHelperOutput CreateTagHelperOutput() =>
		new(
			"phoria-island",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

	private sealed class ThrowingComponentFactory : IPhoriaIslandComponentFactory
	{
		public Task<PhoriaIslandHtmlContent> CreateAsync(
			string component,
			object? props,
			PhoriaIslandClientDirective? client) =>
			throw new PhoriaIslandComponentException("Server is not healthy.");
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: compile error — `PhoriaIslandTagHelper` constructor does not accept `IOptions<PhoriaOptions>`.

- [ ] **Step 3: Make the tag helper policy-aware**

Replace `packages/Phoria/Islands/PhoriaIslandTagHelper.cs`:

```csharp
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Islands;

public class PhoriaIslandTagHelper(
	IPhoriaIslandComponentFactory phoriaIslandComponentFactory,
	IOptions<PhoriaOptions> options)
	: TagHelper
{
	private readonly IPhoriaIslandComponentFactory phoriaIslandComponentFactory = phoriaIslandComponentFactory;
	private readonly PhoriaOptions options = options.Value;

	public required string Component { get; set; }
	public object? Props { get; set; }
	public PhoriaIslandClientDirective? Client { get; set; }

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		try
		{
			PhoriaIslandHtmlContent content = await phoriaIslandComponentFactory.CreateAsync(
				Component,
				Props,
				Client);

			output.TagName = null;
			output.TagMode = TagMode.StartTagAndEndTag;

			output.Content.SetHtmlContent(content);
		}
		catch (PhoriaIslandComponentException)
		{
			// Under the Fail policy the page 500s instead of silently dropping the island; under
			// Degrade the factory has already logged, so suppressing the element is intentional.

			if (options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail)
			{
				throw;
			}

			output.SuppressOutput();

			return;
		}
	}
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — both new tests. No existing test constructs `PhoriaIslandTagHelper` directly (DI resolves it).

- [ ] **Step 5: Commit**

```bash
git add packages/Phoria/Islands/PhoriaIslandTagHelper.cs packages/Phoria.Tests/Islands/PhoriaIslandTagHelperTests.cs
git commit -m "feat(phoria): make island tag helper suppression policy-aware"
```

---

### Task 5: Middleware 503 behavior under Fail, with InternalsVisibleTo

**Files:**
- Modify: `packages/Phoria/Phoria.csproj`
- Modify: `packages/Phoria/Server/PhoriaServerMiddleware.cs`
- Create: `packages/Phoria.Tests/Server/PhoriaServerMiddlewareTests.cs`

**Interfaces:**
- Consumes: `PhoriaOptions.Server.UnavailableBehavior`; `EventId.Server.MiddlewareServerUnavailable`.
- Produces: `PhoriaServerMiddleware` constructor becomes `(ILogger<PhoriaServerMiddleware>, IPhoriaServerMonitor, IPhoriaServerHttpClientFactory, IOptions<PhoriaOptions>, RequestDelegate)`. Under `Fail`: unclaimed GETs return 503 when `ServerStatus.Health != Healthy`, and a mid-proxy `HttpRequestException` returns 503 instead of falling through. Under `Degrade`: both fall through as today. `Phoria.Tests` gains `InternalsVisibleTo` so the internal middleware can be unit-tested.

- [ ] **Step 1: Enable the test assembly visibility**

Add to `packages/Phoria/Phoria.csproj` (inside the last `ItemGroup`):

```xml
	<ItemGroup>
		<InternalsVisibleTo Include="Phoria.Tests" />
	</ItemGroup>
```

- [ ] **Step 2: Write the failing tests**

Create `packages/Phoria.Tests/Server/PhoriaServerMiddlewareTests.cs`:

```csharp
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class PhoriaServerMiddlewareTests
{
	[Fact]
	public async Task InvokeAsync_FailPolicyUnhealthy_Returns503ForUnclaimedGet()
	{
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new StubHttpClientFactory(),
			Options.Create(FailOptions()));

		DefaultHttpContext context = CreateGetContext("/unknown");
		bool nextCalled = false;

		await middleware.InvokeAsync(context, new StubHmrProxy());
		_ = nextCalled;

		Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
	}

	[Fact]
	public async Task InvokeAsync_DegradePolicyUnhealthy_FallsThroughForUnclaimedGet()
	{
		bool nextCalled = false;
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new StubHttpClientFactory(),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		DefaultHttpContext context = CreateGetContext("/unknown");

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.True(nextCalled);
	}

	[Fact]
	public async Task InvokeAsync_FailPolicyHealthy_ProxiesRequestAndDoesNotFallThrough()
	{
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("app-body")
			}),
			Options.Create(FailOptions()),
			_ => throw new InvalidOperationException("next must not be called on the healthy proxy path."));

		DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");
		context.Response.Body = new MemoryStream();

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.Equal("app-body", Encoding.UTF8.GetString(context.Response.Body.ToArray()));
	}

	[Fact]
	public async Task InvokeAsync_FailPolicyMidProxyException_Returns503()
	{
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(_ => throw new HttpRequestException("Connection refused (localhost)")),
			Options.Create(FailOptions()),
			_ => throw new InvalidOperationException("next must not be called in Fail mode."));

		DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
	}

	[Fact]
	public async Task InvokeAsync_DegradePolicyMidProxyException_FallsThrough()
	{
		bool nextCalled = false;
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(_ => throw new HttpRequestException("Connection refused (localhost)")),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.True(nextCalled);
	}

	private static PhoriaOptions FailOptions() => new()
	{
		Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
	};

	private static PhoriaServerMiddleware CreateMiddleware(
		IPhoriaServerMonitor serverMonitor,
		IPhoriaServerHttpClientFactory httpClientFactory,
		IOptions<PhoriaOptions> options,
		RequestDelegate? next = null) =>
		new(
			NullLogger<PhoriaServerMiddleware>.Instance,
			serverMonitor,
			httpClientFactory,
			options,
			next ?? (_ => Task.CompletedTask));

	private static DefaultHttpContext CreateGetContext(string path)
	{
		var context = new DefaultHttpContext();
		context.Request.Method = "GET";
		context.Request.Path = path;
		return context;
	}

	private sealed class StubServerMonitor(PhoriaServerHealth health) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = health,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}

	private sealed class StubHttpClientFactory : IPhoriaServerHttpClientFactory
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> send;

		public StubHttpClientFactory()
			: this(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
		{
		}

		public StubHttpClientFactory(HttpResponseMessage response)
			: this(_ => response)
		{
		}

		public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> send)
		{
			this.send = send;
		}

		public HttpClient CreateClient() => new(new StubHttpMessageHandler(send))
		{
			BaseAddress = new Uri("http://localhost:5173")
		};

		private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
		{
			protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken) =>
				Task.FromResult(send(request));
		}
	}

	private sealed class StubHmrProxy : IViteDevServerHmrProxy
	{
		public Task ProxyAsync(HttpContext context, CancellationToken cancellationToken) =>
			throw new InvalidOperationException("The HMR proxy should not be reached in these tests.");
	}
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: compile errors — `PhoriaServerMiddleware` is internal without `InternalsVisibleTo` (until Step 1 is in place) and the constructor signature differs. After Step 1, the tests compile and FAIL because the middleware has no policy logic.

- [ ] **Step 4: Add the options to the middleware**

In `packages/Phoria/Server/PhoriaServerMiddleware.cs`, add `using Microsoft.Extensions.Options;`, add the constructor parameter `IOptions<PhoriaOptions> options`, store `options.Value`, and restructure `InvokeAsync`:

```csharp
internal sealed class PhoriaServerMiddleware(
	ILogger<PhoriaServerMiddleware> logger,
	IPhoriaServerMonitor serverMonitor,
	IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory,
	IOptions<PhoriaOptions> options,
	RequestDelegate next)
{
	private readonly ILogger<PhoriaServerMiddleware> logger = logger;
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory = phoriaServerHttpClientFactory;
	private readonly PhoriaOptions options = options.Value;
	private readonly RequestDelegate next = next;

	/// <inheritdoc />
	public async Task InvokeAsync(
		HttpContext context,
		IViteDevServerHmrProxy viteDevServerHmrProxy)
	{
		// If the request doesn't have an endpoint, the request path is not null and the request method is GET, proxy the request to the server

		if (context.GetEndpoint() == null
			&& context.Request.Path.HasValue
			&& context.Request.Method == HttpMethod.Get.Method)
		{
			if (serverMonitor.ServerStatus.Health == PhoriaServerHealth.Healthy)
			{
				// If it's an HMR (hot module reload) request, delegate processing to a WebSocket proxy, otherwise, process the request via HTTP

				Task proxyRequest = ViteDevServerHmrProxy.IsHmrRequest(context)
					? viteDevServerHmrProxy.ProxyAsync(context, CancellationToken.None)
					: ProxyViaHttpAsync(context, next);

				await proxyRequest;
				return;
			}

			if (options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail)
			{
				logger.LogServerUnavailable(serverMonitor.ServerStatus.Url);
				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
				return;
			}
		}

		// If the request path is null, call the next middleware

		await next(context);
	}
```

In `ProxyViaHttpAsync`, replace the `catch (HttpRequestException ex)` block:

```csharp
		catch (HttpRequestException ex)
		{
			logger.LogMiddlewareProxyViaHttpError(requestUrl, ex);

			if (options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail)
			{
				logger.LogServerUnavailable(serverMonitor.ServerStatus.Url);
				context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
				return;
			}

			await next(context);
		}
```

- [ ] **Step 5: Add the log message**

Add to `PhoriaServerMiddlewareLogMessages` in the same file:

```csharp
	[LoggerMessage(
		EventId = EventId.Server.MiddlewareServerUnavailable,
		Message = "Phoria server at {Url} is unavailable; returning 503.",
		Level = LogLevel.Warning)]
	internal static partial void LogServerUnavailable(
		this ILogger logger,
		string url);
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — all five new middleware tests.

- [ ] **Step 7: Commit**

```bash
git add packages/Phoria/Phoria.csproj packages/Phoria/Server/PhoriaServerMiddleware.cs packages/Phoria.Tests/Server/PhoriaServerMiddlewareTests.cs
git commit -m "feat(phoria): return 503 from middleware under Fail policy when server is unavailable"
```

---

### Task 6: Bounded process restarts

**Files:**
- Modify: `packages/Phoria/Server/PhoriaServerProcess.cs`
- Test: `packages/Phoria.Tests/Server/PhoriaServerProcessTests.cs`

**Interfaces:**
- Consumes: `PhoriaOptions.Server.Process.MaxRestartAttempts`; `EventId.Server.ServerRestartLimitExceeded`.
- Produces: `PhoriaServerProcess` restarts a process at most `MaxRestartAttempts` times (0 = unlimited); the restart counter resets to 0 whenever the monitor reports healthy; on exhaustion it logs `LogServerRestartLimitExceeded`, ends the supervision loop, and `StartServer` returns **without** terminating any live process. `EnsureProcessIsRunning` becomes `Task<bool>` (returns `false` to end supervision).

- [ ] **Step 1: Write the failing tests**

Add to `packages/Phoria.Tests/Server/PhoriaServerProcessTests.cs`:

```csharp
[Fact]
public async Task StartServer_RestartLimitExceeded_StopsSpawningAndEndsSupervisionLoop()
{
	var hookCalls = 0;
	using var cts = new CancellationTokenSource();
	using PhoriaServerProcess serverProcess = CreateServerProcess(
		script: "process.exit(0);",
		beforeProcessIdAssignment: _ => Interlocked.Increment(ref hookCalls),
		maxRestartAttempts: 1);

	// Give-up returns normally (no throw) once the limit is exceeded.
	await serverProcess.StartServer(cts.Token).WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

	// Initial spawn + 1 restart, then the supervision loop gives up.
	Assert.Equal(2, hookCalls);
}

[Fact]
public async Task StartServer_RestartLimit_ResetsAttemptsWhenServerBecomesHealthy()
{
	var monitor = new SettableServerMonitor();
	var hookCalls = 0;
	using var cts = new CancellationTokenSource();
	using PhoriaServerProcess serverProcess = CreateServerProcess(
		monitor: monitor,
		script: "process.exit(0);",
		beforeProcessIdAssignment: _ => Interlocked.Increment(ref hookCalls),
		maxRestartAttempts: 1);

	Task startTask = serverProcess.StartServer(cts.Token);

	await WaitUntilAsync(() => hookCalls >= 1, TimeSpan.FromSeconds(10));

	// Becoming healthy must reset the restart counter: with max=1 and a single healthy blip the
	// supervision loop can spawn at least one more process than the raw limit would allow.
	monitor.ServerStatus = monitor.ServerStatus with { Health = PhoriaServerHealth.Healthy };
	await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
	monitor.ServerStatus = monitor.ServerStatus with { Health = PhoriaServerHealth.Unhealthy };

	await startTask.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

	Assert.True(hookCalls >= 3, $"Expected at least 3 spawns after a healthy reset, got {hookCalls}.");
}

[Fact]
public async Task StartServer_SupervisionLoop_DoesNotKillRunningProcess()
{
	if (OperatingSystem.IsWindows())
	{
		Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
	}

	string markerPath = CreateMarkerPath(nameof(StartServer_SupervisionLoop_DoesNotKillRunningProcess));
	using var child = StartNode(LongLivedNodeScript(markerPath));
	await WaitForMarker(markerPath, "ready");

	// Seed the live process as the supervised process; while it runs, the loop must never terminate it.
	using PhoriaServerProcess serverProcess = CreateServerProcess(
		processId: child.Id,
		maxRestartAttempts: 1);
	using var cts = new CancellationTokenSource();
	Task startTask = serverProcess.StartServer(cts.Token);

	await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
	Assert.False(child.HasExited, "The supervision loop must not terminate a running process.");

	cts.Cancel();
	await Assert.ThrowsAnyAsync<OperationCanceledException>(
		() => startTask.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));

	Assert.True(child.HasExited);
}
```

Update the test helpers in the same file:

- Replace `CreateServerProcess` with an overload that accepts a monitor and a restart limit, and add `SettableServerMonitor`:

```csharp
	private static PhoriaServerProcess CreateServerProcess(
		int? processId = null,
		TimeSpan? stopGracePeriod = null,
		string? script = null,
		Action<int>? beforeProcessIdAssignment = null,
		IPhoriaServerMonitor? monitor = null,
		int maxRestartAttempts = 0)
	{
		return new PhoriaServerProcess(
			NullLogger<PhoriaServerProcess>.Instance,
			monitor ?? new StubServerMonitor(),
			new StubHostEnvironment(),
			Options.Create(CreateProcessOptions(script ?? "setInterval(() => {}, 1000);", maxRestartAttempts)),
			processId,
			stopGracePeriod ?? TimeSpan.FromSeconds(6),
			beforeProcessIdAssignment);
	}
```

- Replace `CreateProcessOptions` to set a fast loop cadence and the limit:

```csharp
	private static PhoriaOptions CreateProcessOptions(string script, int maxRestartAttempts = 0)
	{
		return new PhoriaOptions
		{
			Server = new PhoriaServerOptions
			{
				Process = new PhoriaServerOptions.ProcessOptions
				{
					Command = "node",
					Arguments = ["-e", script],
					HealthCheckInterval = 1,
					MaxRestartAttempts = maxRestartAttempts
				}
			}
		};
	}
```

- Add `LongLivedNodeScript` and `WaitUntilAsync` and `SettableServerMonitor`:

```csharp
	private static string LongLivedNodeScript(string markerPath) =>
		$$"""
		require('fs').writeFileSync("{{markerPath}}", 'ready');
		setInterval(() => {}, 1000);
		""";

	private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow.Add(timeout);
		while (!condition())
		{
			if (DateTime.UtcNow >= deadline)
			{
				throw new TimeoutException($"Condition was not met within {timeout}.");
			}

			await Task.Delay(25);
		}
	}
```

```csharp
	private sealed class SettableServerMonitor : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; set; } = new()
		{
			Health = PhoriaServerHealth.Unhealthy,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopMonitoring() => Task.CompletedTask;
	}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: `StartServer_RestartLimitExceeded_StopsSpawningAndEndsSupervisionLoop` FAILS by timeout (the loop restarts forever with an immediate-exit script); the other two new tests may hang until the process tests' overall timeout. Existing tests pass.

- [ ] **Step 3: Track restarts and gate spawning**

In `packages/Phoria/Server/PhoriaServerProcess.cs`:
- Add a field: `private int restartAttempts;`
- Change `EnsureProcessIsRunning` to return `Task<bool>`. At the top (healthy branch) reset the counter and return `true`; in the "current process running" branch return `true`; before spawning, add the limit check; on `ExitedCommandEvent`, increment the counter when the server is unhealthy; return `true` at the end.

```csharp
	private async Task<bool> EnsureProcessIsRunning(
		PhoriaServerOptions.ProcessOptions processOptions,
		SemaphoreSlim serverSemaphore,
		CancellationToken cancellationToken)
	{
		if (serverMonitor.ServerStatus.Health == PhoriaServerHealth.Healthy)
		{
			restartAttempts = 0;
			logger.LogServerProcessIsHealthy();

			return true;
		}

		int? currentProcessId;
		lock (sync)
		{
			currentProcessId = processId;
		}

		if (currentProcessId.HasValue)
		{
			var process = Process.GetProcessById(currentProcessId.Value);

			if (process.HasExited)
			{
				lock (sync)
				{
					processId = null;
				}
			}
			else
			{
				logger.LogServerProcessIsRunning(currentProcessId.Value);

				return true;
			}
		}

		if (processOptions.MaxRestartAttempts > 0
			&& restartAttempts > processOptions.MaxRestartAttempts)
		{
			logger.LogServerRestartLimitExceeded(processOptions.MaxRestartAttempts);

			return false;
		}

		if (await serverSemaphore.WaitAsync(0, cancellationToken))
		{
			// ... existing spawn body unchanged, except the ExitedCommandEvent case becomes: ...
						case ExitedCommandEvent exited:
							lock (sync)
							{
								processId = null;
								startCompletion.TrySetResult(null);
								if (serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
								{
									restartAttempts++;
								}
							}

							logger.LogServerProcessExited(exited.ExitCode);
							break;
			// ...
		}

		return true;
	}
```

- [ ] **Step 4: End the supervision loop on give-up without killing a live process**

In `StartServer`, wrap the initial `EnsureProcessIsRunning` call and the `while` loop, and guard the `finally`'s `StopServer()`:

```csharp
		PeriodicTimer? timer = null;
		bool restartLimitReached = false;

		try
		{
			// Start the process

			restartLimitReached = !await EnsureProcessIsRunning(options.Server.Process, serverSemaphore, linkedStopping.Token);

			if (!restartLimitReached)
			{
				lock (sync)
				{
					if (stopping)
					{
						linkedStopping.Token.ThrowIfCancellationRequested();
						return;
					}

					periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.Server.Process.HealthCheckInterval));
					timer = periodicTimer;
				}

				while (await timer.WaitForNextTickAsync(linkedStopping.Token))
				{
					restartLimitReached = !await EnsureProcessIsRunning(options.Server.Process, serverSemaphore, linkedStopping.Token);

					if (restartLimitReached)
					{
						break;
					}
				}
			}
		}
		finally
		{
			// The give-up path only runs after the supervised process has already exited (the counter
			// increments on exit), so StopServer would be a no-op — but a live process must never be
			// terminated by the give-up path, so skip it there entirely.
			if (!restartLimitReached)
			{
				await StopServer();
			}

			lock (sync)
			{
				if (ReferenceEquals(stopSignal, serverStopSignal))
				{
					stopSignal = null;
				}

				if (ReferenceEquals(semaphore, serverSemaphore))
				{
					semaphore = null;
				}

				if (ReferenceEquals(periodicTimer, timer))
				{
					periodicTimer = null;
				}
			}

			timer?.Dispose();
			serverSemaphore.Dispose();
		}
```

- [ ] **Step 5: Add the log message**

Add to `PhoriaServerProcessLogMessages` in the same file:

```csharp
	[LoggerMessage(
		EventId = EventId.Server.ServerRestartLimitExceeded,
		Message = "Phoria server process restart limit of {MaxRestartAttempts} exceeded; stopping supervision.",
		Level = LogLevel.Error)]
	internal static partial void LogServerRestartLimitExceeded(
		this ILogger logger,
		int maxRestartAttempts);
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — the three new process tests plus all existing process tests. (On Windows the two SIGTERM-dependent tests self-skip as before.)

- [ ] **Step 7: Commit**

```bash
git add packages/Phoria/Server/PhoriaServerProcess.cs packages/Phoria.Tests/Server/PhoriaServerProcessTests.cs
git commit -m "feat(phoria): bound phoria server process restarts"
```

---

### Task 7: Phoria Server health check

**Files:**
- Create: `packages/Phoria/Health/PhoriaServerHealthCheck.cs`
- Create: `packages/Phoria/Health/PhoriaHealthChecksBuilderExtensions.cs`
- Create: `packages/Phoria.Tests/Health/PhoriaServerHealthCheckTests.cs`

**Interfaces:**
- Consumes: `IPhoriaServerMonitor.ServerStatus`; `PhoriaOptions.Server.UnavailableBehavior`.
- Produces: `PhoriaServerHealthCheck : IHealthCheck`; `PhoriaHealthChecksBuilderExtensions.AddPhoriaServerHealthCheck(this IHealthChecksBuilder builder, string name = "phoria-server")` (namespace `Phoria`). Consumers opt in with `AddHealthChecks().AddPhoriaServerHealthCheck()` and `MapHealthChecks("/health")`.

- [ ] **Step 1: Write the failing tests**

Create `packages/Phoria.Tests/Health/PhoriaServerHealthCheckTests.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Phoria.Health;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Health;

public class PhoriaServerHealthCheckTests
{
	[Fact]
	public async Task CheckHealthAsync_HealthyServer_ReportsHealthy()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			Options.Create(new PhoriaOptions()));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Healthy, result.Status);
	}

	[Fact]
	public async Task CheckHealthAsync_UnhealthyServerUnderDegradePolicy_ReportsDegraded()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			Options.Create(new PhoriaOptions()));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Degraded, result.Status);
	}

	[Fact]
	public async Task CheckHealthAsync_UnhealthyServerUnderFailPolicy_ReportsUnhealthy()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
	}

	[Fact]
	public async Task CheckHealthAsync_UnknownServerUnderFailPolicy_ReportsUnhealthy()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Unknown),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
	}

	private sealed class StubServerMonitor(PhoriaServerHealth health) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = health,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: compile error — `PhoriaServerHealthCheck` does not exist.

- [ ] **Step 3: Implement the health check**

Create `packages/Phoria/Health/PhoriaServerHealthCheck.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Health;

public sealed class PhoriaServerHealthCheck(
	IPhoriaServerMonitor serverMonitor,
	IOptions<PhoriaOptions> options)
	: IHealthCheck
{
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly PhoriaOptions options = options.Value;

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		PhoriaServerStatus status = serverMonitor.ServerStatus;

		if (status.Health == PhoriaServerHealth.Healthy)
		{
			return Task.FromResult(HealthCheckResult.Healthy($"Phoria server at {status.Url} is healthy."));
		}

		string description = $"Phoria server at {status.Url} is {status.Health.ToString().ToLowerInvariant()}.";
		HealthCheckResult result = options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail
			? HealthCheckResult.Unhealthy(description)
			: HealthCheckResult.Degraded(description);

		return Task.FromResult(result);
	}
}
```

- [ ] **Step 4: Implement the registration extension**

Create `packages/Phoria/Health/PhoriaHealthChecksBuilderExtensions.cs`:

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Phoria.Health;

namespace Phoria;

public static class PhoriaHealthChecksBuilderExtensions
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

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — all four new health-check tests.

- [ ] **Step 6: Commit**

```bash
git add packages/Phoria/Health/ packages/Phoria.Tests/Health/
git commit -m "feat(phoria): add phoria server health check"
```

---

### Task 8: Suppress entry tag output while unhealthy

**Files:**
- Modify: `packages/Phoria/Islands/PhoriaIslandEntryTagHelper.cs`
- Test: `packages/Phoria.Tests/Islands/PhoriaIslandEntryTagHelperTests.cs`

**Interfaces:**
- Consumes: `EventId.Islands.EntryTagsSuppressedWhileUnhealthy`.
- Produces: `PhoriaIslandEntryTagHelper.Process` suppresses script/link output and logs `LogEntryTagsSuppressedWhileUnhealthy` when `ServerStatus.Health != Healthy`. This removes the dev-URL-in-production path (an unhealthy status previously left `Mode` at its `Development` default, emitting `{serverUrl}/@vite/client` in production).

- [ ] **Step 1: Write the failing test and update existing stubs**

In `packages/Phoria.Tests/Islands/PhoriaIslandEntryTagHelperTests.cs`, update the existing `StubServerMonitor` so the healthy-path tests construct a healthy status (the record's `Health` default is `Unknown`, which would now be suppressed):

```csharp
	private sealed class StubServerMonitor(PhoriaServerMode mode) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Healthy,
			Mode = mode,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}
```

Add a new test (plus an `UnhealthyServerMonitor` stub):

```csharp
	[Fact]
	public void Process_ServerUnhealthy_SuppressesOutputAndLogsWarning()
	{
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			new UnhealthyServerMonitor(),
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaSrc = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = CreateTagHelperContext();
		TagHelperOutput output = CreateTagHelperOutput("script");

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Contains(logger.Messages, message => message.Contains("suppressed", StringComparison.Ordinal));
		Assert.Empty(output.Content.GetContent());
		Assert.Null(output.Attributes["src"]);
	}
```

And add the stub next to `StubServerMonitor`:

```csharp
	private sealed class UnhealthyServerMonitor : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Unhealthy,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: the new suppression test FAILS (output is emitted, no log); the existing tests FAIL if the stub update was missed (they would now hit suppression with `Health == Unknown`).

- [ ] **Step 3: Suppress output while unhealthy**

In `packages/Phoria/Islands/PhoriaIslandEntryTagHelper.cs`, insert this block in `Process` right after the "If the value is empty or null" guard and before `value = value.TrimStart('~', '/');`:

```csharp
		// While the server is unhealthy its status can't be trusted (an unknown status defaults to
		// Development), which would otherwise emit dev URLs in production. Suppress the element instead.

		if (serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			logger.LogEntryTagsSuppressedWhileUnhealthy(ViewContext.View.Path);
			output.SuppressOutput();
			return;
		}
```

- [ ] **Step 4: Add the log message**

Add to `PhoriaIslandEntryTagHelperLogMessages` in the same file:

```csharp
	[LoggerMessage(
		EventId = EventId.Islands.EntryTagsSuppressedWhileUnhealthy,
		Message = "Phoria server is unhealthy; suppressing entry tags (check {View}).",
		Level = LogLevel.Warning)]
	internal static partial void LogEntryTagsSuppressedWhileUnhealthy(
		this ILogger logger,
		string view);
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --project packages/Phoria.Tests/Phoria.Tests.csproj --configuration Release`
Expected: PASS — the new test plus all existing entry/scripts/styles tag helper tests.

- [ ] **Step 6: Commit**

```bash
git add packages/Phoria/Islands/PhoriaIslandEntryTagHelper.cs packages/Phoria.Tests/Islands/PhoriaIslandEntryTagHelperTests.cs
git commit -m "fix(phoria): suppress entry tags while server is unhealthy"
```

---

### Task 9: Example app updates

**Files:**
- Modify: `examples/getting-started/WebApp/Program.cs`
- Modify: `examples/framework-multiple/WebApp/Program.cs`
- Modify: `examples/getting-started/WebApp/appsettings.Production.json`
- Modify: `examples/framework-multiple/WebApp/appsettings.Production.json`
- Modify: `examples/getting-started/WebApp/appsettings.Preview.json`
- Modify: `examples/framework-multiple/WebApp/appsettings.Preview.json`
- Modify: `examples/getting-started/WebApp/ui/tests/e2e/smoke.test.ts`
- Modify: `examples/framework-multiple/WebApp/ui/tests/e2e/smoke.test.ts`

**Interfaces:**
- Consumes: `AddPhoriaServerHealthCheck` (Task 7).

- [ ] **Step 1: Wire the health check into both WebApps**

In both `examples/*/WebApp/Program.cs`:
- Add `using Microsoft.AspNetCore.Diagnostics.HealthChecks;` to the usings.
- After `builder.Services.AddPhoria();` add:

```csharp
builder.Services.AddHealthChecks().AddPhoriaServerHealthCheck();
```

- After `app.UseRouting();` add:

```csharp
app.MapHealthChecks("/health");
```

- [ ] **Step 2: Configure the new production options**

In both `examples/*/WebApp/appsettings.Production.json`, replace the `server` block so it contains the fail-fast policy alongside the existing process config:

```json
    "server": {
      "startupTimeout": 60,
      "unavailableBehavior": "Fail",
      "process": {
        "command": "node",
        "arguments": ["ui/dist/server/server.js"],
        "maxRestartAttempts": 5
      }
    }
```

- [ ] **Step 3: Configure the new preview options**

In both `examples/*/WebApp/appsettings.Preview.json`, add the fail-fast policy to the `server` block (no `process` — the AppHost owns Node in Preview):

```json
    "server": {
      "https": false,
      "startupTimeout": 60,
      "unavailableBehavior": "Fail"
    }
```

- [ ] **Step 4: Extend the e2e smoke tests**

In both `examples/*/WebApp/ui/tests/e2e/smoke.test.ts`, add a health-check assertion:

```typescript
  it("reports the Phoria server healthy via the health check", async () => {
    const response = await fetch(`${webAppUrl}/health`)

    expect(response.status).toBe(200)
    expect(await response.text()).toContain("Healthy")
  })
```

- [ ] **Step 5: Verify the examples build and e2e pass**

The examples are linked to in-repo packages (the current working tree already has `file:` refs + `ProjectReference`). Rebuild the repo root first, then run one example's e2e against a running Preview build:

```bash
pnpm build
cd examples/framework-multiple/WebApp && pnpm build && pnpm test:e2e
```

Expected: the build succeeds and the smoke suite passes including the new `/health` assertion. (Run both examples if you have both running.)

- [ ] **Step 6: Commit**

```bash
git add examples/
git commit -m "feat(examples): wire phoria server health check and fail-fast policy"
```

---

### Task 10: Documentation and changeset

**Files:**
- Modify: `docs/ARCHITECTURE.md`
- Modify: `docs/MEMORY.md`
- Modify: `docs/guides/deployment.md`
- Create: `.changeset/unavailable-server-policy.md`

**Interfaces:**
- Consumes: all behaviors from Tasks 1-9.

- [ ] **Step 1: Update the options table**

In `docs/ARCHITECTURE.md`, add rows to the options table (after `Server.HealthCheckTimeout`):

```markdown
| `Server.StartupTimeout` | `0` | Seconds to wait for the first healthy check before failing startup; `0` waits indefinitely |
| `Server.UnavailableBehavior` | `Degrade` | `Degrade` or `Fail` — behavior when the Phoria Server is unavailable |
| `Server.Process.MaxRestartAttempts` | `0` | Max process restarts while unhealthy; `0` restarts indefinitely |
```

- [ ] **Step 2: Update the middleware / process / monitor bullets**

In `docs/ARCHITECTURE.md`, update the middleware bullet (line ~117) to mention the 503 behavior, the `PhoriaServerProcess` bullet (line ~125) to mention the restart limit, and the `PhoriaServerMonitor` bullet (line ~124) to mention the startup timeout and preserved `Mode`/`Frameworks`. Then rewrite the "Health, startup, and degradation" section (line ~423):

```markdown
### Health, startup, and degradation

The `PhoriaServerMonitorService` starts on host startup and blocks until the first successful `GET /hc` — or, when `Server.StartupTimeout` is set, fails startup after that many seconds. `PhoriaServerProcess` (when configured) supervises Node in production, restarting it at most `Server.Process.MaxRestartAttempts` times (0 = indefinitely) while unhealthy. After startup the monitor refreshes `PhoriaServerStatus` every `HealthCheckInterval` seconds, preserving the last-known healthy `Mode`/`Frameworks` through a downtime. If the server goes unhealthy, behavior is a consumer choice:

- **`Degrade`** (default): `Isomorphic` islands degrade to `ClientOnly` and resume SSR once healthy; `ServerOnly` islands throw `PhoriaIslandComponentException` (logged, then suppressed by the tag helper); entry tags are suppressed so no dev URLs leak into production; unclaimed GETs fall through to normal 404 handling.
- **`Fail`**: `Isomorphic` and `ServerOnly` islands throw (page 500s), unclaimed GETs return 503, and a proxy failure mid-request returns 503.

Consumers can opt in to an orchestrator-facing health check with `AddHealthChecks().AddPhoriaServerHealthCheck()` and `MapHealthChecks("/health")` — it reports `Healthy`, `Degraded` (under `Degrade`), or `Unhealthy` (under `Fail`) based on the monitor status.
```

- [ ] **Step 3: Add the MEMORY entry**

Append to `docs/MEMORY.md`:

```markdown
## 2026-08-11 — Configurable Phoria Server unavailable policy

- New `PhoriaServerUnavailableBehavior` (`Degrade`/`Fail`) plus `Server.StartupTimeout` (seconds, 0 = wait indefinitely) and `Server.Process.MaxRestartAttempts` (0 = unlimited), all defaulting to today's behavior and all .NET-only (the Node appsettings parser ignores unknown keys).
- `Fail` makes unavailability explicit: islands throw (page 500), unclaimed GETs return 503, and the process supervisor gives up after the restart limit; `Degrade` keeps best-effort serving with the server-only suppression now logged instead of silent.
- Startup fail-fast is a monitor concern (a `WaitAsync` timeout on `firstHealthy`); the monitor service stops the monitor cleanly then rethrows, so the host stops under `BackgroundServiceExceptionBehavior.StopHost`.
- The monitor preserves the last-known healthy `Mode`/`Frameworks` through downtime, and the entry tag helpers suppress asset output while unhealthy (fixes dev URLs leaking into production when status defaults to `Development`).
- New opt-in `AddPhoriaServerHealthCheck` (`IHealthCheck` reporting `Healthy`/`Degraded`/`Unhealthy` per policy) wired into both examples alongside `/health`, with a `Fail` + `startupTimeout: 60` production/preview policy so orchestrators can restart the app when recovery has failed.
- The restart counter is a plain count reset on healthy; a time-window bound was considered and rejected for v1.
```

- [ ] **Step 4: Add the deployment guide note**

In `docs/guides/deployment.md`, after the existing `phoria:server.process` JSON example (line ~144), add:

```markdown
For a fail-fast deployment you can also set the startup timeout, the unavailable behavior, and a restart limit alongside the process config:

```json
{
  "phoria": {
    "server": {
      "startupTimeout": 60,
      "unavailableBehavior": "Fail",
      "process": {
        "command": "node",
        "arguments": ["WebApp/ui/dist/server/server.js"],
        "maxRestartAttempts": 5
      }
    }
  }
}
```

With `unavailableBehavior: "Fail"` the WebApp reports the server status through the opt-in `/health` check, so an orchestrator (Kubernetes, Azure Container Apps, Docker) can restart the container when recovery has failed.
```
```

- [ ] **Step 5: Add the changeset**

Create `.changeset/unavailable-server-policy.md`:

```markdown
---
phoria-dotnet: minor
---

Configurable Phoria Server unavailable policy (`Degrade`/`Fail`), a startup timeout, bounded process restarts, an opt-in `AddPhoriaServerHealthCheck`, and correctness fixes that keep entry tags and last-known status sane while the server is down.
```

- [ ] **Step 6: Verify docs build and lint**

Run: `pnpm lint` at the repo root.
Expected: Biome passes (Markdown is not linted by Biome, but the command covers any touched JSON/TS from Task 9).

- [ ] **Step 7: Commit**

```bash
git add docs/ARCHITECTURE.md docs/MEMORY.md docs/guides/deployment.md .changeset/unavailable-server-policy.md
git commit -m "docs: document phoria server unavailable policy and health check"
```

---

## Self-Review

**Spec coverage:**
- Configuration surface (enum + 3 options, binding keys, JS-side note) → Task 1.
- Startup fail-fast (monitor timeout, `WaitAsync`, log, monitor-service restructure) → Task 2.
- Degrade vs Fail in `PhoriaIslandComponentFactory` (+ new warning log) → Task 3.
- Policy-aware `PhoriaIslandTagHelper` catch → Task 4.
- Middleware 503 in `Fail` (unclaimed GET + mid-proxy exception), `Degrade` fall-through → Task 5.
- Bounded recovery (counter reset on healthy, increment per exit, limit, no kill on give-up, monitor keeps polling) → Task 6.
- Health check (`PhoriaServerHealthCheck` + `AddPhoriaServerHealthCheck`) → Task 7.
- Correctness fixes (entry tag suppression, preserve last-known `Mode`/`Frameworks`) → Tasks 2 & 8.
- Example app updates (Program.cs, Production/Preview appsettings, e2e `/health`) → Task 9.
- Documentation (ARCHITECTURE, MEMORY, deployment guide) → Task 10.
- Changeset: `phoria-dotnet` minor → Task 10.
- Testing section: monitor timeout + preserve tests (Task 2); process restart tests (Task 6); factory Fail/Degrade tests (Task 3); tag helper Degrade/Fail tests (Task 4); middleware tests incl. mid-proxy exception (Task 5); health check status mapping (Task 7); entry helper suppression test + healthy-path stub fix (Task 8); framework-multiple `/health` e2e (Task 9). The spec's "stop the sidecar mid-run" e2e is deliberately out of scope (unit-tested instead).

**Placeholder scan:** every task has concrete file paths, code, and pass/fail expectations; no TBDs. The one test that is timing-sensitive (`StartServer_RestartLimit_ResetsAttemptsWhenServerBecomesHealthy`) uses a `>=` assertion on spawn count with generous timeouts so it only fails if the reset genuinely did not happen.

**Type consistency:** `PhoriaServerUnavailableBehavior`, `StartupTimeout`, `MaxRestartAttempts`, and the five EventId constants are defined in Task 1 and used verbatim in Tasks 2-8. `EnsureProcessIsRunning` returns `Task<bool>` consistently in Task 6. `AddPhoriaServerHealthCheck` signature matches Task 7 and its usage in Task 9.
