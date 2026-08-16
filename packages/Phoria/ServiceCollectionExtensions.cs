using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Vite;

namespace Phoria;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPhoria(
		this IServiceCollection services,
		Action<PhoriaOptions>? configure = null)
	{
		services.AddOptions<PhoriaOptions>()
			.BindConfiguration(PhoriaOptions.SectionName)
			.Configure(configure ?? (_ => { }));

		services.AddOptions<PhoriaObservabilityOptions>()
			.BindConfiguration(PhoriaObservabilityOptions.SectionName);

		return services.ConfigureServices();
	}

	private static IServiceCollection ConfigureServices(this IServiceCollection services)
	{
		// Add http client factory if not already added

		if (services.All(x => x.ServiceType != typeof(IHttpClientFactory)))
		{
			services.AddHttpClient();
		}

		// Add HttpClients for the Phoria Server and its health monitor

		services.AddPhoriaServerHttpClient(PhoriaServerHttpClientFactory.HttpClientName);
		services.AddPhoriaServerHttpClient(PhoriaServerHttpClientFactory.HealthCheckHttpClientName);
		services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>, PhoriaHttpClientLoggingFilter>());

		// Add Server services

		services.TryAddScoped<IViteDevServerHmrProxy, ViteDevServerHmrProxy>();
		services.TryAddSingleton<PhoriaServerHttpClientFactory>();
		services.TryAddSingleton<IPhoriaServerHttpClientFactory>(services => services.GetRequiredService<PhoriaServerHttpClientFactory>());
		services.TryAddSingleton<IPhoriaServerHealthCheckHttpClientFactory>(services => services.GetRequiredService<PhoriaServerHttpClientFactory>());
		services.TryAddSingleton<IPhoriaServerProcess, PhoriaServerProcess>();
		services.TryAddSingleton<IPhoriaServerMonitor>(services => new PhoriaServerMonitor(
			services.GetRequiredService<ILogger<PhoriaServerMonitor>>(),
			services.GetRequiredService<IOptions<PhoriaOptions>>(),
			services.GetRequiredService<IPhoriaServerHealthCheckHttpClientFactory>(),
			services.GetRequiredService<IOptions<PhoriaObservabilityOptions>>()));
		services.AddHostedService<PhoriaServerProcessService>();
		services.AddHostedService<PhoriaServerMonitorService>();

		// Add Vite services

		services.TryAddSingleton<IViteManifestReader, ViteManifestReader>();
		services.TryAddSingleton<IViteSsrManifestReader, ViteSsrManifestReader>();

		// Add Islands services

		services.TryAddScoped<PhoriaIslandEntryTagHelperMonitor>();
		services.TryAddSingleton<IPhoriaIslandSsr, PhoriaIslandSsr>();
		services.TryAddScoped<IPhoriaIslandScopedContext, PhoriaIslandScopedContext>();
		services.TryAddScoped<IPhoriaIslandComponentFactory, PhoriaIslandComponentFactory>();

		return services;
	}

	private static IHttpClientBuilder AddPhoriaServerHttpClient(this IServiceCollection services, string name) =>
		services.AddHttpClient(name)
			.ConfigurePrimaryHttpMessageHandler(services => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = services.GetRequiredService<IHostEnvironment>().IsDevelopment()
					? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
					: null
			})
			.ConfigureHttpClient((_, client) =>
				client.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("*/*", 0.1)
				));

	private sealed class PhoriaHttpClientLoggingFilter(IOptions<PhoriaObservabilityOptions> observabilityOptions)
		: IConfigureOptions<LoggerFilterOptions>
	{
		public void Configure(LoggerFilterOptions options)
		{
			options.Rules.Add(new LoggerFilterRule(
				providerName: null,
				categoryName: $"System.Net.Http.HttpClient.{PhoriaServerHttpClientFactory.HealthCheckHttpClientName}",
				logLevel: observabilityOptions.Value.LogHealthChecks ? LogLevel.Information : LogLevel.Warning,
				filter: null));
		}
	}
}
