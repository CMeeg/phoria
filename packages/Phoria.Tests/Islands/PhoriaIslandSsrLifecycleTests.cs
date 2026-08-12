using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.Diagnostics;
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
	public async Task RenderIsland_CreatesPhoriaSsrActivity()
	{
		Activity? capturedActivity = null;
		var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == PhoriaActivitySource.Name,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			ActivityStopped = activity => capturedActivity = activity
		};
		ActivitySource.AddActivityListener(listener);

		var ssr = new PhoriaIslandSsr(
			new StubHttpClientFactory(_ =>
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent("<span>Example</span>")
				};
				response.Headers.Add("x-phoria-island-framework", "react");
				return response;
			}),
			Options.Create(new PhoriaOptions()));

		try
		{
			using PhoriaIslandSsrResult result = await ssr.RenderIsland(new PhoriaIsland
			{
				ComponentName = "Example"
			}, TestContext.Current.CancellationToken);

			Assert.NotNull(capturedActivity);
			Assert.Equal(PhoriaActivitySource.Name, capturedActivity!.Source.Name);
			Assert.Equal("phoria.ssr.render", capturedActivity.OperationName);
			Assert.Equal(ActivityKind.Client, capturedActivity.Kind);
			Assert.Equal("Example", capturedActivity.GetTagItem("phoria.component"));
			Assert.Equal("react", capturedActivity.GetTagItem("phoria.framework"));
		}
		finally
		{
			listener.Dispose();
		}
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
			Options.Create(new PhoriaOptions()),
			NullLogger<PhoriaIslandComponentFactory>.Instance);

		_ = await factory.CreateAsync("Example", null, null);
		factory.Dispose();

		AssertDisposed(contentPool);
	}

	[Fact]
	public async Task ComponentFactory_DegradesIsomorphicIslandWhenServerIsUnhealthy()
	{
		var ssr = new TrackingSsr();
		var factory = new PhoriaIslandComponentFactory(
			new UnhealthyServerMonitor(),
			new PhoriaIslandScopedContext(),
			ssr,
			Options.Create(new PhoriaOptions()),
			NullLogger<PhoriaIslandComponentFactory>.Instance);

		PhoriaIslandHtmlContent content = await factory.CreateAsync("Example", null, new PhoriaIslandClientLoadDirective());

		Assert.Equal(0, ssr.CallCount);
		Assert.NotNull(content);
		factory.Dispose();
	}

	[Fact]
	public async Task ComponentFactory_StillThrowsForServerOnlyIslandWhenServerIsUnhealthy()
	{
		var factory = new PhoriaIslandComponentFactory(
			new UnhealthyServerMonitor(),
			new PhoriaIslandScopedContext(),
			new TrackingSsr(),
			Options.Create(new PhoriaOptions()),
			NullLogger<PhoriaIslandComponentFactory>.Instance);

		await Assert.ThrowsAsync<PhoriaIslandComponentException>(() => factory.CreateAsync("Example", null, null));
	}

	[Fact]
	public async Task ComponentFactory_FailPolicy_ThrowsForIsomorphicIslandWhenServerIsUnhealthy()
	{
		var factory = new PhoriaIslandComponentFactory(
			new UnhealthyServerMonitor(),
			new PhoriaIslandScopedContext(),
			new TrackingSsr(),
			Options.Create(new PhoriaOptions
			{
				Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
			}),
			NullLogger<PhoriaIslandComponentFactory>.Instance);

		await Assert.ThrowsAsync<PhoriaIslandComponentException>(
			() => factory.CreateAsync("Example", null, new PhoriaIslandClientLoadDirective()));
	}

	[Fact]
	public async Task ComponentFactory_DegradePolicy_LogsWarningBeforeThrowingForServerOnlyIsland()
	{
		var logger = new ListLogger<PhoriaIslandComponentFactory>();
		var factory = new PhoriaIslandComponentFactory(
			new UnhealthyServerMonitor(),
			new PhoriaIslandScopedContext(),
			new TrackingSsr(),
			Options.Create(new PhoriaOptions()),
			logger);

		await Assert.ThrowsAsync<PhoriaIslandComponentException>(
			() => factory.CreateAsync("Example", null, null));

		Assert.Contains(logger.Messages, message => message.Contains("suppressing", StringComparison.Ordinal));
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

	private sealed class TrackingSsr : IPhoriaIslandSsr
	{
		public int CallCount { get; private set; }

		public Task<PhoriaIslandSsrResult> RenderIsland(PhoriaIsland island, CancellationToken cancellationToken = default)
		{
			CallCount++;
			return Task.FromResult(new PhoriaIslandSsrResult
			{
				Headers = new HttpResponseMessage().Headers,
				Content = new StreamPool()
			});
		}
	}

	private sealed class ListLogger<T> : ILogger<T>
	{
		private readonly List<string> messages = [];

		public IReadOnlyList<string> Messages => messages;

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			messages.Add(formatter(state, exception));
		}
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

	private sealed class UnhealthyServerMonitor : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Unhealthy,
			Url = "http://localhost"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;

		public Task StopMonitoring() => Task.CompletedTask;
	}
}
