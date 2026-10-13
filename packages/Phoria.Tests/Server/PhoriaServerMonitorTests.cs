using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Xunit;
using static Phoria.Tests.TestUtilities.AsyncTestWaits;

namespace Phoria.Tests.Server;

public class PhoriaServerMonitorTests
{
	[Theory]
	[InlineData(false, "localhost", null, "http://localhost")]
	[InlineData(false, "localhost", (ushort)80, "http://localhost:80")]
	[InlineData(true, "example.test", null, "https://example.test")]
	[InlineData(true, "example.test", (ushort)443, "https://example.test:443")]
	public void ServerStatus_UsesConfiguredServerUrl(bool https, string host, ushort? port, string expectedUrl)
	{
		var options = new PhoriaOptions { Server = new PhoriaServerOptions { Https = https, Host = host, Port = port } };
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			new StubHttpClientFactory(HttpStatusCode.OK),
			Options.Create(new PhoriaObservabilityOptions()));

		Assert.Equal(expectedUrl, monitor.ServerStatus.Url);
	}

	[Fact]
	public async Task StartMonitoring_CompletesAfterFirstHealthyCheck()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			new StubHttpClientFactory(HttpStatusCode.OK),
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await startTask.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

		Assert.Equal(PhoriaServerHealth.Healthy, monitor.ServerStatus.Health);
		cancellation.Cancel();
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task StartMonitoring_CancellationStopsUnhealthyStartupWait()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var factory = new ScriptedHttpClientFactory(_ => UnhealthyResponse());
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			factory,
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);
		await WaitUntilAsync(() => factory.RequestCount >= 1, TimeSpan.FromSeconds(3));
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startTask);
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task StartMonitoring_FaultsFirstHealthyGateWhenStartupFails()
	{
		var options = new PhoriaOptions();
		var failure = new InvalidOperationException("health check setup failed");
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			new ThrowingHttpClientFactory(failure),
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await Assert.ThrowsAsync<InvalidOperationException>(() => startTask.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task Monitor_RecoversAndAllowsIsomorphicSsrAfterHealthyPoll()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			new ChangingHealthHttpClientFactory(),
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();
		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await startTask.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

		Assert.Equal(PhoriaServerHealth.Healthy, monitor.ServerStatus.Health);
		var ssr = new TrackingSsr();
		var factory = new PhoriaIslandComponentFactory(
			monitor,
			new PhoriaIslandScopedContext(),
			ssr,
			Options.Create(new PhoriaOptions()),
			NullLogger<PhoriaIslandComponentFactory>.Instance);

		_ = await factory.CreateAsync("Example", null, new PhoriaIslandClientLoadDirective());

		Assert.Equal(1, ssr.CallCount);
		factory.Dispose();
		cancellation.Cancel();
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task StartMonitoring_LogsNotReadyYet_NotUnhealthy_BeforeFirstHealthyCheck()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			new ScriptedHttpClientFactory(i => i == 1 ? UnhealthyResponse() : HealthyResponse()),
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await startTask.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

		Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("is not ready yet."));
		Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
		cancellation.Cancel();
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task StartMonitoring_LogsNotReadyYet_NotUnhealthy_WhenStartupCheckThrows()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			new ScriptedHttpClientFactory(i => i == 1 ? ThrowResponse() : HealthyResponse()),
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await startTask.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

		Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("is not ready yet."));
		Assert.DoesNotContain(logger.Entries, e => e.Level == LogLevel.Error);
		cancellation.Cancel();
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task Monitor_LogsUnhealthyAsError_AfterFirstHealthyCheck()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var observability = new PhoriaObservabilityOptions { LogHealthChecks = true };
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			new ScriptedHttpClientFactory(i => i == 1 ? HealthyResponse() : UnhealthyResponse()),
			Options.Create(observability));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await startTask.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

		await WaitUntilAsync(
			() => logger.Entries.Any(e => e.Level == LogLevel.Error),
			TimeSpan.FromSeconds(3));

		Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("is unhealthy."));
		cancellation.Cancel();
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task Monitor_LogsUnhealthyAsError_WhenCheckThrowsAfterHealthy()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var observability = new PhoriaObservabilityOptions { LogHealthChecks = true };
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			new ScriptedHttpClientFactory(i => i == 1 ? HealthyResponse() : ThrowResponse()),
			Options.Create(observability));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);

		await startTask.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

		await WaitUntilAsync(
			() => logger.Entries.Any(e => e.Level == LogLevel.Error),
			TimeSpan.FromSeconds(3));

		Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("is unhealthy."));
		cancellation.Cancel();
		await monitor.StopMonitoring();
	}

	[Fact]
	public async Task Monitor_LogsHealthyOnceForRepeatedHealthyChecks_WhenHealthCheckLoggingIsDisabled()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var factory = new ScriptedHttpClientFactory(_ => HealthyResponse());
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			factory,
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		try
		{
			await monitor.StartMonitoring(cancellation.Token);
			await WaitUntilAsync(() => factory.RequestCount >= 2, TimeSpan.FromSeconds(3));

			Assert.Equal(1, logger.Entries.Count(e => e.Level == LogLevel.Debug && e.Message.Contains("is healthy.")));
		}
		finally
		{
			cancellation.Cancel();
			await monitor.StopMonitoring();
		}
	}

	[Fact]
	public async Task Monitor_LogsOnlyStatusTransitions_WhenHealthCheckLoggingIsDisabled()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var factory = new ScriptedHttpClientFactory(i => i switch
		{
			1 or 4 or 5 => UnhealthyResponse(),
			_ => HealthyResponse()
		});
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			factory,
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		try
		{
			await monitor.StartMonitoring(cancellation.Token);
			await WaitUntilAsync(() => factory.RequestCount >= 6, TimeSpan.FromSeconds(8));

			Assert.Equal(1, logger.Entries.Count(e => e.Message.Contains("is not ready yet.")));
			Assert.Equal(2, logger.Entries.Count(e => e.Message.Contains("is healthy.")));
			Assert.Equal(1, logger.Entries.Count(e => e.Message.Contains("is unhealthy.")));
		}
		finally
		{
			cancellation.Cancel();
			await monitor.StopMonitoring();
		}
	}

	[Fact]
	public async Task StartMonitoring_ThrowsTimeoutException_WhenStartupTimeoutElapses()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		options.Server.StartupTimeout = 1;
		var logger = new ListLogger<PhoriaServerMonitor>();
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			new StubHttpClientFactory(HttpStatusCode.ServiceUnavailable),
			Options.Create(new PhoriaObservabilityOptions()));
		using var cancellation = new CancellationTokenSource();

		await Assert.ThrowsAsync<TimeoutException>(
			() => monitor.StartMonitoring(cancellation.Token)
				.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));

		Assert.True(
			logger.Entries.Any(e => e.Level == LogLevel.Error && e.Message.Contains("startup timeout", StringComparison.OrdinalIgnoreCase)),
			"The inner startup timeout did not occur before the outer test guard.");
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

	private static HttpResponseMessage HealthyResponse(string mode = "development", string[]? frameworks = null)
	{
		string frameworksJson = string.Join(",", (frameworks ?? []).Select(f => $"\"{f}\""));
		return new(HttpStatusCode.OK)
		{
			Content = new StringContent($"{{\"mode\":\"{mode}\",\"frameworks\":[{frameworksJson}]}}")
		};
	}

	private static HttpResponseMessage UnhealthyResponse() => new(HttpStatusCode.ServiceUnavailable);

	private static HttpResponseMessage ThrowResponse() => throw new HttpRequestException("Connection refused (localhost)");

	private sealed class ThrowingHttpClientFactory(Exception exception) : IPhoriaServerHttpClientFactory
	{
		public HttpClient CreateClient() => throw exception;
	}

	private sealed class ChangingHealthHttpClientFactory : IPhoriaServerHttpClientFactory
	{
		private int requests;

		public HttpClient CreateClient() => new(new ChangingHealthHttpMessageHandler(this))
		{
			BaseAddress = new Uri("http://localhost")
		};

		private sealed class ChangingHealthHttpMessageHandler(ChangingHealthHttpClientFactory factory) : HttpMessageHandler
		{
			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var response = new HttpResponseMessage(Interlocked.Increment(ref factory.requests) == 1
					? HttpStatusCode.ServiceUnavailable
					: HttpStatusCode.OK);
				if (response.IsSuccessStatusCode)
				{
					response.Content = new StringContent("{\"mode\":\"development\",\"frameworks\":[]}");
				}

				return Task.FromResult(response);
			}
		}
	}
}
