using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Logging;
using Phoria.Server;

namespace Phoria.Islands;

public interface IPhoriaIslandComponentFactory
{
	Task<PhoriaIslandHtmlContent> CreateAsync(
		string component,
		object? props,
		PhoriaIslandClientDirective? client);
}

public class PhoriaIslandComponentFactory(
	IPhoriaServerMonitor serverMonitor,
	IPhoriaIslandScopedContext scopedContext,
	IPhoriaIslandSsr phoriaIslandSsr,
	IOptions<PhoriaOptions> options,
	ILogger<PhoriaIslandComponentFactory> logger)
	: IPhoriaIslandComponentFactory, IDisposable
{
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IPhoriaIslandScopedContext scopedContext = scopedContext;
	private readonly IPhoriaIslandSsr phoriaIslandSsr = phoriaIslandSsr;
	private readonly PhoriaOptions options = options.Value;
	private readonly ILogger<PhoriaIslandComponentFactory> logger = logger;
	private readonly List<PhoriaIslandHtmlContent> contents = [];

	public async Task<PhoriaIslandHtmlContent> CreateAsync(
		string component,
		object? props,
		PhoriaIslandClientDirective? client)
	{
		PhoriaIslandRenderMode renderMode = PhoriaIslandRenderMode.Isomorphic;

		if (client == null)
		{
			renderMode = PhoriaIslandRenderMode.ServerOnly;
		}
		else if (client is PhoriaIslandClientOnlyDirective)
		{
			renderMode = PhoriaIslandRenderMode.ClientOnly;
		}

		if (serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			if (renderMode == PhoriaIslandRenderMode.Isomorphic)
			{
				logger.LogServerUnhealthyDegradingToClient(component);
				renderMode = PhoriaIslandRenderMode.ClientOnly;
			}
			else if (renderMode == PhoriaIslandRenderMode.ServerOnly)
			{
				throw new PhoriaIslandComponentException($"Cannot render component '{component}' on the server because the server is not healthy. Server status is '{serverMonitor.ServerStatus.Health}'.");
			}
		}

		var island = new PhoriaIsland
		{
			ComponentName = component,
			Props = props,
			RenderMode = renderMode,
			Client = client
		};

		scopedContext.AddIsland(island);

		PhoriaIslandSsrResult? ssrResult = null;
		try
		{
			ssrResult = island.RenderMode != PhoriaIslandRenderMode.ClientOnly
				? await phoriaIslandSsr.RenderIsland(island)
				: null;

			var content = new PhoriaIslandHtmlContent(
				island,
				ssrResult,
				options);

			contents.Add(content);
			return content;
		}
		catch
		{
			ssrResult?.Dispose();
			throw;
		}
	}

	public void Dispose()
	{
		foreach (PhoriaIslandHtmlContent content in contents)
		{
			content.Dispose();
		}

		contents.Clear();
		GC.SuppressFinalize(this);
	}
}

internal static partial class PhoriaIslandComponentFactoryLogMessages
{
	[LoggerMessage(
		EventId = EventFeature.Islands + 4,
		Message = "Phoria server is unhealthy; degrading isomorphic component {Component} to client-only rendering.",
		Level = LogLevel.Warning)]
	internal static partial void LogServerUnhealthyDegradingToClient(
		this ILogger logger,
		string component);
}
