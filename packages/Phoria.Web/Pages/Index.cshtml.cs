using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Phoria.WebPages.Pages;

public class IndexModel : PageModel
{
	private readonly ILogger<IndexModel> logger;

	public bool ShowReactComponent { get; set; } = true;
	public bool ShowVueComponent { get; set; } = true;
	public bool ShowSvelteComponent { get; set; } = true;

	public IndexModel(ILogger<IndexModel> logger)
	{
		this.logger = logger;
	}

	public void OnGet([FromQuery] string[]? framework)
	{
		if (framework == null || framework.Length == 0)
		{
			return;
		}

		ShowReactComponent = framework.Contains("react", StringComparer.OrdinalIgnoreCase);
		ShowVueComponent = framework.Contains("vue", StringComparer.OrdinalIgnoreCase);
		ShowSvelteComponent = framework.Contains("svelte", StringComparer.OrdinalIgnoreCase);
	}
}
