using Microsoft.AspNetCore.Razor.TagHelpers;
using Phoria.Islands;

namespace WebApp.Components;

public class ReactCounterTagHelper : TagHelper
{
	private readonly IPhoriaIslandComponentFactory phoriaIslandFactory;

	public int? StartAt { get; set; }

	public ReactCounterTagHelper(IPhoriaIslandComponentFactory phoriaIslandFactory)
	{
		this.phoriaIslandFactory = phoriaIslandFactory;
	}

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		var props = new ReactCounterProps
		{
			StartAt = StartAt ?? 0
		};

		PhoriaIslandHtmlContent island = await phoriaIslandFactory.CreateAsync("ReactCounter", props, Client.Load);

		output.TagName = null;
		output.TagMode = TagMode.SelfClosing;

		output.Content.SetHtmlContent(island);
	}
}
