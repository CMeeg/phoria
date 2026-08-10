using Microsoft.Extensions.Options;

namespace Phoria.Server;

public interface IPhoriaServerHttpClientFactory
{
	HttpClient CreateClient();
}

internal interface IPhoriaServerHealthCheckHttpClientFactory
{
	HttpClient CreateClient();
}

internal sealed class PhoriaServerHttpClientFactory(
	IHttpClientFactory httpClientFactory,
	IOptions<PhoriaOptions> options)
	: IPhoriaServerHttpClientFactory, IPhoriaServerHealthCheckHttpClientFactory
{
	internal const string HttpClientName = "PhoriaServerHttpClient";
	internal const string HealthCheckHttpClientName = "PhoriaServerHealthCheckHttpClient";

	private readonly IHttpClientFactory httpClientFactory = httpClientFactory;
	private readonly PhoriaOptions options = options.Value;

	public HttpClient CreateClient()
	{
		HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
		httpClient.BaseAddress = new Uri(options.GetServerUrl());
		return httpClient;
	}

	public HttpClient CreateHealthCheckClient()
	{
		HttpClient httpClient = httpClientFactory.CreateClient(HealthCheckHttpClientName);
		httpClient.BaseAddress = new Uri(options.GetServerUrl());
		return httpClient;
	}
}
