using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
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

	[Fact]
	public void AddPhoria_UsesDangerousCertificateValidationOnlyInDevelopment()
	{
		Assert.Equal(
			HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
			GetPrimaryHandler(Environments.Development).ServerCertificateCustomValidationCallback);
		Assert.Null(GetPrimaryHandler(Environments.Production).ServerCertificateCustomValidationCallback);
	}

	private static void Write(StreamPool pool, string value)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(value);
		pool.Stream.Write(bytes);
	}

	private static HttpClientHandler GetPrimaryHandler(string environmentName)
	{
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddSingleton<IHostEnvironment>(new TestHostEnvironment { EnvironmentName = environmentName });
		services.AddPhoria();

		using ServiceProvider provider = services.BuildServiceProvider();
		HttpMessageHandler handler = provider
			.GetRequiredService<IHttpMessageHandlerFactory>()
			.CreateHandler("PhoriaServerHttpClient");

		while (handler is DelegatingHandler delegatingHandler)
		{
			handler = delegatingHandler.InnerHandler!;
		}

		return Assert.IsType<HttpClientHandler>(handler);
	}

	private sealed class TestHostEnvironment : IHostEnvironment
	{
		public string EnvironmentName { get; set; } = string.Empty;

		public string ApplicationName { get; set; } = "Phoria.Tests";

		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
}
