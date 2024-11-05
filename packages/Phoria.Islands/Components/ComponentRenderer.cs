using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.IO;
using Phoria.Islands.Node;

namespace Phoria.Islands.Components;

public class ComponentRenderer : IDisposable
{
	// TODO: This class needs some serious tidying up!

	protected readonly INodeInvocationService nodeInvocationService;
	protected readonly PhoriaIslandsOptions configuration;
	private readonly IReactIdGenerator reactIdGenerator;

	internal PropsSerialized? SerializedProps { get; set; }

	public Action<Exception, string, string> ExceptionHandler { get; set; }

	private protected PooledStream? OutputHtml { get; set; }

	public ComponentRenderer(
		PhoriaIslandsOptions configuration,
		IReactIdGenerator reactIdGenerator,
		INodeInvocationService nodeInvocationService)
	{
		this.configuration = configuration;
		this.reactIdGenerator = reactIdGenerator;
		this.nodeInvocationService = nodeInvocationService;

		ExceptionHandler = this.configuration.ExceptionHandler;
	}

	public async Task RenderHtml(PhoriaIslandComponent component)
	{
		if (component.ClientOnly)
		{
			return;
		}

		try
		{
			var routingContext = await Render(component, new RenderOptions
			{
				DisableStreaming = true,
				DisableBootstrapPropsInPlace = true,
				BootstrapScriptContent = null,
				ComponentName = component.ComponentName,
				ServerOnly = component.ServerOnly,
				Nonce = null //component.NonceProvider?.Invoke()
			});

			OutputHtml = new PooledStream();
			await routingContext.CopyToStream(OutputHtml.Stream);
		}
		catch (Exception ex)
		{
			ExceptionHandler(ex, component.ComponentName, string.Empty); //, component.ContainerId);
		}
	}

	private async Task<RoutingContext> Render(PhoriaIslandComponent component, RenderOptions options)
	{
		SerializedProps ??= component.Props == null ? null : configuration.PropsSerializer.Serialize(component.Props);

		// TODO: `RenderOptions` is React specific so need to refactor
		HttpResponseMessage httpResponseMessage = await nodeInvocationService.Invoke(
			"renderComponent",
			// TODO: Is null ok here for props?
			[string.Empty, options, SerializedProps]);
			// [ContainerId, options, SerializedProps]);

		// TODO: Not sure about this header or if it's needed
		string? url = null;
		if (httpResponseMessage.Headers.TryGetValues("RspUrl", out IEnumerable<string>? urlHeader))
		{
			url = urlHeader.FirstOrDefault();
		}

		// TODO: Not sure about this header or if it's needed
		int? code = null;
		if (httpResponseMessage.Headers.TryGetValues("RspCode", out IEnumerable<string>? codeHeader) &&
			int.TryParse(codeHeader.FirstOrDefault(), out var codeValue))
		{
			code = codeValue;
		}

		if (httpResponseMessage.Headers.TryGetValues("x-phoria-component-framework", out IEnumerable<string>? componentFrameworkHeader))
		{
			component.ComponentFramework = componentFrameworkHeader.FirstOrDefault();
		}

		return new RoutingContext(
			url ?? string.Empty,
			code,
			httpResponseMessage.Content.CopyToAsync);
	}

	// private void WriterSerialziedProps(TextWriter writer)
	// {
	// 	SerializedProps ??= configuration.PropsSerializer.Serialize(Props);
	// 	WriteUtf8Stream(writer, SerializedProps.Stream);
	// }

	// public void WriteIslandPropsTo(TextWriter writer)
	// {
	// 	if (Props == null)
	// 	{
	// 		return;
	// 	}

	// 	WriterSerialziedProps(writer);
	// }

	// public void WriteOutputHtmlTo(TextWriter writer)
	// {
	// 	// TODO: Need to wrap output in `phoria-island` web component

	// 	if (ServerOnly)
	// 	{
	// 		WriteUtf8Stream(writer, OutputHtml.Stream);
	// 		return;
	// 	}

	// 	writer.Write('<');
	// 	writer.Write(ContainerTag);
	// 	writer.Write(" id=\"");
	// 	writer.Write(ContainerId);
	// 	writer.Write('"');
	// 	if (!string.IsNullOrEmpty(ContainerClass))
	// 	{
	// 		writer.Write(" class=\"");
	// 		writer.Write(ContainerClass);
	// 		writer.Write('"');
	// 	}

