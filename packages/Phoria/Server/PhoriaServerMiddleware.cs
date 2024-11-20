// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Phoria.Logging;

namespace Phoria.Server;

internal sealed class PhoriaServerMiddleware
{
	private readonly ILogger<PhoriaServerMiddleware> logger;
	private readonly IPhoriaServerMonitor serverMonitor;
	private readonly IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory;
	private readonly RequestDelegate next;

	public PhoriaServerMiddleware(
		ILogger<PhoriaServerMiddleware> logger,
		IPhoriaServerMonitor serverMonitor,
		IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory,
		RequestDelegate next)
	{
		this.logger = logger;
		this.serverMonitor = serverMonitor;
		this.phoriaServerHttpClientFactory = phoriaServerHttpClientFactory;
		this.next = next;
	}

	/// <inheritdoc />
	public async Task InvokeAsync(
		HttpContext context,
		IViteDevServerHmrProxy viteDevServerHmrProxy)
	{
		// If the request doesn't have an endpoint, the request path is not null and the request method is GET, and the server is healthy, proxy the request to the server

		if (context.GetEndpoint() == null
			&& context.Request.Path.HasValue
			&& context.Request.Method == HttpMethod.Get.Method
			&& serverMonitor.ServerStatus.Health == PhoriaServerHealth.Healthy)
		{
			// If it's an HMR (hot module reload) request, delegate processing to a WebSocket proxy, otherwise, process the request via HTTP

			Task proxyRequest = ViteDevServerHmrProxy.IsHmrRequest(context)
				? viteDevServerHmrProxy.ProxyAsync(context, CancellationToken.None)
				: ProxyViaHttpAsync(context, next);

			await proxyRequest;
		}
		else
		{
			// If the request path is null, call the next middleware

			await next(context);
		}
	}

	private async Task ProxyViaHttpAsync(HttpContext context, RequestDelegate next)
	{
		using HttpClient httpClient = CreateHttpClient(context.Request.Headers);

		string requestUrl = context.Request.GetEncodedPathAndQuery();

		try
		{
			// Get the requested path from the Phoria Server

			HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

			if (response.IsSuccessStatusCode)
			{
				// If the request was successful, proxy the response

				byte[] content = await response.Content.ReadAsByteArrayAsync();
				string? contentType = response.Content.Headers.ContentType?.MediaType;

				context.Response.ContentType = contentType ?? "application/octet-stream";
				context.Response.ContentLength = content.Length;

				await context.Response.Body.WriteAsync(content);
			}
			else
			{
				// Otherwise, call the next middleware

				await next(context);
			}
		}
		catch (HttpRequestException ex)
		{
			logger.LogMiddlewareProxyViaHttpError(requestUrl, ex);

			await next(context);
		}
	}

	private HttpClient CreateHttpClient(IHeaderDictionary requestHeaders)
	{
		HttpClient client = phoriaServerHttpClientFactory.CreateClient();

		// Pass the "Accept" header from the original request if it exists
		if (requestHeaders.ContainsKey("Accept"))
		{
			client.DefaultRequestHeaders.Add("Accept", requestHeaders.Accept.ToList());
		}

		return client;
	}
}

internal static partial class PhoriaServerMiddlewareLogMessages
{
	private static readonly Action<ILogger, string, Exception?> logMiddlewareProxyViaHttpError = LoggerMessage.Define<string>(
		LogLevel.Error,
		EventFeature.Server + 1,
		"Request to {Url} failed. Please make sure the Phoria server is running.");
	internal static void LogMiddlewareProxyViaHttpError(
		this ILogger logger,
		string url,
		Exception? exception = null)
	{
		logMiddlewareProxyViaHttpError(logger, url, exception);
	}
}
