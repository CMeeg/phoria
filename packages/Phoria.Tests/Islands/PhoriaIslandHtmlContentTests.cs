using System.Text;
using System.Text.Encodings.Web;
using Phoria.IO;
using Phoria.Islands;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandHtmlContentTests
{
	[Fact]
	public void WriteTo_RendersContentAndDisposesSsrStreams()
	{
		var contentPool = new StreamPool();
		var propsPool = new StreamPool();
		Write(contentPool, "<span>Rendered</span>");
		Write(propsPool, "{\"value\":1}");

		var response = new HttpResponseMessage();
		var result = new PhoriaIslandSsrResult
		{
			Headers = response.Headers,
			Content = contentPool,
			Props = propsPool
		};
		var island = new PhoriaIsland
		{
			ComponentName = "Example",
			RenderMode = PhoriaIslandRenderMode.Isomorphic
		};
		var content = new PhoriaIslandHtmlContent(island, result, new PhoriaOptions());

		using var writer = new StringWriter();
		content.WriteTo(writer, HtmlEncoder.Default);

		Assert.Equal("<phoria-island component=\"Example\" props=\"{&quot;value&quot;:1}\"><span>Rendered</span></phoria-island>", writer.ToString());
		Assert.Throws<ObjectDisposedException>(() => _ = contentPool.Stream.Length);
		Assert.Throws<ObjectDisposedException>(() => _ = propsPool.Stream.Length);
	}

	private static void Write(StreamPool pool, string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		pool.Stream.Write(bytes);
	}
}
