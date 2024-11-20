using Microsoft.Extensions.Hosting;

namespace Phoria.Server;

public sealed class PhoriaServerProcessService
	: BackgroundService
{
	private readonly IPhoriaServerProcess serverProcess;

	public PhoriaServerProcessService(IPhoriaServerProcess serverProcess)
	{
		this.serverProcess = serverProcess;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// TODO: Process is not being killed when stoppingToken is triggered - though it seems only a problem when stopping the debugger - stopping the process in the terminal works fine

		try
		{
			await serverProcess.StartServer(stoppingToken);
		}
		catch (OperationCanceledException)
		{
			await serverProcess.StopServer();
		}
	}
}
