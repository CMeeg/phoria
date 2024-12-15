using Microsoft.Extensions.Hosting;

namespace Phoria.Server;

public class PhoriaServerMonitorService(IPhoriaServerMonitor serverMonitor)
	: BackgroundService
{
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// TODO: If process is set to run in-process, then we should block until the server is started

		try
		{
			await serverMonitor.StartMonitoring(stoppingToken);
		}
		catch (OperationCanceledException)
		{
			await serverMonitor.StopMonitoring();
		}
	}
}
