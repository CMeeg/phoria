using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.IO;
using Phoria.IO;

namespace Phoria.Islands;

public class PhoriaIslandHtmlContent(
	PhoriaIsland island,
	PhoriaIslandSsrResult? ssrResult,
	PhoriaOptions options)
	: IHtmlContent
{
	private const string TagName = "phoria-island";

	private readonly PhoriaIsland island = island;
	private readonly PhoriaIslandSsrResult? ssrResult = ssrResult;
	private readonly PhoriaOptions options = options;

	public void WriteTo(TextWriter writer, HtmlEncoder encoder)
	{
		if (island.RenderMode == PhoriaIslandRenderMode.ServerOnly)
		{
			if (ssrResult != null)
			{
				// Render the SSR result

				WriteUtf8Stream(writer, ssrResult.Content.Stream);
			}

			return;
		}

		// Render the phoria-island web component

		writer.Write($"<{TagName} component=\"{island.ComponentName}\"");

		if (island.Client != null)
		{
			if (island.Client.Value == null)
			{
				writer.Write($" {island.Client.Name}");
			}
			else
			{
				writer.Write($" {island.Client.Name}=\"{encoder.Encode(island.Client.Value)}\"");
			}
		}

		// TODO: Maybe it would be more performant to write props to a script tag so they are already parsed as javascript by the browser
		if (ssrResult?.Props != null)
		{
			writer.Write(" props=\"");
			WriteUtf8Stream(writer, ssrResult.Props.Stream, encoder);
			writer.Write("\"");
		}
		else if (island.Props != null)
		{
			writer.Write($" props=\"{encoder.Encode(options.Islands.PropsSerializer.Serialize(island.Props))}\"");
		}

		if (island.Framework != null)
		{
			writer.Write($" framework=\"{island.Framework}\"");
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
