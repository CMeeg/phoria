using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Phoria.Islands;

namespace WebApp.Components;

public class ReactCounterProps
{
	public int StartAt { get; set; }
}

public class ReactCounterViewComponent : ViewComponent
{
	private readonly IPhoriaIslandComponentFactory phoriaIslandFactory;

	public ReactCounterViewComponent(IPhoriaIslandComponentFactory phoriaIslandFactory)
	{
		this.phoriaIslandFactory = phoriaIslandFactory;
	}

	public async Task<IViewComponentResult> InvokeAsync(int? startAt)
	{
		var props = new ReactCounterProps
		{
			StartAt = startAt ?? 0
		};

		PhoriaIslandHtmlContent island = await phoriaIslandFactory.CreateAsync("ReactCounter", props, Client.Load);

		return new HtmlContentViewComponentResult(island);
	}
}
