using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Tests.TestUtilities;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandPreloadHtmlContentTests
{
	[Theory]
	[InlineData("app.js")]
	[InlineData("app.css")]
	[InlineData("font.woff")]
	[InlineData("font.woff2")]
	[InlineData("image.gif")]
	[InlineData("image.jpg")]
	[InlineData("image.jpeg")]
	[InlineData("image.png")]
	public void WriteTo_SupportedExtensionsEmitsLink(string file)
	{
		var content = new PhoriaIslandPreloadHtmlContent(
			new PhoriaIslandUrlHelper(new StubUrlHelper(), new PhoriaOptions { Base = "/ui" }),
			$"assets/{file}");
		using var writer = new StringWriter();

		content.WriteTo(writer, HtmlEncoder.Default);

		Assert.Contains($"href=\"/ui/assets/{file}\"", writer.ToString());
	}

	[Theory]
	[InlineData("app.svg")]
	[InlineData("data.bin")]
	public void WriteTo_UnsupportedExtensionIsEmpty(string file)
	{
		var content = new PhoriaIslandPreloadHtmlContent(
			new PhoriaIslandUrlHelper(new StubUrlHelper(), new PhoriaOptions { Base = "/ui" }),
			file);
		using var writer = new StringWriter();

		content.WriteTo(writer, HtmlEncoder.Default);

		Assert.Empty(writer.ToString());
	}
}
