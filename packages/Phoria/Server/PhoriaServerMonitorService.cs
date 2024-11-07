using Microsoft.Extensions.Hosting;

namespace Phoria.Server;

public class PhoriaServerMonitorService
	: BackgroundService
{
	private readonly IPhoriaServerMonitor serverMonitor;

	public PhoriaServerMonitorService(IPhoriaServerMonitor serverMonitor)
	{
		this.serverMonitor = serverMonitor;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
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
