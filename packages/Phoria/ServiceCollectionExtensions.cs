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
	public static IServiceCollection AddPhoria(this IServiceCollection services)
	{
		return services.SetOptions().ConfigureServices();
	}

	public static IServiceCollection AddPhoria(
		this IServiceCollection services,
		Action<PhoriaOptions>? configure = null)
	{
		if (configure is null)
		{
			services.SetOptions();
		}
		else
		{
			services.Configure(configure);
		}

		return services.ConfigureServices();
	}

	public static IServiceCollection AddPhoria(
		this IServiceCollection services,
		PhoriaOptions options)
	{
		return services.SetOptions(options).ConfigureServices();
	}

	private static IServiceCollection SetOptions(
		this IServiceCollection services,
		PhoriaOptions? options = null)
	{
		if (options is null)
		{
			// Add the options from the configuration

			IServiceProvider serviceProvider = services.BuildServiceProvider();
			IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
			services.Configure<PhoriaOptions>(configuration.GetSection(PhoriaOptions.SectionName));
		}
		else
		{
			// Add the options provided

			services.AddSingleton(Options.Create(options));
		}

		return services;
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

		// Add Islands services

		services.TryAddScoped<PhoriaIslandsEntryTagHelperMonitor>();

		return services;
	}
}
