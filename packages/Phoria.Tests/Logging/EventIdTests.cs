using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Xunit;
using Registry = Phoria.Logging.EventId;

namespace Phoria.Tests.Logging;

public class EventIdTests
{
	[Fact]
	public void Registry_ContainsTheCompleteEventIdContract()
	{
		Assert.Equal(
		[
			(nameof(Registry.Islands.EntryAttributeMissing), 1101),
			(nameof(Registry.Islands.ViteManifestKeyNotFound), 1102),
			(nameof(Registry.Islands.ManifestEntryDoesntHaveCssChunks), 1103),
			(nameof(Registry.Islands.ServerUnhealthyDegradingToClient), 1104)
		],
		[
			(nameof(Registry.Islands.EntryAttributeMissing), Registry.Islands.EntryAttributeMissing),
			(nameof(Registry.Islands.ViteManifestKeyNotFound), Registry.Islands.ViteManifestKeyNotFound),
			(nameof(Registry.Islands.ManifestEntryDoesntHaveCssChunks), Registry.Islands.ManifestEntryDoesntHaveCssChunks),
			(nameof(Registry.Islands.ServerUnhealthyDegradingToClient), Registry.Islands.ServerUnhealthyDegradingToClient)
		]);

		Assert.Equal(
		[
			(nameof(Registry.Server.MiddlewareProxyViaHttpError), 1201),
			(nameof(Registry.Server.ServerIsHealthy), 1202),
			(nameof(Registry.Server.ServerIsUnhealthy), 1203),
			(nameof(Registry.Server.EstablishingWebSocketProxy), 1204),
			(nameof(Registry.Server.FailedToEstablishWebSocketProxy), 1205),
			(nameof(Registry.Server.FailedToCloseWebSocket), 1206),
			(nameof(Registry.Server.ProcessNotConfigured), 1207),
			(nameof(Registry.Server.ProcessIsHealthy), 1208),
			(nameof(Registry.Server.ProcessIsRunning), 1209),
			(nameof(Registry.Server.ProcessStdOut), 1210),
			(nameof(Registry.Server.ProcessStdErr), 1211),
			(nameof(Registry.Server.ProcessExited), 1212),
			(nameof(Registry.Server.ProcessException), 1213),
			(nameof(Registry.Server.ProcessStarting), 1214),
			(nameof(Registry.Server.ProcessTerminationSignalSent), 1215),
			(nameof(Registry.Server.ProcessForceStopped), 1216)
		],
		[
			(nameof(Registry.Server.MiddlewareProxyViaHttpError), Registry.Server.MiddlewareProxyViaHttpError),
			(nameof(Registry.Server.ServerIsHealthy), Registry.Server.ServerIsHealthy),
			(nameof(Registry.Server.ServerIsUnhealthy), Registry.Server.ServerIsUnhealthy),
			(nameof(Registry.Server.EstablishingWebSocketProxy), Registry.Server.EstablishingWebSocketProxy),
			(nameof(Registry.Server.FailedToEstablishWebSocketProxy), Registry.Server.FailedToEstablishWebSocketProxy),
			(nameof(Registry.Server.FailedToCloseWebSocket), Registry.Server.FailedToCloseWebSocket),
			(nameof(Registry.Server.ProcessNotConfigured), Registry.Server.ProcessNotConfigured),
			(nameof(Registry.Server.ProcessIsHealthy), Registry.Server.ProcessIsHealthy),
			(nameof(Registry.Server.ProcessIsRunning), Registry.Server.ProcessIsRunning),
			(nameof(Registry.Server.ProcessStdOut), Registry.Server.ProcessStdOut),
			(nameof(Registry.Server.ProcessStdErr), Registry.Server.ProcessStdErr),
			(nameof(Registry.Server.ProcessExited), Registry.Server.ProcessExited),
			(nameof(Registry.Server.ProcessException), Registry.Server.ProcessException),
			(nameof(Registry.Server.ProcessStarting), Registry.Server.ProcessStarting),
			(nameof(Registry.Server.ProcessTerminationSignalSent), Registry.Server.ProcessTerminationSignalSent),
			(nameof(Registry.Server.ProcessForceStopped), Registry.Server.ProcessForceStopped)
		]);

		Assert.Equal(
		[
			(nameof(Registry.Vite.ManifestFileWontBeRead), 1301),
			(nameof(Registry.Vite.DetectedChangeInManifest), 1302),
			(nameof(Registry.Vite.ManifestFileNotFound), 1303),
			(nameof(Registry.Vite.SsrManifestFileWontBeRead), 1304),
			(nameof(Registry.Vite.DetectedChangeInSsrManifest), 1305),
			(nameof(Registry.Vite.SsrManifestFileNotFound), 1306)
		],
		[
			(nameof(Registry.Vite.ManifestFileWontBeRead), Registry.Vite.ManifestFileWontBeRead),
			(nameof(Registry.Vite.DetectedChangeInManifest), Registry.Vite.DetectedChangeInManifest),
			(nameof(Registry.Vite.ManifestFileNotFound), Registry.Vite.ManifestFileNotFound),
			(nameof(Registry.Vite.SsrManifestFileWontBeRead), Registry.Vite.SsrManifestFileWontBeRead),
			(nameof(Registry.Vite.DetectedChangeInSsrManifest), Registry.Vite.DetectedChangeInSsrManifest),
			(nameof(Registry.Vite.SsrManifestFileNotFound), Registry.Vite.SsrManifestFileNotFound)
		]);
	}

	[Fact]
	public async Task ServerHealthLog_UsesTheRegisteredEventIdAndRenderedMessage()
	{
		var logger = new CapturingLogger<PhoriaServerMonitor>();
		var options = new PhoriaOptions();
		options.Server.HealthCheckInterval = 1;
		var monitor = new PhoriaServerMonitor(
			logger,
			Options.Create(options),
			new HealthyHttpClientFactory());
		using var cancellation = new CancellationTokenSource();

		await monitor.StartMonitoring(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
		await monitor.StopMonitoring();

		var entry = Assert.Single(logger.Entries);
		Assert.Equal(LogLevel.Debug, entry.Level);
		Assert.Equal(Registry.Server.ServerIsHealthy, entry.EventId.Id);
		Assert.Equal("Phoria server at http://localhost:5173 is healthy.", entry.Message);
	}

	private sealed class CapturingLogger<T> : ILogger<T>
	{
		public List<LogEntry> Entries { get; } = [];

		public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			Microsoft.Extensions.Logging.EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter) =>
			Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
	}

	private sealed record LogEntry(LogLevel Level, Microsoft.Extensions.Logging.EventId EventId, string Message);

	private sealed class NullScope : IDisposable
	{
		public static NullScope Instance { get; } = new();

		public void Dispose() { }
	}

	private sealed class HealthyHttpClientFactory : IPhoriaServerHttpClientFactory
	{
		public HttpClient CreateClient() => new(new HealthyHttpMessageHandler())
		{
			BaseAddress = new Uri("http://localhost")
		};
	}

	private sealed class HealthyHttpMessageHandler : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("{\"mode\":\"development\",\"frameworks\":[]}")
			});
	}
}
