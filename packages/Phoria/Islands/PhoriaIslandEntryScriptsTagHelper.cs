using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Phoria.Vite;

namespace Phoria.Islands;

[HtmlTargetElement("phoria-island-scripts", TagStructure = TagStructure.NormalOrSelfClosing)]
public class PhoriaIslandEntryScriptsTagHelper(
	ILogger<PhoriaIslandEntryScriptsTagHelper> logger,
	IViteManifestReader manifestReader,
	IPhoriaServerMonitor serverMonitor,
	PhoriaIslandEntryTagHelperMonitor tagHelperMonitor,
	IOptions<PhoriaOptions> options,
	IUrlHelperFactory urlHelperFactory)
	: PhoriaIslandEntryTagHelper(logger, manifestReader, serverMonitor, tagHelperMonitor, options, urlHelperFactory)
{
	private readonly PhoriaOptions options = options.Value;

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		output.TagName = "script";
		output.Attributes.SetAttribute("type", "module");
		output.TagMode = TagMode.StartTagAndEndTag;

		PhoriaSrc = options.Entry;

		base.Process(context, output);
	}
}
