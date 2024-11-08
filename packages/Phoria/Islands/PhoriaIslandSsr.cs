using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
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
		SerializedProps? props = component.Props == null
			? null
			: options.Islands.PropsSerializer.Serialize(component.Props);

		using HttpClient client = phoriaServerHttpClientFactory.CreateClient();

		var query = QueryString.Create(new Dictionary<string, string?>{
			{ "component", component.ComponentName }
		});

		StreamContent? content = CreatePropsContent(props);

		var response = await client.PostAsync(
			$"{RenderUrl}{query}",
			content,
			cancellationToken);

		string? framework = null;
		if (response.Headers.TryGetValues(
			"x-phoria-component-framework",
			out IEnumerable<string>? componentFrameworkHeader))
		{
			framework = componentFrameworkHeader.FirstOrDefault();
		}

		return new PhoriaIslandSsrResult
		{
			Framework = framework,
			CopyToStream = response.Content.CopyToAsync,
			Props = props
		};
	}

	private static StreamContent? CreatePropsContent(SerializedProps? props)
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

public record PhoriaIslandSsrResult
{
	public string? Framework { get; init; }
	public required Func<Stream, Task> CopyToStream { get; init; }
	public SerializedProps? Props { get; init; }
}
