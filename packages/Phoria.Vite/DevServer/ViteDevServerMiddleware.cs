// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Vite.Logging;

namespace Phoria.Vite.DevServer;

/// <summary>
/// Represents a middleware that proxies requests to the Vite Development Server.
/// </summary>
/// <param name="loggerFactory">The logging factory.</param>
/// <param name="clientFactory">An <see cref="IHttpClientFactory"/> instance used to create <see cref="HttpClient"/> instances.</param>
internal sealed class ViteDevServerMiddleware(
	ILoggerFactory loggerFactory,
	IHttpClientFactory clientFactory,
	IViteDevServerStatus viteDevServerStatus
) : IMiddleware
{
	internal const string HttpClientName = "Phoria.Vite.DevServer.DevHttpClient";

	private readonly ILoggerFactory loggerFactory = loggerFactory;
	private readonly ILogger<ViteDevServerMiddleware> logger =
		loggerFactory.CreateLogger<ViteDevServerMiddleware>();
	private readonly IHttpClientFactory clientFactory = clientFactory;
	private readonly IViteDevServerStatus viteDevServerStatus = viteDevServerStatus;

	/// <inheritdoc />
	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		// If the request doesn't have an endpoint, the request path is not null and the request method is GET, proxy the request to the Vite Development Server.
		if (
			context.GetEndpoint() == null
			&& context.Request.Path.HasValue
			&& context.Request.Method == HttpMethod.Get.Method
		)
		{
			WebSocketManager ws = context.WebSockets;
			bool isHmrRequest =
				ws.IsWebSocketRequest
				&& ws.WebSocketRequestedProtocols.Contains(ViteDevHmrProxy.SubProtocol);
			// If it's an HMR (hot module reload) request, delegate processing to a WebSocket proxy, otherwise, process the request via HTTP.
			Task proxyRequest = isHmrRequest
				? ProxyViaHmrAsync(context)
				: ProxyViaHttpAsync(context, next);
			await proxyRequest;
		}
		// If the request path is null, call the next middleware.
		else
		{
			await next(context);
		}
	}

	// Proxies the HMR request to the Vite Dev Server via WebSocket.
	private async Task ProxyViaHmrAsync(HttpContext context)
	{
		string serverUrlWithBasePath = viteDevServerStatus.ServerUrlWithBasePath;
		var wsUriBuilder = new UriBuilder(serverUrlWithBasePath);
		wsUriBuilder.Scheme = wsUriBuilder.Scheme.ToLowerInvariant() switch
		{
			"http" => "ws",
			"https" => "wss",
			_ => throw new ArgumentException(nameof(wsUriBuilder.Scheme))
		};

		ILogger<ViteDevHmrProxy> logger = loggerFactory.CreateLogger<ViteDevHmrProxy>();
		await new ViteDevHmrProxy(logger).ProxyAsync(
			context,
			wsUriBuilder.Uri,
			CancellationToken.None
		);
	}

	// Proxies the request to the Vite Dev Server via HTTP.
	private async Task ProxyViaHttpAsync(HttpContext context, RequestDelegate next)
	{
		using HttpClient client = CreateClient(context.Request.Headers);
		// Get the request path
		string path = context.Request.GetEncodedPathAndQuery();

		try
		{
			// Get the requested path from the Vite Dev Server.
			HttpResponseMessage response = await client.GetAsync(path);
			// If the response is successful, process.
			if (response.IsSuccessStatusCode)
			{
				// Get the response content.
				byte[] content = await response.Content.ReadAsByteArrayAsync();
				// Get the response content type.
				string? contentType = response.Content.Headers.ContentType?.MediaType;
				// Set the response content type.
				context.Response.ContentType = contentType ?? "application/octet-stream";
				// Set the response content length.
				context.Response.ContentLength = content.Length;
				// Write the response content.
				await context.Response.Body.WriteAsync(content);
			}
			// Otherwise, call the next middleware.
			else
			{
				await next(context);
			}
		}
		catch (HttpRequestException exp)
		{
			// Log the exception.
			logger.LogMiddlewareProxyViaHttpError(exp.Message);
			await next(context);
		}
	}

	// Creates a new instance of the HttpClient to connect to the Vite Dev Server.
	private HttpClient CreateClient(IHeaderDictionary requestHeaders)
	{
		HttpClient client = clientFactory.CreateClient(HttpClientName);
		string serverUrl = viteDevServerStatus.ServerUrl;
		client.BaseAddress = new Uri(serverUrl);
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.1));

		// Pass the "Accept" header from the original request if it exists.
		if (requestHeaders.ContainsKey("Accept"))
		{
			client.DefaultRequestHeaders.Add("Accept", requestHeaders.Accept.ToList());
		}

		return client;
	}
}
