using System.Diagnostics;
using System.Runtime.InteropServices;
using CliWrap;
using CliWrap.EventStream;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EventId = Phoria.Logging.EventId;

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
	private readonly Action<int>? beforeProcessIdAssignment;
	private readonly TimeSpan stopGracePeriod;
	private readonly object sync = new();
	private SemaphoreSlim? semaphore;
	private PeriodicTimer? periodicTimer;
	private int? processId;
	private TaskCompletionSource<int?>? processStartCompletion;
	private Task? stopTask;
	private CancellationTokenSource? stopSignal;
	private bool stopping;

	public PhoriaServerProcess(
		ILogger<PhoriaServerProcess> logger,
		IPhoriaServerMonitor serverMonitor,
		IHostEnvironment environment,
		IOptions<PhoriaOptions> options)
		: this(logger, serverMonitor, environment, options, null, StopGracePeriod)
	{
	}

	public PhoriaServerProcess(
		ILogger<PhoriaServerProcess> logger,
		IPhoriaServerMonitor serverMonitor,
		IHostEnvironment environment,
		IOptions<PhoriaOptions> options,
		int? processId,
		TimeSpan stopGracePeriod,
		Action<int>? beforeProcessIdAssignment = null)
	{
		this.logger = logger;
		this.serverMonitor = serverMonitor;
		this.environment = environment;
		this.options = options.Value;
		this.processId = processId;
		this.stopGracePeriod = stopGracePeriod;
		this.beforeProcessIdAssignment = beforeProcessIdAssignment;
	}

	public async Task StartServer(CancellationToken stoppingToken)
	{
		if (options.Server.Process is null)
		{
			logger.LogServerProcessNotConfigured();

			return;
		}

		SemaphoreSlim serverSemaphore;
		lock (sync)
		{
			if (semaphore != null || periodicTimer != null)
			{
				return;
			}

			serverSemaphore = new(1, 1);
			semaphore = serverSemaphore;
		}

		// Host shutdown is driven by StopServer rather than by cancelling ListenAsync: CliWrap registers
		// process.Kill() (SIGKILL) on the forceful token passed to ListenAsync, which would pre-empt
		// StopServer's graceful SIGTERM sequence. The callback runs StopServer (SIGTERM -> grace period ->
		// process-tree kill) when the host requests shutdown.
		using var serverStopSignal = new CancellationTokenSource();
		using CancellationTokenSource linkedStopping = CancellationTokenSource.CreateLinkedTokenSource(
			stoppingToken,
			serverStopSignal.Token);
		lock (sync)
		{
			stopSignal = serverStopSignal;
		}

		using CancellationTokenRegistration stopRegistration = stoppingToken.Register(() => _ = StopServer());

		PeriodicTimer? timer = null;

		try
		{
			// Start the process

			await EnsureProcessIsRunning(options.Server.Process, serverSemaphore, linkedStopping.Token);

			lock (sync)
			{
				if (stopping)
				{
					linkedStopping.Token.ThrowIfCancellationRequested();
					return;
				}

				periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(options.Server.Process.HealthCheckInterval));
				timer = periodicTimer;
			}

			while (await timer.WaitForNextTickAsync(linkedStopping.Token))
			{
				await EnsureProcessIsRunning(options.Server.Process, serverSemaphore, linkedStopping.Token);
			}
		}
		finally
		{
			await StopServer();

			lock (sync)
			{
				if (ReferenceEquals(stopSignal, serverStopSignal))
				{
					stopSignal = null;
				}

				if (ReferenceEquals(semaphore, serverSemaphore))
				{
					semaphore = null;
				}

				if (ReferenceEquals(periodicTimer, timer))
				{
					periodicTimer = null;
				}
			}

			timer?.Dispose();
			serverSemaphore.Dispose();
		}
	}

	private async Task EnsureProcessIsRunning(
		PhoriaServerOptions.ProcessOptions processOptions,
		SemaphoreSlim serverSemaphore,
		CancellationToken cancellationToken)
	{
		if (serverMonitor.ServerStatus.Health == PhoriaServerHealth.Healthy)
		{
			logger.LogServerProcessIsHealthy();

			return;
		}

		int? currentProcessId;
		lock (sync)
		{
			currentProcessId = processId;
		}

		if (currentProcessId.HasValue)
		{
			var process = Process.GetProcessById(currentProcessId.Value);

			if (process.HasExited)
			{
				lock (sync)
				{
					processId = null;
				}
			}
			else
			{
				logger.LogServerProcessIsRunning(currentProcessId.Value);

				return;
			}
		}

		if (await serverSemaphore.WaitAsync(0, cancellationToken))
		{
			TaskCompletionSource<int?>? startCompletion = null;

			if (logger.IsEnabled(LogLevel.Information))
			{
				logger.LogServerProcessIsStarting(processOptions.Command, string.Join(" ", processOptions.Arguments ?? []));
			}

			try
			{
				lock (sync)
				{
					if (stopping)
					{
						return;
					}

					startCompletion = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
					processStartCompletion = startCompletion;
				}

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
							beforeProcessIdAssignment?.Invoke(started.ProcessId);
							lock (sync)
							{
								processId = started.ProcessId;
								startCompletion.TrySetResult(processId);
							}

							logger.LogServerProcessIsRunning(started.ProcessId);
							break;
						case StandardOutputCommandEvent stdOut:
							logger.LogServerProcessStdOut(stdOut.Text);
							break;
						case StandardErrorCommandEvent stdErr:
							logger.LogServerProcessStdErr(stdErr.Text);
							break;
						case ExitedCommandEvent exited:
							lock (sync)
							{
								processId = null;
								startCompletion.TrySetResult(null);
							}

							logger.LogServerProcessExited(exited.ExitCode);
							break;
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogServerProcessException(ex);
			}
			finally
			{
				lock (sync)
				{
					startCompletion?.TrySetResult(processId);
					if (ReferenceEquals(processStartCompletion, startCompletion))
					{
						processStartCompletion = null;
					}

					serverSemaphore.Release();
				}
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

	public Task StopServer()
	{
		lock (sync)
		{
			if (stopTask is not null)
			{
				return stopTask;
			}

			stopping = true;
			stopTask = StopServerCore(processStartCompletion);
			return stopTask;
		}
	}

	private async Task StopServerCore(TaskCompletionSource<int?>? startCompletion)
	{
		if (startCompletion is not null)
		{
			await startCompletion.Task;
		}

		int? processIdToStop;

		lock (sync)
		{
			stopSignal?.Cancel();
			processIdToStop = processId;
			processId = null;
		}

		if (processIdToStop.HasValue)
		{
			await StopProcess(processIdToStop.Value);
		}

	}

	public void Dispose()
	{
		// StartServer owns the semaphore and timer for the duration of its lifecycle.
	}
}

internal static partial class PhoriaServerProcessLogMessages
{
	[LoggerMessage(
		EventId = EventId.Server.ProcessNotConfigured,
		Message = "Phoria server process will not start. Process is not configured.",
		Level = LogLevel.Information)]
	internal static partial void LogServerProcessNotConfigured(this ILogger logger);

	[LoggerMessage(
		EventId = EventId.Server.ProcessIsHealthy,
		Message = "Phoria server process is healthy.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessIsHealthy(this ILogger logger);

	[LoggerMessage(
		EventId = EventId.Server.ProcessStarting,
		Message = "Phoria server process starting {Command} {Args}.",
		Level = LogLevel.Information)]
	internal static partial void LogServerProcessIsStarting(
		this ILogger logger,
		string command,
		string args);

	[LoggerMessage(
		EventId = EventId.Server.ProcessIsRunning,
		Message = "Phoria server process is running on pid {ProcessId}.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessIsRunning(
		this ILogger logger,
		int processId);

	[LoggerMessage(
		EventId = EventId.Server.ProcessStdOut,
		Message = "Phoria server out: {StdOut}",
		Level = LogLevel.Information)]
	internal static partial void LogServerProcessStdOut(
		this ILogger logger,
		string stdOut);

	[LoggerMessage(
		EventId = EventId.Server.ProcessStdErr,
		Message = "Phoria server err: {StdErr}",
		Level = LogLevel.Error)]
	internal static partial void LogServerProcessStdErr(
		this ILogger logger,
		string stdErr);

	[LoggerMessage(
		EventId = EventId.Server.ProcessExited,
		Message = "Phoria server process exited with code {ExitCode}.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessExited(
		this ILogger logger,
		int exitCode);

	[LoggerMessage(
		EventId = EventId.Server.ProcessTerminationSignalSent,
		Message = "Phoria server process {ProcessId} was sent a termination signal.",
		Level = LogLevel.Debug)]
	internal static partial void LogServerProcessTerminationSignalSent(
		this ILogger logger,
		int processId);

	[LoggerMessage(
		EventId = EventId.Server.ProcessForceStopped,
		Message = "Phoria server process {ProcessId} did not exit within the grace period and was forcefully terminated.",
		Level = LogLevel.Warning)]
	internal static partial void LogServerProcessForceStopped(
		this ILogger logger,
		int processId);

	private static readonly Action<ILogger, Exception?> logServerProcessException = LoggerMessage.Define(
		LogLevel.Error,
		EventId.Server.ProcessException,
		"Phoria server process caused exception.");
	internal static void LogServerProcessException(
		this ILogger logger,
		Exception? exception = null) => logServerProcessException(logger, exception);
}
