using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class PhoriaServerMiddlewareTests
{
	[Fact]
	public async Task InvokeAsync_FailPolicyUnhealthy_Returns503ForUnclaimedGet()
	{
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new StubHttpClientFactory(),
			Options.Create(FailOptions()));

		DefaultHttpContext context = CreateGetContext("/unknown");
		bool nextCalled = false;

		await middleware.InvokeAsync(context, new StubHmrProxy());
		_ = nextCalled;

		Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
	}

	[Fact]
	public async Task InvokeAsync_DegradePolicyUnhealthy_FallsThroughForUnclaimedGet()
	{
		bool nextCalled = false;
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			new StubHttpClientFactory(),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		DefaultHttpContext context = CreateGetContext("/unknown");

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.True(nextCalled);
	}

	[Fact]
	public async Task InvokeAsync_FailPolicyHealthy_ProxiesRequestAndDoesNotFallThrough()
	{
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("app-body")
			}),
			Options.Create(FailOptions()),
			_ => throw new InvalidOperationException("next must not be called on the healthy proxy path."));

		DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");
		context.Response.Body = new MemoryStream();

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.Equal("app-body", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
	}

	[Fact]
	public async Task InvokeAsync_FailPolicyMidProxyException_Returns503()
	{
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(_ => throw new HttpRequestException("Connection refused (localhost)")),
			Options.Create(FailOptions()),
			_ => throw new InvalidOperationException("next must not be called in Fail mode."));

		DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
	}

	[Fact]
	public async Task InvokeAsync_DegradePolicyMidProxyException_FallsThrough()
	{
		bool nextCalled = false;
		PhoriaServerMiddleware middleware = CreateMiddleware(
			new StubServerMonitor(PhoriaServerHealth.Healthy),
			new StubHttpClientFactory(_ => throw new HttpRequestException("Connection refused (localhost)")),
			Options.Create(new PhoriaOptions()),
			_ => { nextCalled = true; return Task.CompletedTask; });

		DefaultHttpContext context = CreateGetContext("/ui/assets/app.js");

		await middleware.InvokeAsync(context, new StubHmrProxy());

		Assert.True(nextCalled);
	}

	private static PhoriaOptions FailOptions() => new()
	{
		Server = new PhoriaServerOptions { UnavailableBehavior = PhoriaServerUnavailableBehavior.Fail }
	};

	private static PhoriaServerMiddleware CreateMiddleware(
		IPhoriaServerMonitor serverMonitor,
		IPhoriaServerHttpClientFactory httpClientFactory,
		IOptions<PhoriaOptions> options,
		RequestDelegate? next = null) =>
		new(
			NullLogger<PhoriaServerMiddleware>.Instance,
			serverMonitor,
			httpClientFactory,
			options,
			next ?? (_ => Task.CompletedTask));

	private static DefaultHttpContext CreateGetContext(string path)
	{
		var context = new DefaultHttpContext();
		context.Request.Method = "GET";
		context.Request.Path = path;
		return context;
	}

	private sealed class StubServerMonitor(PhoriaServerHealth health) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = health,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}

	private sealed class StubHttpClientFactory : IPhoriaServerHttpClientFactory
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> send;

		public StubHttpClientFactory()
			: this(_ => new HttpResponseMessage(HttpStatusCode.NotFound))
		{
		}

		public StubHttpClientFactory(HttpResponseMessage response)
			: this(_ => response)
		{
		}

		public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> send)
		{
			this.send = send;
		}

		public HttpClient CreateClient() => new(new StubHttpMessageHandler(send))
		{
			BaseAddress = new Uri("http://localhost:5173")
		};

		private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
		{
			protected override Task<HttpResponseMessage> SendAsync(
				HttpRequestMessage request,
				CancellationToken cancellationToken) =>
				Task.FromResult(send(request));
		}
	}

	private sealed class StubHmrProxy : IViteDevServerHmrProxy
	{
		public Task ProxyAsync(HttpContext context, CancellationToken cancellationToken) =>
			throw new InvalidOperationException("The HMR proxy should not be reached in these tests.");
	}
}
