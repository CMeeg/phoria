using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Phoria;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class PhoriaServerMiddlewareTests
{
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
