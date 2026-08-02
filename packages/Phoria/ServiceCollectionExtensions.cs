using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

		return services.ConfigureServices();
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
			.ConfigurePrimaryHttpMessageHandler(services => new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = services.GetRequiredService<IHostEnvironment>().IsDevelopment()
					? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
					: null
			})
			.ConfigureHttpClient((services, client) =>
				client.DefaultRequestHeaders.Accept.Add(
					new MediaTypeWithQualityHeaderValue("*/*", 0.1)
				)
			);

		// Add Server services

		services.TryAddScoped<IViteDevServerHmrProxy, ViteDevServerHmrProxy>();
		services.TryAddSingleton<IPhoriaServerHttpClientFactory, PhoriaServerHttpClientFactory>();
		services.TryAddSingleton<IPhoriaServerProcess, PhoriaServerProcess>();
		services.TryAddSingleton<IPhoriaServerMonitor, PhoriaServerMonitor>();
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
}
