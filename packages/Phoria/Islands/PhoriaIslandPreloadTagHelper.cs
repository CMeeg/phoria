using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Phoria.Vite;

namespace Phoria.Islands;

public class PhoriaIslandPreloadTagHelper
	: TagHelper
{
	private readonly IPhoriaServerMonitor serverMonitor;
	private readonly IPhoriaIslandScopedContext scopedContext;
	private readonly IViteSsrManifestReader ssrManifestReader;
	private readonly IUrlHelperFactory urlHelperFactory;
	private readonly PhoriaOptions options;

	/// <inheritdoc />
	[ViewContext]
	[HtmlAttributeNotBound]
	public ViewContext ViewContext { get; set; } = default!;

	public PhoriaIslandPreloadTagHelper(
		IPhoriaServerMonitor serverMonitor,
		IPhoriaIslandScopedContext scopedContext,
		IViteSsrManifestReader ssrManifestReader,
		IOptions<PhoriaOptions> options,
		IUrlHelperFactory urlHelperFactory)
	{
		this.serverMonitor = serverMonitor;
		this.scopedContext = scopedContext;
		this.ssrManifestReader = ssrManifestReader;
		this.urlHelperFactory = urlHelperFactory;
		this.options = options.Value;
	}

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		output.TagName = null;
		output.TagMode = TagMode.StartTagAndEndTag;

		if (serverMonitor.ServerStatus.Mode != PhoriaServerMode.Production)
		{
			// Only production will have a usable SSR manifest

			return;
		}

		// Generate preload directives for all islands in the current scoped context
		// See https://vite.dev/guide/ssr#generating-preload-directives
		// TODO: Also add Early hints for the preload directives as suggested in the above guide?

		IViteSsrManifest ssrManifest = ssrManifestReader.ReadSsrManifest();

		string basePath = options.GetBasePath().TrimStart('/');

		HashSet<string> seenFiles = new();

		foreach (PhoriaIslandComponent component in scopedContext.Components)
		{
			if (string.IsNullOrEmpty(component.ComponentPath))
			{
				continue;
			}

			string componentPath = component.ComponentPath.TrimStart('/');

			// We then need to remove the base path to get the island's module ID

			if (!string.IsNullOrEmpty(basePath)
				&& componentPath.StartsWith(basePath, StringComparison.InvariantCulture))
			{
				componentPath = componentPath[basePath.Length..].TrimStart('/');
			}

			// Now we can see if the module exists in the SSR manifest

			string[]? files = ssrManifest[componentPath];

			if (files == null)
			{
				continue;
			}

			// Generate a preload directive for each of the module's files

			var urlHelper = new PhoriaIslandUrlHelper(
				urlHelperFactory.GetUrlHelper(ViewContext),
				options);

			foreach (string file in files)
			{
				if (seenFiles.Contains(file))
				{
					continue;
				}

				seenFiles.Add(file);

				string filename = Path.GetFileName(file);

				// Check if the file has any dependencies that we need to preload first

				string[]? depFiles = ssrManifest[filename];

				if (depFiles != null)
				{
					foreach (string depFile in depFiles)
					{
						// Generate a preload directive for the dependency

						output.Content.AppendHtml(new PhoriaIslandPreloadHtmlContent(urlHelper, depFile));

						seenFiles.Add(depFile);
					}
				}

				// Generate a preload directive for the file itself

				output.Content.AppendHtml(new PhoriaIslandPreloadHtmlContent(urlHelper, file));
			}
		}
	}
}
