using System.Diagnostics;
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

public sealed class PhoriaServerProcess(
	ILogger<PhoriaServerProcess> logger,
	IPhoriaServerMonitor serverMonitor,
	IHostEnvironment environment,
	IOptions<PhoriaOptions> options)
	: IPhoriaServerProcess, IDisposable
{
	private readonly ILogger<PhoriaServerProcess> logger = logger;
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IHostEnvironment environment = environment;
	private readonly PhoriaOptions options = options.Value;
	private SemaphoreSlim? semaphore;
	private PeriodicTimer? periodicTimer;
	private int? processId;

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
			logger.LogServerProcessIsStarting(processOptions.Command, string.Join(" ", processOptions.Arguments ?? []));

			try
			{
				Command cmd = Cli.Wrap(processOptions.Command)
					.WithArguments(processOptions.Arguments ?? [])
					.WithWorkingDirectory(environment.ContentRootPath)
					.WithValidation(CommandResultValidation.None);

				await foreach (CommandEvent cmdEvent in cmd.ListenAsync(cancellationToken))
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
				semaphore.Release();

				await StopServer();
			}
		}
	}

	public Task StopServer()
	{
		if (processId.HasValue)
		{
			try
			{
				var process = Process.GetProcessById(processId.Value);
				process.Kill();
				process.WaitForExit(10000);
			}
			catch (Exception ex)
			{
				logger.LogServerProcessException(ex);
			}
			finally
			{
				processId = null;
			}
		}

		Dispose();

		semaphore = null;
		periodicTimer = null;
		processId = null;

		return Task.CompletedTask;
	}

	public void Dispose()
	{
		semaphore?.Dispose();
		periodicTimer?.Dispose();
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

	private static readonly Action<ILogger, Exception?> logServerProcessException = LoggerMessage.Define(
		LogLevel.Error,
		EventFeature.Server + 13,
		"Phoria server process caused exception.");
	internal static void LogServerProcessException(
		this ILogger logger,
		Exception? exception = null) => logServerProcessException(logger, exception);
}
