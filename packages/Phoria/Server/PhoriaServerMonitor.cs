using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Phoria.Server;

// TODO: Do I want to implement something like ViteDevServerLauncher to start the server on startup?

public interface IPhoriaServerMonitor
{
	PhoriaServerStatus ServerStatus { get; }
	Task StartMonitoring(CancellationToken cancellationToken);
	Task StopMonitoring();
}

public sealed class PhoriaServerMonitor
	: IPhoriaServerMonitor, IDisposable
{
	internal const string HealthCheckUrl = "/hc";

	private readonly ILogger<PhoriaServerMonitor> logger;
	private readonly PhoriaOptions options;
	private readonly IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory;

	private PhoriaServerStatus serverStatus;
	private SemaphoreSlim? semaphore;
	private PeriodicTimer? periodicTimer;

	public PhoriaServerStatus ServerStatus => serverStatus;

	private static readonly JsonSerializerOptions jsonDeserializeOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
	};

	public PhoriaServerMonitor(
		ILogger<PhoriaServerMonitor> logger,
		IOptions<PhoriaOptions> options,
		IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory)
	{
		this.logger = logger;
		this.options = options.Value;
		this.phoriaServerHttpClientFactory = phoriaServerHttpClientFactory;

		serverStatus = CreateUnknownServerStatus();
	}

	public async Task StartMonitoring(CancellationToken cancellationToken)
	{
		if (periodicTimer != null)
		{
			return;
		}

		semaphore = new(1, 1);

		// Make an initial health check

		await CheckHealth();

		// Start a periodic health check
		periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.Server.HealthCheckInterval));

		while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
		{
			await CheckHealth();
		}
	}

	public Task StopMonitoring()
	{
		Dispose();

		semaphore = null;
		periodicTimer = null;

		return Task.CompletedTask;
	}

	private async Task CheckHealth()
	{
		if (await semaphore!.WaitAsync(0))
		{
			using HttpClient httpClient = phoriaServerHttpClientFactory.CreateClient();

			using var timeout = new CancellationTokenSource(
				TimeSpan.FromSeconds(options.Server.HealthCheckTimeout)
			);

			try
			{
				HttpResponseMessage response = await httpClient.GetAsync(HealthCheckUrl, timeout.Token);

				if (response.IsSuccessStatusCode)
				{
					// TODO: We may want to get more info from the server response e.g. what frameworks are registered

					PhoriaHealthCheckResult? result = await response.Content.ReadFromJsonAsync<PhoriaHealthCheckResult>(jsonDeserializeOptions);

					if (result != null)
					{
						logger.LogServerIsHealthy(serverStatus.Url);

						serverStatus = CreateHealthyServerStatus(result);

						return;
					}

				}

				logger.LogServerIsUnhealthy(serverStatus.Url);

				serverStatus = CreateUnhealthyServerStatus();
			}
			catch (Exception ex)
			{
				logger.LogServerIsUnhealthy(serverStatus.Url, ex);

				serverStatus = CreateUnhealthyServerStatus();
			}
			finally
			{
				semaphore.Release();
			}
		}
	}

	private PhoriaServerStatus CreateHealthyServerStatus(PhoriaHealthCheckResult result)
	{
		return new PhoriaServerStatus
		{
			Health = PhoriaServerHealth.Healthy,
			Mode = result.Mode,
			Url = options.GetServerUrl()
		};
	}

	private PhoriaServerStatus CreateUnhealthyServerStatus()
	{
		return new PhoriaServerStatus
		{
			Health = PhoriaServerHealth.Unhealthy,
			Mode = PhoriaServerMode.Unknown,
			Url = options.GetServerUrl()
		};
	}

	private PhoriaServerStatus CreateUnknownServerStatus()
	{
		return new PhoriaServerStatus
		{
			Health = PhoriaServerHealth.Unknown,
			Mode = PhoriaServerMode.Unknown,
			Url = options.GetServerUrl()
		};
	}

	public void Dispose()
	{
		semaphore?.Dispose();
		periodicTimer?.Dispose();
	}
}

internal sealed record PhoriaHealthCheckResult
{
	public PhoriaServerMode Mode { get; init; }
}

internal static partial class PhoriaServerMonitorLogMessages
{
	[LoggerMessage(
		EventId = 1201,
		Message = "Phoria server at {Url} is healthy.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerIsHealthy(
		this ILogger logger,
		string url);

	private static readonly Action<ILogger, string, Exception?> logServerIsUnhealthy = LoggerMessage.Define<string>(
		LogLevel.Error,
		1202,
		"Phoria server at {Url} is unhealthy.");
	internal static void LogServerIsUnhealthy(
		this ILogger logger,
		string url,
		Exception? exception = null)
	{
		logServerIsUnhealthy(logger, url, exception);
	}
}
