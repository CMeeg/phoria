using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Phoria.Islands.Components;

namespace Phoria.Islands.UI;

public class PhoriaIslandTagHelper : TagHelper
{
	private readonly IPhoriaIslandRegistry registry;
	private readonly IServiceProvider serviceProvider;

	public required string Component { get; set; }
	public object? Props { get; set; }
	// TODO: Decide on what this attribute will look like
	public ClientMode Client { get; set; } = ClientMode.None;
	// TODO: Shouldn't need a "container" now as the web component is the container
	// public string ContainerTag { get; set; } = "div";
	// public string? ContainerId { get; set; }
	// public string? ContainerClass { get; set; }

	public PhoriaIslandTagHelper(
		IPhoriaIslandRegistry registry,
		IServiceProvider serviceProvider)
	{
		this.registry = registry;
		this.serviceProvider = serviceProvider;
	}

	public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
	{
		bool clientOnly = false;
		bool serverOnly = true;

		switch (Client)
		{
			case ClientMode.OnLoad:
				serverOnly = false;
				break;
			case ClientMode.Only:
				clientOnly = true;
				serverOnly = false;
				break;
		}

		var component = new PhoriaIslandComponent
		{
			ComponentName = Component,
			Props = Props,
			// ContainerTag = ContainerTag,
			// ContainerId = ContainerId,
			// ContainerClass = ContainerClass,
			ServerOnly = serverOnly,
			ClientOnly = clientOnly
		};

		registry.RegisterComponent(component);

		// reactComponent.NonceProvider = config.ScriptNonceProvider;
		// reactComponent.BootstrapInPlace = bootstrapInPlace;
		// reactComponent.BootstrapScriptContentProvider = bootstrapScriptContentProvider;

		// if (exceptionHandler != null)
		// {
		//     reactComponent.ExceptionHandler = exceptionHandler;
		// }

		output.TagName = null;
		output.TagMode = TagMode.StartTagAndEndTag;

		// TODO: RenderHtml gets the Node rendered output stream and WriteIslandOutputHtmlTo writes the stream to the writer - I think the `ComponentRenderer` needs splitting so it's not responsible for writing to the writer
		ComponentRenderer componentRenderer = serviceProvider.GetRequiredService<ComponentRenderer>();
		await componentRenderer.RenderHtml(component);

		output.Content.SetHtmlContent(new ActionHtmlString(writer => componentRenderer.WriteIslandOutputHtmlTo(component, writer)));
	}
}

public enum ClientMode
{
	None,
	OnLoad,
	Only
}
