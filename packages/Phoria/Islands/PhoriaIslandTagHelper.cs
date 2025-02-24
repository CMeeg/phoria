using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Phoria.Islands;

public class PhoriaIslandTagHelper(IPhoriaIslandComponentFactory phoriaIslandComponentFactory)
	: TagHelper
{
	private readonly IPhoriaIslandComponentFactory phoriaIslandComponentFactory = phoriaIslandComponentFactory;

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
			// TODO: Log or throw exception?

			output.SuppressOutput();

			return;
		}
	}
}
