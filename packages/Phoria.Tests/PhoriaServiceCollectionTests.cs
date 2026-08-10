using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests;

public class PhoriaServiceCollectionTests
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(true, true)]
	public void AddPhoria_ConfiguresHealthCheckHttpClientLogging(bool logHealthChecks, bool informationEnabled)
	{
		ServiceCollection services = new();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddLogging(logging => logging.AddConsole());
		services.AddPhoria();
		services.Configure<PhoriaObservabilityOptions>(options => options.LogHealthChecks = logHealthChecks);

		using ServiceProvider provider = services.BuildServiceProvider();
		ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
		ILogger healthCheckLogger = loggerFactory.CreateLogger(
			"System.Net.Http.HttpClient.PhoriaServerHealthCheckHttpClient.LogicalHandler");
		ILogger serverLogger = loggerFactory.CreateLogger(
			"System.Net.Http.HttpClient.PhoriaServerHttpClient.LogicalHandler");

		Assert.Equal(informationEnabled, healthCheckLogger.IsEnabled(LogLevel.Information));
		Assert.True(healthCheckLogger.IsEnabled(LogLevel.Warning));
		Assert.True(serverLogger.IsEnabled(LogLevel.Information));
	}
}
