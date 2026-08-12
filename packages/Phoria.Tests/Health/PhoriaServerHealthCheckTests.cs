using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Phoria.Health;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Health;

public class PhoriaServerHealthCheckTests
{
	[Fact]
	public async Task CheckHealthAsync_HealthyServer_ReportsHealthy()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			Options.Create(new PhoriaOptions()));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Healthy, result.Status);
	}

	[Fact]
	public async Task CheckHealthAsync_UnhealthyServerUnderDegradePolicy_ReportsDegraded()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			Options.Create(new PhoriaOptions()));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Degraded, result.Status);
	}

	[Fact]
	public async Task CheckHealthAsync_UnhealthyServerUnderFailPolicy_ReportsUnhealthy()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
	}

	[Fact]
	public async Task CheckHealthAsync_UnknownServerUnderFailPolicy_ReportsUnhealthy()
	{
		var check = new PhoriaServerHealthCheck(
			new StubServerMonitor(PhoriaServerHealth.Unknown),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}));

		HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext());

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
	}

	private sealed class StubServerMonitor(PhoriaServerHealth health) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = health,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}
}
