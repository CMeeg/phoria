using Microsoft.Extensions.Hosting;

namespace Phoria.Server;

public sealed class PhoriaServerProcessService(IPhoriaServerProcess serverProcess)
	: BackgroundService
{
	private readonly IPhoriaServerProcess serverProcess = serverProcess;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		// Note: stopping the debugger tears down the host without running graceful shutdown, so
		// StopServer never runs and the node process is orphaned. That is host/debugger behavior this
		// library cannot intercept. StopServer now sends SIGTERM, waits a grace period, then force-kills
		// the process tree, but on a normal host shutdown this graceful path is pre-empted: CliWrap's
		// ListenAsync registers process.Kill() on the stopping token, SIGKILLing the node process before
		// StopServer sees it. Making the graceful path effective requires StartServer to stop passing the
		// stopping token to ListenAsync (see the phase-2 server-robustness task 4 report / follow-ups).

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
