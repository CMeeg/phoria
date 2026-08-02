using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Phoria.IO;
using Phoria.Islands;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandSsrLifecycleTests
{
	[Fact]
	public async Task RenderIsland_DisposesPropsPoolWhenSerializationFails()
	{
		var serializer = new ThrowingPropsSerializer();
		var options = new PhoriaOptions();
		options.Islands.PropsSerializer = serializer;
		var ssr = new PhoriaIslandSsr(new StubHttpClientFactory(_ => throw new InvalidOperationException()), Options.Create(options));

		await Assert.ThrowsAsync<InvalidOperationException>(() => ssr.RenderIsland(new PhoriaIsland
		{
			ComponentName = "Example",
			Props = new object()
		}, TestContext.Current.CancellationToken));

		AssertDisposed(serializer.Pool!);
	}

	[Fact]
	public async Task RenderIsland_DisposesPropsPoolWhenPostFails()
	{
		var serializer = new CapturingPropsSerializer();
		var options = new PhoriaOptions();
		options.Islands.PropsSerializer = serializer;
		var ssr = new PhoriaIslandSsr(new StubHttpClientFactory(_ => throw new HttpRequestException()), Options.Create(options));

		await Assert.ThrowsAsync<HttpRequestException>(() => ssr.RenderIsland(new PhoriaIsland
		{
			ComponentName = "Example",
			Props = new object()
		}, TestContext.Current.CancellationToken));

		AssertDisposed(serializer.Pool!);
	}

	[Fact]
	public async Task RenderIsland_DisposesBothPoolsWhenResponseCopyFails()
	{
		var serializer = new CapturingPropsSerializer();
		var options = new PhoriaOptions();
		options.Islands.PropsSerializer = serializer;
		var ssr = new PhoriaIslandSsr(
			new StubHttpClientFactory(_ => new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new ThrowingHttpContent()
			}),
			Options.Create(options));

		await Assert.ThrowsAsync<InvalidOperationException>(() => ssr.RenderIsland(new PhoriaIsland
		{
			ComponentName = "Example",
			Props = new object()
		}, TestContext.Current.CancellationToken));

		AssertDisposed(serializer.Pool!);
	}

	[Fact]
	public async Task ComponentFactory_DisposesAbandonedContentAtScopeEnd()
	{
		var contentPool = new StreamPool();
		var result = new PhoriaIslandSsrResult
		{
			Headers = new HttpResponseMessage().Headers,
			Content = contentPool
		};
		var factory = new PhoriaIslandComponentFactory(
			new HealthyServerMonitor(),
			new PhoriaIslandScopedContext(),
			new StubSsr(result),
			Options.Create(new PhoriaOptions()));

		_ = await factory.CreateAsync("Example", null, null);
		factory.Dispose();

		AssertDisposed(contentPool);
	}

	private static void AssertDisposed(StreamPool pool) => Assert.Throws<ObjectDisposedException>(() => _ = pool.Stream.Length);

	private sealed class ThrowingPropsSerializer : IPhoriaIslandPropsSerializer
	{
		public StreamPool? Pool { get; private set; }

		public string Serialize(object props) => throw new NotSupportedException();

		public void Serialize(object props, StreamPool streamPool)
		{
			Pool = streamPool;
			throw new InvalidOperationException();
		}
	}

	private sealed class CapturingPropsSerializer : IPhoriaIslandPropsSerializer
	{
		public StreamPool? Pool { get; private set; }

		public string Serialize(object props) => "{}";

		public void Serialize(object props, StreamPool streamPool)
		{
			Pool = streamPool;
			streamPool.Stream.WriteByte((byte)'{');
			streamPool.Stream.WriteByte((byte)'}');
		}
	}

	private sealed class StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> send)
		: IPhoriaServerHttpClientFactory
	{
		public HttpClient CreateClient() => new(new StubHttpMessageHandler(send))
		{
			BaseAddress = new Uri("http://localhost")
		};
	}

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send)
		: HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(send(request));
	}

	private sealed class ThrowingHttpContent : HttpContent
	{
		protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => throw new InvalidOperationException();

		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return true;
		}
	}

	private sealed class StubSsr(PhoriaIslandSsrResult result) : IPhoriaIslandSsr
	{
		public Task<PhoriaIslandSsrResult> RenderIsland(PhoriaIsland island, CancellationToken cancellationToken = default) => Task.FromResult(result);
	}

	private sealed class HealthyServerMonitor : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Healthy,
			Url = "http://localhost"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopMonitoring() => Task.CompletedTask;
	}
}
