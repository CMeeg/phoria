using System.Net;
using Microsoft.AspNetCore.Http;
using Phoria.Server;

namespace Phoria.Tests.TestUtilities;

internal sealed class StubHttpClientFactory : IPhoriaServerHttpClientFactory
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

	public StubHttpClientFactory(HttpStatusCode statusCode)
		: this(_ => CreateResponse(statusCode))
	{
	}

	public StubHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> send)
	{
		this.send = send;
	}

	public HttpClient CreateClient() => new(new StubHttpMessageHandler(send))
	{
		BaseAddress = new Uri("http://localhost")
	};

	private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode)
	{
		var response = new HttpResponseMessage(statusCode);
		if (statusCode == HttpStatusCode.OK)
		{
			response.Content = new StringContent("{\"mode\":\"development\",\"frameworks\":[]}");
		}

		return response;
	}

	private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken) =>
			Task.FromResult(send(request));
	}
}

internal sealed class ScriptedHttpClientFactory(Func<int, HttpResponseMessage> responseFor)
	: IPhoriaServerHttpClientFactory
{
	private readonly Func<int, HttpResponseMessage> responseFor = responseFor;
	private int requests;

	public int RequestCount => Volatile.Read(ref requests);

	public HttpClient CreateClient() => new(new ScriptedHttpMessageHandler(this))
	{
		BaseAddress = new Uri("http://localhost")
	};

	private sealed class ScriptedHttpMessageHandler(ScriptedHttpClientFactory factory) : HttpMessageHandler
	{
		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(factory.responseFor(Interlocked.Increment(ref factory.requests)));
		}
	}
}

internal sealed class StubHmrProxy : IViteDevServerHmrProxy
{
	public Task ProxyAsync(HttpContext context, CancellationToken cancellationToken) =>
		throw new InvalidOperationException("The HMR proxy should not be reached in these tests.");
}
