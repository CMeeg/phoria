using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.IO;
using Phoria.IO;

namespace Phoria.Islands;

public class PhoriaIslandHtmlContent
	: IHtmlContent
{
	private const string TagName = "phoria-island";

	private readonly PhoriaIslandComponent component;
	private readonly PhoriaIslandSsrResult? ssrResult;
	private readonly PhoriaOptions options;

	public PhoriaIslandHtmlContent(
		PhoriaIslandComponent component,
		PhoriaIslandSsrResult? ssrResult,
		PhoriaOptions options)
	{
		this.component = component;
		this.ssrResult = ssrResult;
		this.options = options;
	}

	public void WriteTo(TextWriter writer, HtmlEncoder encoder)
	{
		if (component.RenderMode == PhoriaIslandRenderMode.ServerOnly)
		{
			if (ssrResult != null)
			{
				// Render the SSR result

				WriteUtf8Stream(writer, ssrResult.Content.Stream);
			}

			return;
		}

		// Render the phoria-island web component

		writer.Write($"<{TagName} component=\"{component.ComponentName}\"");

		if (component.Client != null)
		{
			if (component.Client.Value == null)
			{
				writer.Write($" {component.Client.Name}");
			}
			else
			{
				writer.Write($" {component.Client.Name}=\"{encoder.Encode(component.Client.Value)}\"");
			}
		}

		// TODO: Maybe it would be more performant to write props to a script tag so they are already parsed as javascript by the browser
		if (ssrResult?.Props != null)
		{
			writer.Write(" props=\"");
			WriteUtf8Stream(writer, ssrResult.Props.Stream, encoder);
			writer.Write("\"");
		}
		else if (component.Props != null)
		{
			writer.Write($" props=\"{encoder.Encode(options.Islands.PropsSerializer.Serialize(component.Props))}\"");
		}

		if (component.Framework != null)
		{
			writer.Write($" framework=\"{component.Framework}\"");
		}

		writer.Write(">");

		if (ssrResult != null)
		{
			WriteUtf8Stream(writer, ssrResult.Content.Stream);
		}

		writer.Write($"</{TagName}>");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteUtf8Stream(
		TextWriter textWriter,
		RecyclableMemoryStream stream,
		TextEncoder? encoder = null)
	{
		if (stream == null || stream.Length == 0)
		{
			return;
		}

		stream.Position = 0;

		var textWriterBufferWriter = new TextWriterBufferWriter(textWriter, encoder);

		Encoding.UTF8.GetDecoder().Convert(
			stream.GetReadOnlySequence(),
			textWriterBufferWriter,
			true,
			out _,
			out _);
	}
}
