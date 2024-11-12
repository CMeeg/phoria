using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Phoria.IO;
using Phoria.Server;

namespace Phoria.Islands;

public interface IPhoriaIslandSsr
{
	Task<PhoriaIslandSsrResult> RenderComponent(
		PhoriaIslandComponent component,
		CancellationToken cancellationToken = default);
}

public class PhoriaIslandSsr
	: IPhoriaIslandSsr
{
	internal const string RenderUrl = "/render";

	private readonly IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory;
	private readonly PhoriaOptions options;

	public PhoriaIslandSsr(
		IPhoriaServerHttpClientFactory phoriaServerHttpClientFactory,
		IOptions<PhoriaOptions> options)
	{
		this.phoriaServerHttpClientFactory = phoriaServerHttpClientFactory;
		this.options = options.Value;
	}

	public async Task<PhoriaIslandSsrResult> RenderComponent(
		PhoriaIslandComponent component,
		CancellationToken cancellationToken = default)
	{
		StreamPool? propsStreamPool = null;
		if (component.Props != null)
		{
			propsStreamPool = new StreamPool();
			options.Islands.PropsSerializer.Serialize(component.Props, propsStreamPool);
		}

		using HttpClient client = phoriaServerHttpClientFactory.CreateClient();

		StreamContent? body = CreatePropsContent(propsStreamPool);

		var response = await client.PostAsync(
			$"{RenderUrl}/{component.ComponentName}",
			body,
			cancellationToken);

		var contentStreamPool = new StreamPool();
		await response.Content.CopyToAsync(contentStreamPool.Stream, cancellationToken);

		if (response.Headers.TryGetValues(
			"x-phoria-island-framework",
			out IEnumerable<string>? componentFrameworkHeader))
		{
			component.Framework = componentFrameworkHeader.FirstOrDefault();
		}

		if (response.Headers.TryGetValues(
			"x-phoria-island-path",
			out IEnumerable<string>? componentPathHeader))
		{
			component.ComponentPath = componentPathHeader.FirstOrDefault();
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
