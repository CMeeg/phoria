using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Diagnostics.HealthChecks;

public sealed class PhoriaServerHealthCheck(
	IPhoriaServerMonitor serverMonitor,
	IOptions<PhoriaOptions> options)
	: IHealthCheck
{
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly PhoriaOptions options = options.Value;

	public Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		PhoriaServerStatus status = serverMonitor.ServerStatus;

		if (status.Health == PhoriaServerHealth.Healthy)
		{
			return Task.FromResult(HealthCheckResult.Healthy($"Phoria server at {status.Url} is healthy."));
		}

		string description = $"Phoria server at {status.Url} is {status.Health.ToString().ToLowerInvariant()}.";
		HealthCheckResult result = options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail
			? HealthCheckResult.Unhealthy(description)
			: HealthCheckResult.Degraded(description);

		return Task.FromResult(result);
	}
}
