using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Xunit;
using static Phoria.Tests.TestUtilities.AsyncTestWaits;

namespace Phoria.Tests.Server;

public class PhoriaServerProcessTests
{
	// These tests spawn real node processes, so node must be on PATH.

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
			stopGracePeriod: TimeSpan.FromSeconds(10));

		var stopwatch = Stopwatch.StartNew();
		await serverProcess.StopServer();
		stopwatch.Stop();

		// The stop completes quickly on the graceful path; if the grace period were waited out (signal not
		// delivered) the elapsed time would be much larger.
		Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Stop took {stopwatch.Elapsed}.");

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

	[Fact]
	public async Task StopServer_ConcurrentCallersAwaitTheSameStopOperation()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		string markerPath = CreateMarkerPath(nameof(StopServer_ConcurrentCallersAwaitTheSameStopOperation));
		using var child = StartNode(IgnoringNodeScript(markerPath));
		await WaitForMarker(markerPath, "ready");

		using PhoriaServerProcess serverProcess = CreateServerProcess(
			processId: child.Id,
			stopGracePeriod: TimeSpan.FromSeconds(1));

		Task firstStop = serverProcess.StopServer();
		Task secondStop = serverProcess.StopServer();

		Assert.Same(firstStop, secondStop);
		await WaitForMarker(markerPath, "sigterm");

		Assert.False(secondStop.IsCompleted);
		await Task.WhenAll(firstStop, secondStop);

		Assert.True(child.HasExited);
	}

	[Fact]
	public async Task StartServer_CancellationDuringProcessStartup_StopsSpawnedProcess()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		string markerPath = CreateMarkerPath(nameof(StartServer_CancellationDuringProcessStartup_StopsSpawnedProcess));
		int spawnedPid = 0;

		using var cts = new CancellationTokenSource();
		using PhoriaServerProcess serverProcess = CreateServerProcess(
			stopGracePeriod: TimeSpan.FromSeconds(1),
			script: StartupNodeScript(markerPath),
			beforeProcessIdAssignment: pid =>
			{
				spawnedPid = pid;
				cts.Cancel();
			});

		Task startTask = serverProcess.StartServer(cts.Token);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startTask.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));

		Assert.NotEqual(0, spawnedPid);
		Assert.False(IsProcessRunning(spawnedPid));
	}

	[Fact]
	public async Task StartServer_ConcurrentStartup_UsesOneSemaphoreOwner()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		var startedEvent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		int hookCalls = 0;
		Task? secondStart = null;
		using var firstCts = new CancellationTokenSource();
		using var secondCts = new CancellationTokenSource();
		PhoriaServerProcess? serverProcess = null;
		using PhoriaServerProcess ownedServerProcess = CreateServerProcess(
			script: "setInterval(() => {}, 1000);",
			beforeProcessIdAssignment: _ =>
			{
				if (Interlocked.Increment(ref hookCalls) == 1)
				{
					secondStart = serverProcess!.StartServer(secondCts.Token);
					startedEvent.SetResult();
				}
			});
		serverProcess = ownedServerProcess;

		Task firstStart = ownedServerProcess.StartServer(firstCts.Token);
		await startedEvent.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

		Assert.NotNull(secondStart);
		Assert.True(secondStart.IsCompleted);

		firstCts.Cancel();
		secondCts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstStart.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task StartServer_HostStopping_SendsSIGTERMAndRunsGracefulHandlerToCompletion()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		// The SIGTERM handler takes 2s to complete, so the graceful stop is only preserved if the child
		// is not SIGKILLed by CliWrap's ListenAsync cancellation (the pre-fix wiring) in the meantime.
		string markerPath = CreateMarkerPath(nameof(StartServer_HostStopping_SendsSIGTERMAndRunsGracefulHandlerToCompletion));
		string completionMarkerPath = CreateMarkerPath(nameof(StartServer_HostStopping_SendsSIGTERMAndRunsGracefulHandlerToCompletion) + "-done");
		string pidPath = CreateMarkerPath(nameof(StartServer_HostStopping_SendsSIGTERMAndRunsGracefulHandlerToCompletion) + "-pid");

		using PhoriaServerProcess serverProcess = CreateServerProcessViaPublicConstructor(SlowGracefulNodeScript(markerPath, completionMarkerPath, pidPath));
		using var cts = new CancellationTokenSource();

		var startTask = serverProcess.StartServer(cts.Token);

		await WaitForMarker(markerPath, "ready");
		int pid = int.Parse(await File.ReadAllTextAsync(pidPath, TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
		using var child = Process.GetProcessById(pid);

		cts.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startTask.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));

		Assert.Equal("sigterm", await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken));

		// The handler ran to completion rather than being interrupted by a SIGKILL: this is the
		// regression test for the pre-fix wiring, where CliWrap SIGKILLed the child before StopServer's
		// graceful SIGTERM sequence could run.
		Assert.True(File.Exists(completionMarkerPath), "The node SIGTERM handler was interrupted before completing (likely SIGKILLed).");
		Assert.Equal("sigterm-done", await File.ReadAllTextAsync(completionMarkerPath, TestContext.Current.CancellationToken));

		await child.WaitForExitAsync(TestContext.Current.CancellationToken);
		Assert.True(child.HasExited);
	}

	[Fact]
	public async Task StartServer_HostStopping_ForceKillsAfterGracePeriodWhenTerminationSignalIgnored()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		string markerPath = CreateMarkerPath(nameof(StartServer_HostStopping_ForceKillsAfterGracePeriodWhenTerminationSignalIgnored));
		string pidPath = CreateMarkerPath(nameof(StartServer_HostStopping_ForceKillsAfterGracePeriodWhenTerminationSignalIgnored) + "-pid");

		// Inject a short grace period via the internal test seam; StartServer is otherwise exercised
		// through the same path as the public constructor.
		using PhoriaServerProcess serverProcess = CreateServerProcess(
			stopGracePeriod: TimeSpan.FromSeconds(1),
			script: StartServerIgnoringNodeScript(markerPath, pidPath));
		using var cts = new CancellationTokenSource();

		var startTask = serverProcess.StartServer(cts.Token);

		await WaitForMarker(markerPath, "ready");
		int pid = int.Parse(await File.ReadAllTextAsync(pidPath, TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
		using var child = Process.GetProcessById(pid);

		var stopwatch = Stopwatch.StartNew();
		cts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startTask.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));
		stopwatch.Stop();

		Assert.Equal("sigterm", await File.ReadAllTextAsync(markerPath, TestContext.Current.CancellationToken));
		Assert.InRange(stopwatch.Elapsed, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(15));

		await child.WaitForExitAsync(TestContext.Current.CancellationToken);
		Assert.True(child.HasExited);
	}

	[Fact]
	public async Task StartServer_RestartLimitExceeded_StopsSpawningAndEndsSupervisionLoop()
	{
		var hookCalls = 0;
		using var cts = new CancellationTokenSource();
		using PhoriaServerProcess serverProcess = CreateServerProcess(
			script: "process.exit(0);",
			beforeProcessIdAssignment: _ => Interlocked.Increment(ref hookCalls),
			maxRestartAttempts: 1);

		// Give-up returns normally (no throw) once the limit is exceeded.
		await serverProcess.StartServer(cts.Token).WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

		// Initial spawn + 1 restart, then the supervision loop gives up.
		Assert.Equal(2, hookCalls);
	}

	[Fact]
	public async Task StartServer_RestartLimit_ResetsAttemptsWhenServerBecomesHealthy()
	{
		var monitor = new SettableServerMonitor();
		var hookCalls = 0;
		using var cts = new CancellationTokenSource();
		using PhoriaServerProcess serverProcess = CreateServerProcess(
			monitor: monitor,
			script: "process.exit(0);",
			beforeProcessIdAssignment: _ => Interlocked.Increment(ref hookCalls),
			maxRestartAttempts: 1);

		Task startTask = serverProcess.StartServer(cts.Token);

		await WaitUntilAsync(() => hookCalls >= 1, TimeSpan.FromSeconds(10));

		// Becoming healthy must reset the restart counter: with max=1 and a single healthy blip the
		// supervision loop can spawn at least one more process than the raw limit would allow.
		monitor.ServerStatus = monitor.ServerStatus with { Health = PhoriaServerHealth.Healthy };
		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
		monitor.ServerStatus = monitor.ServerStatus with { Health = PhoriaServerHealth.Unhealthy };

		await startTask.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);

		Assert.True(hookCalls >= 3, $"Expected at least 3 spawns after a healthy reset, got {hookCalls}.");
	}

	[Fact]
	public async Task StartServer_SupervisionLoop_DoesNotKillRunningProcess()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Skip("Graceful SIGTERM-based stop is Unix-only.");
		}

		string markerPath = CreateMarkerPath(nameof(StartServer_SupervisionLoop_DoesNotKillRunningProcess));
		using var child = StartNode(LongLivedNodeScript(markerPath));
		await WaitForMarker(markerPath, "ready");

		// Seed the live process as the supervised process; while it runs, the loop must never terminate it.
		using PhoriaServerProcess serverProcess = CreateServerProcess(
			processId: child.Id,
			maxRestartAttempts: 1);
		using var cts = new CancellationTokenSource();
		Task startTask = serverProcess.StartServer(cts.Token);

		await Task.Delay(TimeSpan.FromSeconds(1.5), TestContext.Current.CancellationToken);
		Assert.False(child.HasExited, "The supervision loop must not terminate a running process.");

		cts.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(
			() => startTask.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));

		Assert.True(child.HasExited);
	}

	private static PhoriaServerProcess CreateServerProcess(
		int? processId = null,
		TimeSpan? stopGracePeriod = null,
		string? script = null,
		Action<int>? beforeProcessIdAssignment = null,
		IPhoriaServerMonitor? monitor = null,
		int maxRestartAttempts = 0)
	{
		return new PhoriaServerProcess(
			NullLogger<PhoriaServerProcess>.Instance,
			monitor ?? new StubServerMonitor(),
			new StubHostEnvironment(),
			Options.Create(CreateProcessOptions(script ?? "setInterval(() => {}, 1000);", maxRestartAttempts)),
			processId,
			stopGracePeriod ?? TimeSpan.FromSeconds(6),
			beforeProcessIdAssignment);
	}

	private static PhoriaServerProcess CreateServerProcessViaPublicConstructor(string script)
	{
		return new PhoriaServerProcess(
			NullLogger<PhoriaServerProcess>.Instance,
			new StubServerMonitor(),
			new StubHostEnvironment(),
			Options.Create(CreateProcessOptions(script)));
	}

	private static PhoriaOptions CreateProcessOptions(string script, int maxRestartAttempts = 0)
	{
		return new PhoriaOptions
		{
			Server = new PhoriaServerOptions
			{
				Process = new PhoriaServerOptions.ProcessOptions
				{
					Command = "node",
					Arguments = ["-e", script],
					HealthCheckInterval = 1,
					MaxRestartAttempts = maxRestartAttempts
				}
			}
		};
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

	private static string SlowGracefulNodeScript(string markerPath, string completionMarkerPath, string pidPath) =>
		$$"""
		process.on('SIGTERM', () => {
			require('fs').writeFileSync("{{markerPath}}", 'sigterm');
			setTimeout(() => {
				require('fs').writeFileSync("{{completionMarkerPath}}", 'sigterm-done');
				process.exit(0);
			}, 2000);
		});
		require('fs').writeFileSync("{{pidPath}}", String(process.pid));
		require('fs').writeFileSync("{{markerPath}}", 'ready');
		setInterval(() => {}, 1000);
		""";

	private static string StartServerIgnoringNodeScript(string markerPath, string pidPath) =>
		$$"""
		process.on('SIGTERM', () => {
			require('fs').writeFileSync("{{markerPath}}", 'sigterm');
		});
		require('fs').writeFileSync("{{pidPath}}", String(process.pid));
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

	private static string StartupNodeScript(string markerPath) =>
		$$"""
		process.on('SIGTERM', () => {
			require('fs').writeFileSync("{{markerPath}}", 'sigterm');
			process.exit(0);
		});
		require('fs').writeFileSync("{{markerPath}}", 'starting');
		setTimeout(() => {}, 10000);
		""";

	private static string LongLivedNodeScript(string markerPath) =>
		$$"""
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

	private static bool IsProcessRunning(int processId)
	{
		try
		{
			using Process process = Process.GetProcessById(processId);
			return !process.HasExited;
		}
		catch (ArgumentException)
		{
			return false;
		}
	}

	private sealed class StubHostEnvironment : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = Environments.Development;

		public string ApplicationName { get; set; } = "Phoria.Tests";

		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
