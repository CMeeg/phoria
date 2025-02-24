using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Islands;

public interface IPhoriaIslandComponentFactory
{
	Task<PhoriaIslandHtmlContent> CreateAsync(
		string component,
		object? props,
		PhoriaIslandClientDirective? client);
}

public class PhoriaIslandComponentFactory(
	IPhoriaServerMonitor serverMonitor,
	IPhoriaIslandScopedContext scopedContext,
	IPhoriaIslandSsr phoriaIslandSsr,
	IOptions<PhoriaOptions> options)
	: IPhoriaIslandComponentFactory
{
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IPhoriaIslandScopedContext scopedContext = scopedContext;
	private readonly IPhoriaIslandSsr phoriaIslandSsr = phoriaIslandSsr;
	private readonly PhoriaOptions options = options.Value;

	public async Task<PhoriaIslandHtmlContent> CreateAsync(
		string component,
		object? props,
		PhoriaIslandClientDirective? client)
	{
		PhoriaIslandRenderMode renderMode = PhoriaIslandRenderMode.Isomorphic;

		if (client == null)
		{
			renderMode = PhoriaIslandRenderMode.ServerOnly;
		}
		else if (client is PhoriaIslandClientOnlyDirective)
		{
			renderMode = PhoriaIslandRenderMode.ClientOnly;
		}

		if (renderMode != PhoriaIslandRenderMode.ServerOnly
			&& serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			// We have to render the component on the server, but the server is not healthy

			throw new PhoriaIslandComponentException($"Cannot render component '{component}' on the server because the server is not healthy. Server status is '{serverMonitor.ServerStatus.Health}'.");
		}

		var island = new PhoriaIsland
		{
			ComponentName = component,
			Props = props,
			RenderMode = renderMode,
			Client = client
		};

		scopedContext.AddIsland(island);

		PhoriaIslandSsrResult? ssrResult = island.RenderMode != PhoriaIslandRenderMode.ClientOnly
			? await phoriaIslandSsr.RenderIsland(island)
			: null;

		return new PhoriaIslandHtmlContent(
			island,
			ssrResult,
			options);
	}
}
