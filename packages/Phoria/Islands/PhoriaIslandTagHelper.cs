using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.IO;
using Phoria.IO;
using Phoria.Server;

namespace Phoria.Islands;

public enum ClientMode
{
	OnLoad,
	Only
}

public class PhoriaIslandTagHelper
	: TagHelper
{
	private readonly IPhoriaIslandRegistry registry;
	private readonly IPhoriaServerMonitor serverMonitor;
	private readonly IPhoriaIslandSsr phoriaIslandSsr;

	public required string Component { get; set; }
	public object? Props { get; set; }
	public ClientMode? Client { get; set; }

	public PhoriaIslandTagHelper(
		IPhoriaIslandRegistry registry,
		IPhoriaServerMonitor serverMonitor,
		IPhoriaIslandSsr phoriaIslandSsr)
	{
		this.registry = registry;
		this.serverMonitor = serverMonitor;
		this.phoriaIslandSsr = phoriaIslandSsr;
	}

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		PhoriaIslandRenderMode renderMode = Client switch
		{
			ClientMode.OnLoad => PhoriaIslandRenderMode.Isomorphic,
			ClientMode.Only => PhoriaIslandRenderMode.ClientOnly,
			_ => PhoriaIslandRenderMode.ServerOnly
		};

		if (renderMode != PhoriaIslandRenderMode.ServerOnly
			&& serverMonitor.ServerStatus.Health != PhoriaServerHealth.Healthy)
		{
			// We have to render the component on the server, but the server is not healthy

			// TODO: Log this
			// TODO: Throw an exception?

			output.SuppressOutput();

			return;
		}

		var component = new PhoriaIslandComponent
		{
			ComponentName = Component,
			Props = Props,
			RenderMode = renderMode
		};

		registry.RegisterComponent(component);

		output.TagName = null;
		output.TagMode = TagMode.StartTagAndEndTag;

		if (renderMode != PhoriaIslandRenderMode.ServerOnly)
		{
			// Render the phoria-island web component

			// TODO: Maybe allow setting the tag name via config?
			output.TagName = "phoria-island";

			output.Attributes.Add("component", component.ComponentName);

			// TODO: Support other directives - see Astro for inspiration

			string? clientDirective = Client switch
			{
				ClientMode.Only => "client:only",
				ClientMode.OnLoad => "client:load",
				_ => null
			};

			if (clientDirective != null)
			{
				output.Attributes.Add(new TagHelperAttribute(
					clientDirective,
					null,
					HtmlAttributeValueStyle.Minimized));
			}
		}

		if (renderMode != PhoriaIslandRenderMode.ClientOnly)
		{
			try
			{
				PhoriaIslandSsrResult ssrResult = await phoriaIslandSsr.RenderComponent(component);

				if (ssrResult.Props != null)
				{
					output.Attributes.Add("props", CreateUtf8TextWriter(ssrResult.Props.Stream).ToString());
				}

				var ssrOutput = new PooledStream();
				await ssrResult.CopyToStream(ssrOutput.Stream);

				output.Content.SetHtmlContent(CreateUtf8TextWriter(ssrOutput.Stream).ToString());
			}
			catch (Exception)
			{
				// TODO: Log this
				// TODO: Throw an exception?

				output.SuppressOutput();

				return;
			}
		}
	}

	private static StringWriter CreateUtf8TextWriter(RecyclableMemoryStream stream)
	{
		var textWriter = new StringWriter();
		WriteUtf8Stream(textWriter, stream);

		return textWriter;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void WriteUtf8Stream(TextWriter textWriter, RecyclableMemoryStream stream)
	{
		if (stream == null || stream.Length == 0)
		{
			return;
		}

		stream.Position = 0;

		var textWriterBufferWriter = new TextWriterBufferWriter(textWriter);

		Encoding.UTF8.GetDecoder().Convert(
			stream.GetReadOnlySequence(),
			textWriterBufferWriter,
			true,
			out _,
			out _);
	}
}
