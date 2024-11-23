using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Phoria.Vite;

namespace Phoria.Islands;

[HtmlTargetElement("phoria-island-styles", TagStructure = TagStructure.WithoutEndTag)]
public class PhoriaIslandEntryStylesTagHelper
	: PhoriaIslandEntryTagHelper
{
	private readonly PhoriaOptions options;

	public PhoriaIslandEntryStylesTagHelper(
		ILogger<PhoriaIslandEntryStylesTagHelper> logger,
		IViteManifestReader manifestReader,
		IPhoriaServerMonitor serverMonitor,
		PhoriaIslandEntryTagHelperMonitor tagHelperMonitor,
		IOptions<PhoriaOptions> options,
		IUrlHelperFactory urlHelperFactory)
		: base(logger, manifestReader, serverMonitor, tagHelperMonitor, options, urlHelperFactory)
	{
		this.options = options.Value;
	}

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		output.TagName = "link";
		output.Attributes.SetAttribute("rel", "stylesheet");
		output.TagMode = TagMode.SelfClosing;

		PhoriaHref = options.Entry;

		base.Process(context, output);
	}
}
