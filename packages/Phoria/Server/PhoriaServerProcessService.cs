using Microsoft.Extensions.Hosting;

namespace Phoria.Server;

public sealed class PhoriaServerProcessService(IPhoriaServerProcess serverProcess)
	: BackgroundService
{
	private readonly IPhoriaServerProcess serverProcess = serverProcess;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Note: stopping the debugger tears down the host without running graceful shutdown, so StopServer
		// never runs and the node process is orphaned. That is host/debugger behavior this library cannot
		// intercept (see the plan's "Known deferred issues").
		// On a normal host shutdown, StartServer registers the stopping token to run StopServer (SIGTERM,
		// grace period, process-tree kill) and deliberately does not pass the token to CliWrap ListenAsync,
		// so the graceful path is not pre-empted by CliWrap's process.Kill().

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
