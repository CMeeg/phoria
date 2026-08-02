using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.IO;
using Phoria.Islands;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class PhoriaServerMonitorTests
{
	[Fact]
	public async Task StartMonitoring_CompletesAfterFirstHealthyCheck()
	{
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			new StubHttpClientFactory(HttpStatusCode.OK));
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
		var monitor = new PhoriaServerMonitor(
			NullLogger<PhoriaServerMonitor>.Instance,
			Options.Create(options),
			new StubHttpClientFactory(HttpStatusCode.ServiceUnavailable));
		using var cancellation = new CancellationTokenSource();

		Task startTask = monitor.StartMonitoring(cancellation.Token);
		await Task.Delay(50, TestContext.Current.CancellationToken);
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
			new ThrowingHttpClientFactory(failure));
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
			new ChangingHealthHttpClientFactory());
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

	private sealed class StubHttpClientFactory(HttpStatusCode statusCode) : IPhoriaServerHttpClientFactory
	{
		public HttpClient CreateClient() => new(new StubHttpMessageHandler(statusCode))
		{
			BaseAddress = new Uri("http://localhost")
		};
	}

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

	private sealed class TrackingSsr : IPhoriaIslandSsr
	{
		public int CallCount { get; private set; }

		public Task<PhoriaIslandSsrResult> RenderIsland(PhoriaIsland island, CancellationToken cancellationToken = default)
		{
			CallCount++;
			return Task.FromResult(new PhoriaIslandSsrResult
			{
				Headers = new HttpResponseMessage().Headers,
				Content = new StreamPool()
			});
		}
	}

	private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var response = new HttpResponseMessage(statusCode);
			if (statusCode == HttpStatusCode.OK)
			{
				response.Content = new StringContent("{\"mode\":\"development\",\"frameworks\":[]}");
			}

			return Task.FromResult(response);
		}
	}
}
