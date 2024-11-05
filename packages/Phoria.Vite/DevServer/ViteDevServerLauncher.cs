// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Vite.Logging;

namespace Phoria.Vite.DevServer;

// TODO: Would it be better to have the server running in a sidecar? It might be more reliable as it could be more easily monitored and restarted if it fails

/// <summary>
/// Provides a way to launch the Vite Development Server.
/// </summary>
/// <remarks>
/// Based on <see href="https://github.com/dotnet/aspnetcore/blob/1121b2bbb123ad9044d593955e7a2ef863fcf2e5/src/Middleware/Spa/SpaProxy/src/SpaProxyLaunchManager.cs">Microsoft.AspNetCore.SpaProxy.SpaProxyLaunchManager</see>
/// </remarks>
/// <param name="logger">An <see cref="ILogger{TCategoryName}"/> instance used to log messages.</param>
/// <param name="options">The Vite options.</param>
/// <param name="environment">The <see cref="IWebHostEnvironment"/> instance.</param>
internal sealed class ViteDevServerLauncher(
	ILogger<ViteDevServerLauncher> logger,
	IOptions<ViteOptions> options,
	IWebHostEnvironment environment,
	IHostApplicationLifetime appLifetime,
	IHttpClientFactory httpClientFactory
) : IDisposable
{
	private readonly ILogger<ViteDevServerLauncher> logger = logger;
	private readonly ViteOptions options = options.Value;
	private readonly string contentRootPath = environment.ContentRootPath;
	private readonly IHostApplicationLifetime appLifetime = appLifetime;
	private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
	private bool disposedValue;
	private Process? process;
	private Task? launchTask;
	private bool isRunning;

	/// <summary>
	/// Launch the Vite development server.
	/// </summary>
	/// <param name="httpClient">The <see cref="HttpClient"/> instance used to test the connection to the Vite development server.</param>
	public void LaunchIfNotRunning()
	{
		launchTask ??= StartViteDevServerIfNotRunningAsync();

		if (!isRunning)
		{
			// Wait for the Vite development server to start or timeout.
			int attempts = options.Server.TimeOut;

			do
			{
				// Check if the Vite development server is running.
				IsViteDevelopmentServerRunning().GetAwaiter().GetResult();
				// If it's not running, wait 1 second and try again.
				if (!isRunning)
				{
					Task.Delay(1000).GetAwaiter().GetResult();
				}

				attempts--;
			} while (!isRunning && attempts > 0);

			if (!isRunning)
			{
				logger.LogViteDevServerDidNotStart(options.Server.TimeOut);
			}
			else if (options.Server.AutoRun)
			{
				logger.LogViteDevServerRunning(options.GetViteDevServerUrl());
			}
		}
	}

	/// <summary>
	/// Check if the Vite development server is already running.
	/// </summary>
	private async Task<bool> IsViteDevelopmentServerRunning()
	{
		if (isRunning)
		{
			return true;
		}

		HttpClient httpClient = httpClientFactory.CreateClient();
		using var timeout = new CancellationTokenSource(
			TimeSpan.FromMinutes(options.Server.TimeOut)
		);
		using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
			timeout.Token,
			appLifetime.ApplicationStopping
		);

		try
		{
			// Test the connection to the Vite development server.
			var response = await httpClient.GetAsync(
				options.GetViteDevServerUrl(),
				cancellationTokenSource.Token
			);
			// Check if the Vite development server is running. It could be running if the response is successful or if the status code is 404 (Not Found).
			var running =
				response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
			// Return true if the Vite development server is running, otherwise false.
			return isRunning = running;
		}
		catch (Exception exception)
			when (exception is HttpRequestException
				|| exception is TaskCanceledException
				|| exception is OperationCanceledException
			)
		{
			logger.LogViteDevServerNotRunning();
			return false;
		}
	}

	/// <summary>
	/// Start the Vite development server if it is not already running.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns></returns>
	private async Task StartViteDevServerIfNotRunningAsync()
	{
		// If the Vite development server is already running, return.
		if (await IsViteDevelopmentServerRunning())
		{
			logger.LogViteDevServerAlreadyRunning(options.GetViteDevServerUrl());
			return;
		}

		// Set the command to run.
		string command = options.Server.PackageManager;
		// Set the arguments to run.
		string args = $"run {options.Server.ScriptName}";
		// Set the working directory.
		string workingDirectory = options.Server.PackageDirectory ?? contentRootPath;

		// If the working directory is relative, combine it with the app's base directory.
		if (!Path.IsPathRooted(workingDirectory))
		{
			workingDirectory = Path.GetFullPath(workingDirectory, contentRootPath);
		}

		// Create the process start info.
		var startInfo = new ProcessStartInfo(command, args)
		{
			CreateNoWindow = false,
			UseShellExecute = true,
			WindowStyle = ProcessWindowStyle.Normal,
			WorkingDirectory = Path.GetFullPath(workingDirectory)
		};

		try
		{
			logger.LogStartingViteDevServer();

			// Start the process.
			process = Process.Start(startInfo);

			if (process is { HasExited: false })
			{
				logger.LogViteDevServerStarted(process.Id);

				bool? stopScriptLaunched = null;
				// Ensure the process is killed if the app shuts down.
				if (OperatingSystem.IsWindows())
				{
					ChildProcessTracker.AddProcess(process);
					return;
				}
				else if (OperatingSystem.IsMacOS())
				{
					stopScriptLaunched = LaunchStopScriptForMacOs(process.Id);
				}
				// TODO: Linux?

				// If the stop script was not launched, log a warning.
				if (stopScriptLaunched == false)
				{
					logger.LogFailedToLaunchStopScript(process.Id);
				}
			}
		}
		catch (Exception exp)
		{
			logger.LogFailedToLaunchViteDevServer(exp.Message);
		}
	}

	/// <summary>
	/// On Mac OS, kill the process tree using a Bash script.
	/// </summary>
	/// <param name="processId">The process ID.</param>
	/// <returns>True if the script was launched successfully, otherwise false.</returns>
	private bool LaunchStopScriptForMacOs(int processId)
	{
		// Define the script file name.
		string fileName = Guid.NewGuid().ToString("N") + ".sh";
		// Define the script path.
		string scriptPath = Path.Combine(contentRootPath, fileName);
		// Create the Bash script.
		string stopScript =
			@$"function list_child_processes () {{
    local ppid=$1;
    local current_children=$(pgrep -P $ppid);
    local local_child;
    if [ $? -eq 0 ];
    then
        for current_child in $current_children
        do
          local_child=$current_child;
          list_child_processes $local_child;
          echo $local_child;
        done;
    else
      return 0;
    fi;
}}
ps {Environment.ProcessId};
while [ $? -eq 0 ];
do
  sleep 1;
  ps {Environment.ProcessId} > /dev/null;
done;
for child in $(list_child_processes {processId});
do
  echo killing $child;
  kill -s KILL $child;
done;
rm {scriptPath};
";
		// Write the script to the file.
		File.WriteAllText(scriptPath, stopScript.ReplaceLineEndings());
		// Create the process start info.
		var stopScriptInfo = new ProcessStartInfo("/bin/bash", scriptPath)
		{
			CreateNoWindow = true,
			WorkingDirectory = contentRootPath
		};
		// Start the process.
		var stopProcess = Process.Start(stopScriptInfo);

		// Return true if the process was started successfully.
		return !(stopProcess == null || stopProcess.HasExited);
	}

	private void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			try
			{
				if (process?.HasExited is false && process?.CloseMainWindow() == false)
				{
					process.Kill(true);
					process = null;
					launchTask?.Dispose();
					launchTask = null;
				}
			}
			catch (Exception)
			{
				if (disposing)
				{
					throw;
				}
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			disposedValue = true;
		}
	}

	// TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	~ViteDevServerLauncher()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