	// 	writer.Write('>');

	// 	if (!ClientOnly)
	// 	{
	// 		WriteUtf8Stream(writer, OutputHtml?.Stream);
	// 	}

	// 	writer.Write("</");
	// 	writer.Write(ContainerTag);
	// 	writer.Write('>');

	// 	// if (BootstrapInPlace)
	// 	// {
	// 	// 	writer.Write("<script");
	// 	// 	if (NonceProvider != null)
	// 	// 	{
	// 	// 		writer.Write(" nonce=\"");
	// 	// 		writer.Write(NonceProvider());
	// 	// 		writer.Write("\"");
	// 	// 	}

	// 	// 	writer.Write(">");
	// 	// 	writer.Write("(window.__nrp = window.__nrp || {})['");
	// 	// 	writer.Write(ContainerId);
	// 	// 	writer.Write("'] = ");
	// 	// 	WriterSerialziedProps(writer);
	// 	// 	writer.Write(';');

	// 	// 	if (BootstrapScriptContentProvider != null)
	// 	// 	{
	// 	// 		writer.Write(BootstrapScriptContentProvider(ContainerId));
	// 	// 	}

	// 	// 	writer.Write("</script>");
	// 	// }
	// }

	public void WriteIslandOutputHtmlTo(PhoriaIslandComponent component, TextWriter writer)
	{
		// TODO: Need to wrap output in `phoria-island` web component

		if (component.ServerOnly)
		{
			WriteUtf8Stream(writer, OutputHtml.Stream);
			return;
		}

		writer.Write('<');
		writer.Write("phoria-island");
		// writer.Write(" id=\"");
		// writer.Write(ContainerId);
		// writer.Write('"');
		// if (!string.IsNullOrEmpty(ContainerClass))
		// {
		// 	writer.Write(" class=\"");
		// 	writer.Write(ContainerClass);
		// 	writer.Write('"');
		// }

		writer.Write(" component=\"");
		writer.Write(component.ComponentName);
		writer.Write('"');

		// TODO: Support other directives - see Astro for inspiration
		if (component.ClientOnly)
		{
			writer.Write(" client:only");
		}
		else
		{
			writer.Write(" client:load");
		}

		// if (!string.IsNullOrEmpty(ComponentFramework))
		// {
		// 	writer.Write(" data-phoria-component-framework=\"");
		// 	writer.Write(ComponentFramework);
		// 	writer.Write('"');
		// }

		// TODO: Pass props to the component - would prob be easier to set as an attribute, but would it be "better"?
		if (component.Props != null)
		{
		    writer.Write(" props='");
		    // TODO: Will need to HTML encode this
			SerializedProps ??= configuration.PropsSerializer.Serialize(component.Props);
			WriteUtf8Stream(writer, SerializedProps.Stream);
		    writer.Write("'");
		}

		writer.Write('>');

		if (!component.ClientOnly)
		{
			WriteUtf8Stream(writer, OutputHtml?.Stream);
		}

		writer.Write("</");
		writer.Write("phoria-island");
		writer.Write('>');

		// if (BootstrapInPlace)
		// {
		//     writer.Write("<script");
		//     if (NonceProvider != null)
		//     {
		//         writer.Write(" nonce=\"");
		//         writer.Write(NonceProvider());
		//         writer.Write("\"");
		//     }

		//     writer.Write(">");
		//     writer.Write("(window.__nrp = window.__nrp || {})['");
		//     writer.Write(ContainerId);
		//     writer.Write("'] = ");
		//     WriterSerialziedProps(writer);
		//     writer.Write(';');

		//     if (BootstrapScriptContentProvider != null)
		//     {
		//         writer.Write(BootstrapScriptContentProvider(ContainerId));
		//     }

		//     writer.Write("</script>");
		// }
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteUtf8Stream(TextWriter writer, RecyclableMemoryStream stream)
	{
		if (stream?.Length == 0)
		{
			return;
		}

		stream.Position = 0;
		var textWriterBufferWriter = new TextWriterBufferWriter(writer);

		Encoding.UTF8.GetDecoder().Convert(
			stream.GetReadOnlySequence(),
			textWriterBufferWriter,
			true,
			out _,
			out _);
	}

	public virtual void Dispose()
	{
		OutputHtml?.Dispose();
		SerializedProps?.Dispose();
	}
}
