using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApp.Pages;

public class IndexModel(ILogger<IndexModel> logger) : PageModel
{
	private readonly ILogger<IndexModel> logger = logger;

	public bool ShowReactComponent { get; set; } = true;
	public bool ShowVueComponent { get; set; } = true;
	public bool ShowSvelteComponent { get; set; } = true;

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
