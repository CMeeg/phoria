using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Islands;

public class PhoriaIslandTagHelper(
	IPhoriaIslandScopedContext scopedContext,
	IPhoriaServerMonitor serverMonitor,
	IPhoriaIslandSsr phoriaIslandSsr,
	IOptions<PhoriaOptions> options)
	: TagHelper
{
	private readonly IPhoriaIslandScopedContext scopedContext = scopedContext;
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IPhoriaIslandSsr phoriaIslandSsr = phoriaIslandSsr;
	private readonly PhoriaOptions options = options.Value;

	public required string Component { get; set; }
	public object? Props { get; set; }
	public PhoriaIslandClientDirective? Client { get; set; }

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		PhoriaIslandRenderMode renderMode = PhoriaIslandRenderMode.Isomorphic;

		if (Client == null)
		{
			renderMode = PhoriaIslandRenderMode.ServerOnly;
		}
		else if (Client is PhoriaIslandClientOnlyDirective)
		{
			renderMode = PhoriaIslandRenderMode.ClientOnly;
		}

		if (renderMode != PhoriaIslandRenderMode.ServerOnly
			&& serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			// We have to render the component on the server, but the server is not healthy

			// TODO: Log this or throw an exception?

			output.SuppressOutput();

			return;
		}

		var island = new PhoriaIsland
		{
			ComponentName = Component,
			Props = Props,
			RenderMode = renderMode,
			Client = Client
		};

		scopedContext.AddIsland(island);

		output.TagName = null;
		output.TagMode = TagMode.StartTagAndEndTag;

		PhoriaIslandSsrResult? ssrResult = island.RenderMode != PhoriaIslandRenderMode.ClientOnly
			? await phoriaIslandSsr.RenderIsland(island)
			: null;

		var content = new PhoriaIslandHtmlContent(
			island,
			ssrResult,
			options);

		output.Content.SetHtmlContent(content);
	}
}
