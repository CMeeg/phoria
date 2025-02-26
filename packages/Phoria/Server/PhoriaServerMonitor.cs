using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Logging;

namespace Phoria.Server;

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
	private SemaphoreSlim? semaphore;
	private PeriodicTimer? periodicTimer;

	public PhoriaServerStatus ServerStatus { get; private set; }

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

		ServerStatus = CreateUnknownServerStatus();
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
					PhoriaHealthCheckResult? result = await response.Content.ReadFromJsonAsync<PhoriaHealthCheckResult>(jsonDeserializeOptions);

					if (result != null)
					{
						logger.LogServerIsHealthy(ServerStatus.Url);

						ServerStatus = CreateHealthyServerStatus(result);

						return;
					}
				}

				logger.LogServerIsUnhealthy(ServerStatus.Url);

				ServerStatus = CreateUnhealthyServerStatus();
			}
			catch (Exception ex)
			{
				logger.LogServerIsUnhealthy(ServerStatus.Url, ex);

				ServerStatus = CreateUnhealthyServerStatus();
			}
			finally
			{
				semaphore.Release();
			}
		}
	}

	private PhoriaServerStatus CreateHealthyServerStatus(PhoriaHealthCheckResult result) => new()
	{
		Health = PhoriaServerHealth.Healthy,
		Mode = result.Mode,
		Frameworks = result.Frameworks,
		Url = options.GetServerUrl()
	};

	private PhoriaServerStatus CreateUnhealthyServerStatus() => new()
	{
		Health = PhoriaServerHealth.Unhealthy,
		Url = options.GetServerUrl()
	};

	private PhoriaServerStatus CreateUnknownServerStatus() => new()
	{
		Url = options.GetServerUrl()
	};

	public Task StopMonitoring()
	{
		Dispose();

		semaphore = null;
		periodicTimer = null;

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		semaphore?.Dispose();
		periodicTimer?.Dispose();
	}
}

internal sealed record PhoriaHealthCheckResult
{
	public PhoriaServerMode Mode { get; init; } = PhoriaServerMode.Unknown;
	public string[] Frameworks { get; init; } = [];
}

internal static partial class PhoriaServerMonitorLogMessages
{
	[LoggerMessage(
		EventId = EventFeature.Server + 2,
		Message = "Phoria server at {Url} is healthy.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerIsHealthy(
		this ILogger logger,
		string url);

	private static readonly Action<ILogger, string, Exception?> logServerIsUnhealthy = LoggerMessage.Define<string>(
		LogLevel.Error,
		EventFeature.Server + 3,
		"Phoria server at {Url} is unhealthy.");
	internal static void LogServerIsUnhealthy(
		this ILogger logger,
		string url,
		Exception? exception = null) => logServerIsUnhealthy(logger, url, exception);
}
