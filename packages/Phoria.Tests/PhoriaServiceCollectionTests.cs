using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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

	[Theory]
	[InlineData("Development", true)]
	[InlineData("Production", false)]
	public void AddPhoria_UsesDangerousCertificateValidationOnlyInDevelopment(string environmentName, bool expectsDangerousAcceptAny)
	{
		HttpClientHandler handler = GetPrimaryHandler(environmentName);

		Assert.Equal(
			expectsDangerousAcceptAny ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator : null,
			handler.ServerCertificateCustomValidationCallback);
	}

	private static HttpClientHandler GetPrimaryHandler(string environmentName)
	{
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { EnvironmentName = environmentName });
		services.AddPhoria();

		using ServiceProvider provider = services.BuildServiceProvider();
		HttpMessageHandler handler = provider
			.GetRequiredService<IHttpMessageHandlerFactory>()
			.CreateHandler("PhoriaServerHttpClient");

		while (handler is DelegatingHandler delegatingHandler)
		{
			handler = delegatingHandler.InnerHandler!;
		}

		return Assert.IsType<HttpClientHandler>(handler);
	}

	private sealed class TestHostEnvironment : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = string.Empty;

		public string ApplicationName { get; set; } = "Phoria.Tests";

		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
