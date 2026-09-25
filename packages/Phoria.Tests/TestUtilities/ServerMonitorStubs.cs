using Phoria.Server;

namespace Phoria.Tests.TestUtilities;

internal sealed class StubServerMonitor : IPhoriaServerMonitor
{
	public StubServerMonitor(
		PhoriaServerHealth health = PhoriaServerHealth.Unhealthy,
		string url = "http://localhost:5173")
	{
		ServerStatus = new PhoriaServerStatus
		{
			Health = health,
			Url = url
		};
	}

	public StubServerMonitor(
		PhoriaServerMode mode,
		string url = "http://localhost:5173")
	{
		ServerStatus = new PhoriaServerStatus
		{
			Health = PhoriaServerHealth.Healthy,
			Mode = mode,
			Url = url
		};
	}

	public PhoriaServerStatus ServerStatus { get; }

	public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;

	public Task StopMonitoring() => Task.CompletedTask;
}

internal sealed class SettableServerMonitor : IPhoriaServerMonitor
{
	public PhoriaServerStatus ServerStatus { get; set; } = new()
	{
		Health = PhoriaServerHealth.Unhealthy,
		Url = "http://localhost:5173"
	};

	public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;

	public Task StopMonitoring() => Task.CompletedTask;
}
