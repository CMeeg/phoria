using Microsoft.Extensions.Hosting;

namespace Phoria.Server;

public sealed class PhoriaServerMonitorService(IPhoriaServerMonitor serverMonitor)
	: BackgroundService
{
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// StartMonitoring blocks until the server passes its first health check.

		try
		{
			await serverMonitor.StartMonitoring(stoppingToken);
			await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
		}
		catch (OperationCanceledException)
		{
			await serverMonitor.StopMonitoring();
		}
	}
}
