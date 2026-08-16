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
}
