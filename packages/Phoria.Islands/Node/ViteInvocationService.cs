using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Phoria.Islands.Components;
using Phoria.Server;

namespace Phoria.Islands.Node;

public class ViteInvocationService : INodeInvocationService
{
	private readonly IHttpClientFactory clientFactory;
	private readonly IPhoriaServerMonitor serverMonitor;

	internal const string HttpClientName = "Phoria.Islands.Node.ViteHttpClient";

	public ViteInvocationService(
		IHttpClientFactory clientFactory,
		IPhoriaServerMonitor serverMonitor)
	{
		this.clientFactory = clientFactory;
		this.serverMonitor = serverMonitor;
	}

	public async Task<HttpResponseMessage> Invoke(string function, object[] args, CancellationToken cancellationToken = default)
	{
		using HttpClient client = CreateClient();

		string? containerId = args[0]?.ToString();
		var options = args[1] as RenderOptions;
		var props = args[2] as PropsSerialized;

		string path = string.Empty;

		QueryString query = QueryString.Create(new Dictionary<string, string?>{
				{ "containerId", containerId },
				{ "component", options?.ComponentName },
				{ "serverOnly", options?.ServerOnly.ToString() },
				{ "nonce", options?.Nonce }
			});

		StreamContent? content = CreatePropsContent(props);

		return await client.PostAsync(query.Value, content, cancellationToken);
	}

	// Creates a new instance of the HttpClient to connect to the Vite Dev Server.
	private HttpClient CreateClient()
	{
		var client = clientFactory.CreateClient(HttpClientName);
		client.BaseAddress = new Uri(serverMonitor.ServerStatus.Url);
		return client;
	}

	private StreamContent? CreatePropsContent(PropsSerialized? props)
	{
		if (props == null)
		{
			return null;
		}

		if (props.Stream == null || props.Stream.Length == 0)
		{
			return null;
		}

		props.Stream.Position = 0;

		var content = new StreamContent(props.Stream);
		content.Headers.Add("Content-Type", "application/json");

		return content;
	}
}
