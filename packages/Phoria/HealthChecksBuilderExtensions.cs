using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Phoria.Diagnostics.HealthChecks;

namespace Phoria;

public static class HealthChecksBuilderExtensions
{
	public static IHealthChecksBuilder AddPhoriaServerHealthCheck(
		this IHealthChecksBuilder builder,
		string name = "phoria-server")
	{
		ArgumentNullException.ThrowIfNull(builder);

		builder.AddCheck<PhoriaServerHealthCheck>(name);

		return builder;
	}
}
