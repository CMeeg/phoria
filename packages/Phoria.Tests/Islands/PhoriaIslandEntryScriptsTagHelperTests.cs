using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandEntryScriptsTagHelperTests
{
	[Fact]
	public void Process_SetsScriptAttributesAndUsesEntryOption()
	{
		var tagHelper = CreateTagHelper(new PhoriaOptions { Entry = "src/entry-client.ts" }, new StubServerMonitor(PhoriaServerMode.Development));
		var output = TagHelperTestFactory.CreateTagHelperOutput("phoria-island-scripts", new TagHelperAttribute("phoria-src", "ignored"));
		tagHelper.ViewContext = new ViewContext { View = new StubView() };

		tagHelper.Process(TagHelperTestFactory.CreateTagHelperContext(), output);

		Assert.Equal("script", output.TagName);
		Assert.Equal("module", output.Attributes["type"]?.Value);
		Assert.Equal("src/entry-client.ts", tagHelper.PhoriaSrc);
	}

	[Fact]
	public void Process_DevelopmentModeRendersViteClientAndEntry()
	{
		var options = new PhoriaOptions { Entry = "src/entry-client.ts" };
		var tagHelper = CreateTagHelper(options, new StubServerMonitor(PhoriaServerMode.Development));
		tagHelper.ViewContext = new ViewContext { View = new StubView() };
		var output = TagHelperTestFactory.CreateTagHelperOutput("phoria-island-scripts");

		tagHelper.Process(TagHelperTestFactory.CreateTagHelperContext(), output);

		Assert.Contains("/@vite/client", output.PreElement.GetContent());
		Assert.Contains("/src/entry-client.ts", output.Attributes["src"]?.Value.ToString());
	}

	[Fact]
	public void Process_ProductionModeRendersManifestFile()
	{
		var options = new PhoriaOptions { Entry = "src/entry-client.ts", Base = "/ui" };
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry-client.ts"] = new() { File = "assets/entry-abc.js" }
		});
		var tagHelper = CreateTagHelper(options, new StubServerMonitor(PhoriaServerMode.Production), new StubManifestReader(manifest));
		tagHelper.ViewContext = new ViewContext { View = new StubView() };
		var output = TagHelperTestFactory.CreateTagHelperOutput("phoria-island-scripts");

		tagHelper.Process(TagHelperTestFactory.CreateTagHelperContext(), output);

		Assert.Equal("/ui/assets/entry-abc.js", output.Attributes["src"]?.Value.ToString());
	}

	[Fact]
	public void Process_ProductionMissingManifestKeyLogsAndSuppresses()
	{
		var logger = new ListLogger<PhoriaIslandEntryScriptsTagHelper>();
		var tagHelper = CreateTagHelper(new PhoriaOptions { Entry = "src/missing.ts" }, new StubServerMonitor(PhoriaServerMode.Production), new StubManifestReader(new ViteManifest()), logger);
		tagHelper.ViewContext = new ViewContext { View = new StubView() };
		var output = TagHelperTestFactory.CreateTagHelperOutput("phoria-island-scripts");

		tagHelper.Process(TagHelperTestFactory.CreateTagHelperContext(), output);

		Assert.Empty(output.Content.GetContent());
		Assert.Contains(logger.Messages, message => message.Contains("not found", StringComparison.OrdinalIgnoreCase));
	}

	private static PhoriaIslandEntryScriptsTagHelper CreateTagHelper(
		PhoriaOptions options,
		IPhoriaServerMonitor monitor,
		IViteManifestReader? manifestReader = null,
		ILogger<PhoriaIslandEntryScriptsTagHelper>? logger = null) =>
		new(
			logger ?? new ListLogger<PhoriaIslandEntryScriptsTagHelper>(),
			manifestReader ?? new StubManifestReader(new ViteManifest()),
			monitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			Options.Create(options),
			new StubUrlHelperFactory(new StubUrlHelper()));

	private sealed class StubView : IView
	{
		public string Path => "test";

		public Task RenderAsync(ViewContext context) => Task.CompletedTask;
	}
}
