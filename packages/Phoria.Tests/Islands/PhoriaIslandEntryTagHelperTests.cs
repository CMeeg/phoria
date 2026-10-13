using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandEntryTagHelperTests
{
	[Fact]
	public void Process_LinkWithoutCssChunks_LogsWarningAndSuppressesOutput()
	{
		// Arrange
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			serverMonitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaHref = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = TagHelperTestFactory.CreateTagHelperContext();
		TagHelperOutput output = TagHelperTestFactory.CreateTagHelperOutput("link", new TagHelperAttribute("rel", "stylesheet"));

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Contains(logger.Messages, message => message.Contains("doesn't have CSS chunks", StringComparison.Ordinal));
		Assert.Empty(output.Content.GetContent());
	}

	[Fact]
	public void Process_LinkWithCssChunks_RendersStylesheetLink()
	{
		// Arrange
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js", Css = ["assets/entry.css"] }
		});
		var manifestReader = new StubManifestReader(manifest);
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			serverMonitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaHref = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = TagHelperTestFactory.CreateTagHelperContext();
		TagHelperOutput output = TagHelperTestFactory.CreateTagHelperOutput("link", new TagHelperAttribute("rel", "stylesheet"));

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Equal("/ui/assets/entry.css", output.Attributes["href"]?.Value.ToString());
	}

	[Fact]
	public void Process_EntryStylesWithoutCssChunks_DoesNotLogWarning()
	{
		// Arrange
		var logger = new ListLogger<PhoriaIslandEntryStylesTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui", Entry = "src/entry.ts" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryStylesTagHelper(
			logger,
			manifestReader,
			serverMonitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory);
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = TagHelperTestFactory.CreateTagHelperContext();
		TagHelperOutput output = TagHelperTestFactory.CreateTagHelperOutput("phoria-island-styles");

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.DoesNotContain("doesn't have CSS chunks", logger.Messages);
		Assert.Empty(output.Content.GetContent());
	}

	[Fact]
	public void Process_ServerUnhealthy_SuppressesOutputAndLogsWarning()
	{
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaSrc = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext { View = new StubView() };

		TagHelperContext context = TagHelperTestFactory.CreateTagHelperContext();
		TagHelperOutput output = TagHelperTestFactory.CreateTagHelperOutput("script");

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Contains(logger.Messages, message => message.Contains("suppressing", StringComparison.Ordinal));
		Assert.Empty(output.Content.GetContent());
		Assert.Null(output.Attributes["src"]);
	}

	private sealed class StubView : IView
	{
		public string Path => "test";

		public Task RenderAsync(ViewContext context) => Task.CompletedTask;
	}
}
