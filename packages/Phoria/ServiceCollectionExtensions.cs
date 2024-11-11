using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
		// Create options from appsettings first

		IServiceProvider serviceProvider = services.BuildServiceProvider();
		IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

		var options = new PhoriaOptions();
		configuration.GetSection(PhoriaOptions.SectionName).Bind(options);

		// Then set options from the configure action

		configure?.Invoke(options);

		return services.AddSingleton(Options.Create(options)).ConfigureServices();
	}

	private static IServiceCollection ConfigureServices(this IServiceCollection services)
	{
		// Add http client factory if not already added

		if (services.All(x => x.ServiceType != typeof(IHttpClientFactory)))
		{
			services.AddHttpClient();
		}

		// Add an HttpClient for the Phoria Server

		services.AddHttpClient(PhoriaServerHttpClientFactory.HttpClientName)
			.ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
			})
			.ConfigureHttpClient((services, client) =>
				client.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("*/*", 0.1)
				)
			);

		// Add Server services

		services.TryAddSingleton<IPhoriaServerHttpClientFactory, PhoriaServerHttpClientFactory>();
		services.TryAddSingleton<IPhoriaServerMonitor, PhoriaServerMonitor>();
		services.AddHostedService<PhoriaServerMonitorService>();
		services.TryAddScoped<IViteDevServerHmrProxy, ViteDevServerHmrProxy>();

		// Add Vite services

		services.TryAddSingleton<IViteManifestReader, ViteManifestReader>();
		services.TryAddSingleton<IViteSsrManifestReader, ViteSsrManifestReader>();

		// Add Islands services

		services.TryAddScoped<PhoriaIslandEntryTagHelperMonitor>();
		services.TryAddSingleton<IPhoriaIslandSsr, PhoriaIslandSsr>();
		services.TryAddScoped<IPhoriaIslandScopedContext, PhoriaIslandScopedContext>();

		return services;
	}
}
