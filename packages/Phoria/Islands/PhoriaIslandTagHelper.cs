using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Islands;

public class PhoriaIslandTagHelper
	: TagHelper
{
	private readonly IPhoriaIslandRegistry registry;
	private readonly IPhoriaServerMonitor serverMonitor;
	private readonly IPhoriaIslandSsr phoriaIslandSsr;
	private readonly PhoriaOptions options;

	public required string Component { get; set; }
	public object? Props { get; set; }
	public ClientMode? Client { get; set; }

	public PhoriaIslandTagHelper(
		IPhoriaIslandRegistry registry,
		IPhoriaServerMonitor serverMonitor,
		IPhoriaIslandSsr phoriaIslandSsr,
		IOptions<PhoriaOptions> options)
	{
		this.registry = registry;
		this.serverMonitor = serverMonitor;
		this.phoriaIslandSsr = phoriaIslandSsr;
		this.options = options.Value;
	}

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		PhoriaIslandRenderMode renderMode = Client switch
		{
			ClientMode.OnLoad => PhoriaIslandRenderMode.Isomorphic,
			ClientMode.Only => PhoriaIslandRenderMode.ClientOnly,
			_ => PhoriaIslandRenderMode.ServerOnly
		};

		if (renderMode != PhoriaIslandRenderMode.ServerOnly
			&& serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			// We have to render the component on the server, but the server is not healthy

			// TODO: Log this
			// TODO: Throw an exception?

			output.SuppressOutput();

			return;
		}

		var component = new PhoriaIslandComponent
		{
			ComponentName = Component,
			Props = Props,
			RenderMode = renderMode,
			Client = Client
		};

		registry.RegisterComponent(component);

		output.TagName = null;
		output.TagMode = TagMode.StartTagAndEndTag;

		PhoriaIslandSsrResult? ssrResult = component.RenderMode != PhoriaIslandRenderMode.ClientOnly
			? await phoriaIslandSsr.RenderComponent(component)
			: null;

		var content = new PhoriaIslandHtmlContent(
			component,
			ssrResult,
			options);

		output.Content.SetHtmlContent(content);
	}
}