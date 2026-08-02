using System.Diagnostics;
using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Logging;

namespace Phoria.Server;

public interface IPhoriaServerProcess
{
	Task StartServer(CancellationToken stoppingToken);
	Task StopServer();
}

public sealed class PhoriaServerProcess
	: IPhoriaServerProcess, IDisposable
{
	internal static readonly TimeSpan StopGracePeriod = TimeSpan.FromSeconds(6);

	private readonly ILogger<PhoriaServerProcess> logger;
	private readonly IPhoriaServerMonitor serverMonitor;
	private readonly IHostEnvironment environment;
	private readonly PhoriaOptions options;
	private readonly TimeSpan stopGracePeriod;
	private readonly object sync = new();
	private SemaphoreSlim? semaphore;
	private PeriodicTimer? periodicTimer;
	private int? processId;

	public PhoriaServerProcess(
		ILogger<PhoriaServerProcess> logger,
		IPhoriaServerMonitor serverMonitor,
		IHostEnvironment environment,
		IOptions<PhoriaOptions> options)
		: this(logger, serverMonitor, environment, options, null, StopGracePeriod)
	{
	}

	internal PhoriaServerProcess(
		ILogger<PhoriaServerProcess> logger,
		IPhoriaServerMonitor serverMonitor,
		IHostEnvironment environment,
		IOptions<PhoriaOptions> options,
		int? processId,
		TimeSpan stopGracePeriod)
	{
		this.logger = logger;
		this.serverMonitor = serverMonitor;
		this.environment = environment;
		this.options = options.Value;
		this.processId = processId;
		this.stopGracePeriod = stopGracePeriod;
	}

	public async Task StartServer(CancellationToken stoppingToken)
	{
		if (options.Server.Process is null)
		{
			logger.LogServerProcessNotConfigured();

			return;
		}

		if (periodicTimer != null)
		{
			return;
		}

		semaphore = new(1, 1);

		// Host shutdown is driven by StopServer rather than by cancelling ListenAsync: CliWrap registers
		// process.Kill() (SIGKILL) on the forceful token passed to ListenAsync, which would pre-empt
		// StopServer's graceful SIGTERM sequence. The callback runs StopServer (SIGTERM -> grace period ->
		// process-tree kill) when the host requests shutdown.
		using CancellationTokenRegistration stopRegistration = stoppingToken.Register(() => _ = StopServer());

		// Start the process

		await EnsureProcessIsRunning(options.Server.Process, stoppingToken);

		// Start a periodic timer to keep the process running

		periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.Server.Process.HealthCheckInterval));

		while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
		{
			await EnsureProcessIsRunning(options.Server.Process, stoppingToken);
		}
	}

	private async Task EnsureProcessIsRunning(
		PhoriaServerOptions.ProcessOptions processOptions,
		CancellationToken cancellationToken)
	{
		if (serverMonitor.ServerStatus.Health == PhoriaServerHealth.Healthy)
		{
			logger.LogServerProcessIsHealthy();

			return;
		}

		if (processId.HasValue)
		{
			var process = Process.GetProcessById(processId.Value);

			if (process.HasExited)
			{
				processId = null;
			}
			else
			{
				logger.LogServerProcessIsRunning(processId.Value);

				return;
			}
		}

		if (await semaphore!.WaitAsync(0, cancellationToken))
		{
			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogServerProcessIsStarting(processOptions.Command, string.Join(" ", processOptions.Arguments ?? []));
			}

			try
			{
				Command cmd = Cli.Wrap(processOptions.Command)
					.WithArguments(processOptions.Arguments ?? [])
					.WithWorkingDirectory(environment.ContentRootPath)
					.WithValidation(CommandResultValidation.None);

				// Deliberately no cancellation tokens: the host stopping token must not reach ListenAsync,
				// where CliWrap would register process.Kill() (SIGKILL, forceful token) or process.Interrupt()
				// (SIGINT, graceful token) on it and pre-empt StopServer's SIGTERM sequence. Termination is
				// owned by StopServer (see the stop registration in StartServer); ListenAsync unwinds when
				// the process exits.
				await foreach (CommandEvent cmdEvent in cmd.ListenAsync(CancellationToken.None))
				{
					switch (cmdEvent)
					{
						case StartedCommandEvent started:
							processId = started.ProcessId;
							logger.LogServerProcessIsRunning(processId.Value);
							break;
						case StandardOutputCommandEvent stdOut:
							logger.LogServerProcessStdOut(stdOut.Text);
							break;
						case StandardErrorCommandEvent stdErr:
							logger.LogServerProcessStdErr(stdErr.Text);
							break;
						case ExitedCommandEvent exited:
							processId = null;
							logger.LogServerProcessExited(exited.ExitCode);
							break;
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogServerProcessException(ex);
			}
			catch (Exception ex)
			{
				logger.LogServerProcessException(ex);
			}
			finally
			{
				lock (sync)
				{
					semaphore?.Release();
				}

				await StopServer();
			}
		}
	}

	private const int TermSignal = 15;

	[DllImport("libc", EntryPoint = "kill")]
	private static extern int SendSignal(int pid, int signal);

	private async Task StopProcess(int pid)
	{
		try
		{
			using var process = Process.GetProcessById(pid);

			if (process.HasExited)
			{
				return;
			}

			if (OperatingSystem.IsWindows())
			{
				process.Kill(entireProcessTree: true);
			}
			else if (SendSignal(pid, TermSignal) != 0)
			{
				logger.LogServerProcessException(
					new InvalidOperationException($"Failed to send termination signal to process {pid}."));

				process.Kill(entireProcessTree: true);
			}
			else
			{
				logger.LogServerProcessTerminationSignalSent(pid);
			}

			using var waitTimeout = new CancellationTokenSource(stopGracePeriod);

			try
			{
				await process.WaitForExitAsync(waitTimeout.Token);
			}
			catch (OperationCanceledException) when (waitTimeout.IsCancellationRequested)
			{
				logger.LogServerProcessForceStopped(pid);

				process.Kill(entireProcessTree: true);

				await process.WaitForExitAsync();
			}
		}
		catch (Exception ex)
		{
			logger.LogServerProcessException(ex);
		}
	}

	public async Task StopServer()
	{
		int? processIdToStop;
		SemaphoreSlim? semaphoreToDispose;
		PeriodicTimer? periodicTimerToDispose;

		// Capture and clear the state under the lock so concurrent callers (the shutdown registration in
		// StartServer, the EnsureProcessIsRunning finally block, and PhoriaServerProcessService) only stop
		// the process once.
		lock (sync)
		{
			processIdToStop = processId;
			processId = null;
			semaphoreToDispose = semaphore;
			semaphore = null;
			periodicTimerToDispose = periodicTimer;
			periodicTimer = null;
		}

		if (processIdToStop.HasValue)
		{
			await StopProcess(processIdToStop.Value);
		}

		semaphoreToDispose?.Dispose();
		periodicTimerToDispose?.Dispose();
	}

	public void Dispose()
	{
		lock (sync)
		{
			semaphore?.Dispose();
			periodicTimer?.Dispose();
			semaphore = null;
			periodicTimer = null;
		}
	}
}

