using Microsoft.Extensions.Options;

namespace Phoria.Server;

// TODO: Is this the best way to do this?

public interface IPhoriaServerHttpClientFactory
{
	HttpClient CreateClient();
}

internal sealed class PhoriaServerHttpClientFactory
	: IPhoriaServerHttpClientFactory
{
	internal const string HttpClientName = "PhoriaServerHttpClient";

	private readonly IHttpClientFactory httpClientFactory;
	private readonly PhoriaOptions options;

	public PhoriaServerHttpClientFactory(
		IHttpClientFactory httpClientFactory,
		IOptions<PhoriaOptions> options)
	{
		this.httpClientFactory = httpClientFactory;
		this.options = options.Value;
	}

	public HttpClient CreateClient()
	{
		HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
		httpClient.BaseAddress = new Uri(options.GetServerUrl());
		return httpClient;
	}
}
