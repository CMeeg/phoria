using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Phoria;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Xunit;

namespace Phoria.Tests.Server;

public class PhoriaServerMiddlewareTests
{
	[Fact]
	public async Task InvokeAsync_HmrRequest_UsesHmrProxyAndSkipsNext()
	{
		var proxy = new RecordingHmrProxy();
		bool nextCalled = false;
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; },
			proxy);

		try
		{
			DefaultHttpContext context = CreateGetContext("/@vite/client");
			context.Request.Headers.Upgrade = "websocket";
			context.Request.Headers["Sec-WebSocket-Protocol"] = "vite-hmr";
			context.Features.Set<IHttpWebSocketFeature>(new StubWebSocketFeature(true, ["vite-hmr"]));
			context.RequestServices = services;

			await pipeline(context);

			Assert.True(proxy.Called);
			Assert.False(nextCalled);
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}

	[Theory]
	[InlineData(false, "vite-hmr")]
	[InlineData(true, "other")]
	public async Task InvokeAsync_NonHmrWebSocket_FallsThrough(bool isWebSocket, string protocol)
	{
		var proxy = new RecordingHmrProxy();
		bool nextCalled = false;
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; },
			proxy);

		try
		{
			DefaultHttpContext context = CreateGetContext("/@vite/client");
			context.Features.Set<IHttpWebSocketFeature>(new StubWebSocketFeature(isWebSocket, [protocol]));
			context.RequestServices = services;

			await pipeline(context);

			Assert.True(nextCalled);
			Assert.False(proxy.Called);
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}
	[Fact]
	public async Task InvokeAsync_FailPolicyUnhealthy_Returns503ForUnclaimedGet()
	{
		bool nextCalled = false;
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new StubHttpClientFactory(),
			Options.Create(FailOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		try
		{
			DefaultHttpContext context = CreateGetContext("/unknown");
			context.RequestServices = services;

			await pipeline(context);

			Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
			Assert.False(nextCalled);
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}

	[Fact]
	public async Task InvokeAsync_DegradePolicyUnhealthy_FallsThroughForUnclaimedGet()
	{
		bool nextCalled = false;
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new StubHttpClientFactory(),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		try
		{
			DefaultHttpContext context = CreateGetContext("/unknown");
			context.RequestServices = services;

			await pipeline(context);

			Assert.True(nextCalled);
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}

	[Fact]
	public async Task InvokeAsync_FailPolicyHealthy_ProxiesRequestAndDoesNotFallThrough()
	{
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("app-body")
			}),
			Options.Create(FailOptions()),
			_ => throw new InvalidOperationException("next must not be called on the healthy proxy path."));

		try
		{
			DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");
			context.RequestServices = services;
			context.Response.Body = new MemoryStream();

			await pipeline(context);

			Assert.Equal("app-body", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}

	[Fact]
	public async Task InvokeAsync_FailPolicyMidProxyException_Returns503()
	{
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(_ => throw new HttpRequestException("Connection refused (localhost)")),
			Options.Create(FailOptions()),
			_ => throw new InvalidOperationException("next must not be called in Fail mode."));

		try
		{
			DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");
			context.RequestServices = services;

			await pipeline(context);

			Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}

	[Fact]
	public async Task InvokeAsync_DegradePolicyMidProxyException_FallsThrough()
	{
		bool nextCalled = false;
		var (pipeline, services) = CreatePipeline(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(_ => throw new HttpRequestException("Connection refused (localhost)")),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		try
		{
			DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");
			context.RequestServices = services;

			await pipeline(context);

			Assert.True(nextCalled);
		}
		finally
		{
			(services as IDisposable)?.Dispose();
		}
	}

	private static PhoriaOptions FailOptions() => new()
	{
		Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
	};

	private static (RequestDelegate Pipeline, IServiceProvider Services) CreatePipeline(
		IPhoriaServerMonitor serverMonitor,
		IPhoriaServerHttpClientFactory httpClientFactory,
		IOptions<PhoriaOptions> options,
		RequestDelegate? next = null,
		IViteDevServerHmrProxy? hmrProxy = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton(serverMonitor);
		services.AddSingleton(httpClientFactory);
		services.AddSingleton(options);
		services.AddSingleton(hmrProxy ?? new StubHmrProxy());

		ServiceProvider serviceProvider = services.BuildServiceProvider();
		var application = new ApplicationBuilder(serviceProvider);
		application.UsePhoria();
		application.Run(next ?? (_ => Task.CompletedTask));

		return (application.Build(), serviceProvider);
	}

	private static DefaultHttpContext CreateGetContext(string path)
	{
		var context = new DefaultHttpContext();
		context.Request.Method = "GET";
		context.Request.Path = path;
		return context;
	}

	private sealed class RecordingHmrProxy : IViteDevServerHmrProxy
	{
		public bool Called { get; private set; }

		public Task ProxyAsync(HttpContext context, CancellationToken cancellationToken)
		{
			Called = true;
			return Task.CompletedTask;
		}
	}

	private sealed class StubWebSocketFeature(bool isWebSocket, IList<string> protocols) : IHttpWebSocketFeature
	{
		public bool IsWebSocketRequest { get; } = isWebSocket;
		public IList<string> WebSocketRequestedProtocols { get; } = protocols;

		public Task<WebSocket> AcceptAsync(WebSocketAcceptContext context) =>
			throw new NotSupportedException();
	}
}