internal static partial class PhoriaServerProcessLogMessages
{
	[LoggerMessage(
		EventId = EventFeature.Server + 7,
		Message = "Phoria server process will not start. Process is not configured.",
		Level = LogLevel.Information)]
	internal static partial void LogServerProcessNotConfigured(this ILogger logger);

	[LoggerMessage(
		EventId = EventFeature.Server + 8,
		Message = "Phoria server process is healthy.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessIsHealthy(this ILogger logger);

	[LoggerMessage(
		EventId = EventFeature.Server + 14,
		Message = "Phoria server process starting {Command} {Args}.",
		Level = LogLevel.Information)]
	internal static partial void LogServerProcessIsStarting(
		this ILogger logger,
		string command,
		string args);

	[LoggerMessage(
		EventId = EventFeature.Server + 9,
		Message = "Phoria server process is running on pid {ProcessId}.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessIsRunning(
		this ILogger logger,
		int processId);

	[LoggerMessage(
		EventId = EventFeature.Server + 10,
		Message = "Phoria server out: {StdOut}",
		Level = LogLevel.Information)]
	internal static partial void LogServerProcessStdOut(
		this ILogger logger,
		string stdOut);

	[LoggerMessage(
		EventId = EventFeature.Server + 11,
		Message = "Phoria server err: {StdErr}",
		Level = LogLevel.Error)]
	internal static partial void LogServerProcessStdErr(
		this ILogger logger,
		string stdErr);

	[LoggerMessage(
		EventId = EventFeature.Server + 12,
		Message = "Phoria server process exited with code {ExitCode}.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessExited(
		this ILogger logger,
		int exitCode);

	[LoggerMessage(
		EventId = EventFeature.Server + 15,
		Message = "Phoria server process {ProcessId} was sent a termination signal.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessTerminationSignalSent(
		this ILogger logger,
		int processId);

	[LoggerMessage(
		EventId = EventFeature.Server + 16,
		Message = "Phoria server process {ProcessId} did not exit within the grace period and was forcefully terminated.",
		Level = LogLevel.Warning)]
	internal static partial void LogServerProcessForceStopped(
		this ILogger logger,
		int processId);

	private static readonly Action<ILogger, Exception?> logServerProcessException = LoggerMessage.Define(
		LogLevel.Error,
		EventFeature.Server + 13,
		"Phoria server process caused exception.");
	internal static void LogServerProcessException(
		this ILogger logger,
		Exception? exception = null) => logServerProcessException(logger, exception);
}
