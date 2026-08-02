using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

	private sealed class StubHttpClientFactory(HttpStatusCode statusCode) : IPhoriaServerHttpClientFactory
	{
		public HttpClient CreateClient() => new(new StubHttpMessageHandler(statusCode))
		{
			BaseAddress = new Uri("http://localhost")
		};
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
