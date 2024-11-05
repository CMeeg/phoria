using Microsoft.Extensions.DependencyInjection;
using Phoria.Islands;
using Phoria.Islands.Components;
using Phoria.Islands.Node;

namespace Phoria.islands;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPhoriaIslands(this IServiceCollection services,
		Action<PhoriaIslandsOptions>? configuration = null)
	{
		var config = new PhoriaIslandsOptions();
		configuration?.Invoke(config);

		services.AddSingleton(config);

		services.AddSingleton<IComponentNameValidator, ComponentNameValidator>();
		services.AddSingleton<IReactIdGenerator, ReactIdGenerator>();
		services.AddSingleton<INodeInvocationService, ViteInvocationService>();

		// services.AddNodeJS();
		// services.Configure<NodeJSProcessOptions>(options =>
		// {
		// 	options.EnvironmentVariables.Add("NODEREACT_FILEWATCHERDEBOUNCE", config.FileWatcherDebounceMs.ToString());

		// 	config.ConfigureNodeJSProcessOptionsAction?.Invoke(options);
		// });
		// services.Configure<OutOfProcessNodeJSServiceOptions>(options =>
		// {
		// 	options.Concurrency = Concurrency.MultiProcess;
		// 	options.ConcurrencyDegree = config.EnginesCount;

		// 	config.ConfigureOutOfProcessNodeJSServiceOptionsAction?.Invoke(options);
		// });
		// services.Configure<HttpNodeJSServiceOptions>(options =>
		// {
		// 	config.ConfigureHttpNodeJSServiceOptionsAction?.Invoke(options);
		// });


		// services.Replace(new ServiceDescriptor(
		// 	typeof(IJsonService),
		// 	typeof(NodeReactJeringNodeJsonService),
		// 	ServiceLifetime.Singleton));

		services.AddScoped<IPhoriaIslandRegistry, PhoriaIslandRegistry>();

		services.AddTransient<ComponentRenderer>();

		// services.AddTransient<ReactComponent>();
		// services.AddTransient<ReactRouterComponent>();

		return services;
	}
}
