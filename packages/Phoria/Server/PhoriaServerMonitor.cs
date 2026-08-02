using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventId = Phoria.Logging.EventId;

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
	private Task? monitoringTask;
	private CancellationTokenSource? monitoringCancellation;
	private TaskCompletionSource firstHealthy = CreateFirstHealthySource();

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
		if (monitoringTask != null)
		{
			await firstHealthy.Task.WaitAsync(cancellationToken);
			return;
		}

		semaphore = new(1, 1);
		firstHealthy = CreateFirstHealthySource();
		monitoringCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		monitoringTask = MonitorAsync(monitoringCancellation.Token);

		await firstHealthy.Task.WaitAsync(cancellationToken);
	}

	private async Task MonitorAsync(CancellationToken cancellationToken)
	{
		try
		{
			await CheckHealth(cancellationToken);
			periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.Server.HealthCheckInterval));

			while (await periodicTimer.WaitForNextTickAsync(cancellationToken))
			{
				await CheckHealth(cancellationToken);
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			firstHealthy.TrySetCanceled(cancellationToken);
		}
		catch (Exception ex)
		{
			firstHealthy.TrySetException(ex);
		}
	}

	private async Task CheckHealth(CancellationToken cancellationToken)
	{
		if (await semaphore!.WaitAsync(0, cancellationToken))
		{
			using HttpClient httpClient = phoriaServerHttpClientFactory.CreateClient();

			using var timeout = new CancellationTokenSource(
				TimeSpan.FromSeconds(options.Server.HealthCheckTimeout)
			);

			try
			{
				using CancellationTokenSource linkedTimeout = CancellationTokenSource.CreateLinkedTokenSource(
					cancellationToken,
					timeout.Token);
				HttpResponseMessage response = await httpClient.GetAsync(HealthCheckUrl, linkedTimeout.Token);

				if (response.IsSuccessStatusCode)
				{
					PhoriaHealthCheckResult? result = await response.Content.ReadFromJsonAsync<PhoriaHealthCheckResult>(jsonDeserializeOptions, cancellationToken);

					if (result != null)
					{
						logger.LogServerIsHealthy(ServerStatus.Url);

						ServerStatus = CreateHealthyServerStatus(result);
						firstHealthy.TrySetResult();

						return;
					}
				}

				logger.LogServerIsUnhealthy(ServerStatus.Url);

				ServerStatus = CreateUnhealthyServerStatus();
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
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

	public async Task StopMonitoring()
	{
		monitoringCancellation?.Cancel();

		if (monitoringTask is not null)
		{
			await monitoringTask;
		}

		Dispose();

		semaphore = null;
		periodicTimer = null;
		monitoringTask = null;
		monitoringCancellation?.Dispose();
		monitoringCancellation = null;

	}

	public void Dispose()
	{
		semaphore?.Dispose();
		periodicTimer?.Dispose();
	}

	private static TaskCompletionSource CreateFirstHealthySource() =>
		new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed record PhoriaHealthCheckResult
{
	public PhoriaServerMode Mode { get; init; } = PhoriaServerMode.Unknown;
	public string[] Frameworks { get; init; } = [];
}

internal static partial class PhoriaServerMonitorLogMessages
{
	[LoggerMessage(
		EventId = EventId.Server.ServerIsHealthy,
		Message = "Phoria server at {Url} is healthy.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerIsHealthy(
		this ILogger logger,
		string url);

	private static readonly Action<ILogger, string, Exception?> logServerIsUnhealthy = LoggerMessage.Define<string>(
		LogLevel.Error,
		EventId.Server.ServerIsUnhealthy,
		"Phoria server at {Url} is unhealthy.");
	internal static void LogServerIsUnhealthy(
		this ILogger logger,
		string url,
		Exception? exception = null) => logServerIsUnhealthy(logger, url, exception);
}
