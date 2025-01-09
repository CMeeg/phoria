using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Phoria.IO;
using Phoria.Server;

namespace Phoria.Islands;

public interface IPhoriaIslandSsr
{
	Task<PhoriaIslandSsrResult> RenderIsland(
		PhoriaIsland island,
		CancellationToken cancellationToken = default);
}

public class PhoriaIslandSsr(
	IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory,
	IOptions<PhoriaOptions> options)
	: IPhoriaIslandSsr
{
	private readonly IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory = phoriaServerHttpClientFactory;
	private readonly PhoriaOptions options = options.Value;

	public async Task<PhoriaIslandSsrResult> RenderIsland(
		PhoriaIsland island,
		CancellationToken cancellationToken = default)
	{
		StreamPool? propsStreamPool = null;
		if (island.Props != null)
		{
			propsStreamPool = new StreamPool();
			options.Islands.PropsSerializer.Serialize(island.Props, propsStreamPool);
		}

		using HttpClient client = phoriaServerHttpClientFactory.CreateClient();

		StreamContent? body = CreatePropsContent(propsStreamPool);

		HttpResponseMessage response = await client.PostAsync(
			$"{options.SsrBase}/render/{island.ComponentName}",
			body,
			cancellationToken);

		var contentStreamPool = new StreamPool();
		await response.Content.CopyToAsync(contentStreamPool.Stream, cancellationToken);

		if (response.Headers.TryGetValues(
			"x-phoria-island-framework",
			out IEnumerable<string>? componentFrameworkHeader))
		{
			island.Framework = componentFrameworkHeader.FirstOrDefault();
		}

		if (response.Headers.TryGetValues(
			"x-phoria-island-path",
			out IEnumerable<string>? componentPathHeader))
		{
			island.ComponentPath = componentPathHeader.FirstOrDefault();
		}

		return new PhoriaIslandSsrResult
		{
			Headers = response.Headers,
			Content = contentStreamPool,
			Props = propsStreamPool
		};
	}

	private static StreamContent? CreatePropsContent(StreamPool? props)
	{
		if (props == null || props.Stream == null || props.Stream.Length == 0)
		{
			return null;
		}

		props.Stream.Position = 0;

		var content = new StreamContent(props.Stream);
		content.Headers.Add("Content-Type", "application/json");

		return content;
	}
}

public record PhoriaIslandSsrResult
{
	public required HttpResponseHeaders Headers { get; init; }
	public required StreamPool Content { get; init; }
	public StreamPool? Props { get; init; }
}
