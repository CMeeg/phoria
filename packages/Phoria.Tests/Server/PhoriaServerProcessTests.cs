using System.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class PhoriaServerProcessTests
{
	[Fact]
	public async Task StopServer_NoProcessStarted_CompletesWithoutThrowing()
	{
		using PhoriaServerProcess serverProcess = CreateServerProcess(processId: null);

		await serverProcess.StopServer();
	}

	[Fact]
	public async Task StopServer_ProcessAlreadyExited_CompletesWithoutThrowing()
	{
		using var child = StartNode("process.exit(0);");
		child.WaitForExit();

		using PhoriaServerProcess serverProcess = CreateServerProcess(processId: child.Id);

		await serverProcess.StopServer();
	}

	[Fact]
	public async Task StopServer_ProcessExitsWithinGracePeriod_StopsGracefullyWithoutForceKill()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		string markerPath = CreateMarkerPath(nameof(StopServer_ProcessExitsWithinGracePeriod_StopsGracefullyWithoutForceKill));
		using var child = StartNode(GracefulNodeScript(markerPath));
		await WaitForMarker(markerPath, "ready");

		using PhoriaServerProcess serverProcess = CreateServerProcess(
			processId: child.Id,
			stopGracePeriod: TimeSpan.FromSeconds(30));

		await serverProcess.StopServer();

		string markerContents = await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken);
		Assert.Equal("sigterm", markerContents);
		Assert.Equal(0, child.ExitCode);
	}

	[Fact]
	public async Task StopServer_ProcessIgnoresTerminationSignal_ForceKillsAfterGracePeriod()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		string markerPath = CreateMarkerPath(nameof(StopServer_ProcessIgnoresTerminationSignal_ForceKillsAfterGracePeriod));
		using var child = StartNode(IgnoringNodeScript(markerPath));
		await WaitForMarker(markerPath, "ready");

		using PhoriaServerProcess serverProcess = CreateServerProcess(
			processId: child.Id,
			stopGracePeriod: TimeSpan.FromSeconds(1));

		var stopwatch = Stopwatch.StartNew();
		await serverProcess.StopServer();
		stopwatch.Stop();

		string markerContents = await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken);
		Assert.Equal("sigterm", markerContents);
		Assert.Equal(137, child.ExitCode);
		Assert.True(child.HasExited);
		Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15));
	}

	private static PhoriaServerProcess CreateServerProcess(int? processId = null, TimeSpan? stopGracePeriod = null)
	{
		var options = new PhoriaOptions
		{
			Server = new PhoriaServerOptions
			{
				Process = new PhoriaServerOptions.ProcessOptions
				{
					Command = "node",
					Arguments = ["-e", "setInterval(() => {}, 1000);"]
				}
			}
		};

		return new PhoriaServerProcess(
			NullLogger<PhoriaServerProcess>.Instance,
			new StubServerMonitor(),
			new StubHostEnvironment(),
			Options.Create(options),
			processId,
			stopGracePeriod ?? PhoriaServerProcess.StopGracePeriod);
	}

	private static Process StartNode(string script)
	{
		return Process.Start(new ProcessStartInfo("node", ["-e", script])
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true
		}) ?? throw new InvalidOperationException("Failed to start the node process.");
	}

	private static string CreateMarkerPath(string testName) =>
		Path.Combine(Path.GetTempPath(), $"phoria-{testName}-{Guid.NewGuid():N}.marker");

	private static string GracefulNodeScript(string markerPath) =>
		$$"""
		process.on('SIGTERM', () => {
			require('fs').writeFileSync("{{markerPath}}", 'sigterm');
			process.exit(0);
		});
		require('fs').writeFileSync("{{markerPath}}", 'ready');
		setInterval(() => {}, 1000);
		""";

	private static string IgnoringNodeScript(string markerPath) =>
		$$"""
		process.on('SIGTERM', () => {
			require('fs').writeFileSync("{{markerPath}}", 'sigterm');
		});
		require('fs').writeFileSync("{{markerPath}}", 'ready');
		setInterval(() => {}, 1000);
		""";

	private static async Task WaitForMarker(string markerPath, string expectedContents)
	{
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

		while (DateTime.UtcNow < deadline)
		{
			if (File.Exists(markerPath)
				&& await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken) == expectedContents)
			{
				return;
			}

			await Task.Delay(25);
		}

		throw new InvalidOperationException(
			$"Timed out waiting for node process to write '{expectedContents}' to {markerPath}.");
	}

	private sealed class StubServerMonitor : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Unhealthy,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopMonitoring() => Task.CompletedTask;
	}

	private sealed class StubHostEnvironment : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Development;

		public string ApplicationName { get; set; } = "Phoria.Tests";

		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
