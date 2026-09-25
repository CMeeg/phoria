using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Server;

namespace Phoria.Islands;

public class PhoriaIslandTagHelper(
	IPhoriaIslandComponentFactory phoriaIslandComponentFactory,
	IOptions<PhoriaOptions> options)
	: TagHelper
{
	private readonly IPhoriaIslandComponentFactory phoriaIslandComponentFactory = phoriaIslandComponentFactory;
	private readonly PhoriaOptions options = options.Value;

	public required string Component { get; set; }
	public object? Props { get; set; }
	public PhoriaIslandClientDirective? Client { get; set; }

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		try
		{
			PhoriaIslandHtmlContent content = await phoriaIslandComponentFactory.CreateAsync(
				Component,
				Props,
				Client);

			output.TagName = null;
			output.TagMode = TagMode.StartTagAndEndTag;

			output.Content.SetHtmlContent(content);
		}
		catch (PhoriaIslandComponentException)
		{
			// Under the Fail policy the page 500s instead of silently dropping the island; under
			// Degrade the factory has already logged, so suppressing the element is intentional.

			if (options.Server.UnavailableBehavior == PhoriaServerUnavailableBehavior.Fail)
			{
				throw;
			}

			output.SuppressOutput();

			return;
		}
	}
}
