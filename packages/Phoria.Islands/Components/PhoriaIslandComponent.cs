namespace Phoria.Islands.Components;

public class PhoriaIslandComponent
{
	/// <summary>
	/// Gets or sets the name of the component
	/// </summary>
	public required string ComponentName { get; set; }

	// TODO: Not sure if this is needed, or if it makes sense to put this here
	public string? ComponentFramework { get; set; }

	// TODO: This is React-specific so need to refactor
	// private string containerId;

	// /// <summary>
	// /// Gets or sets the unique ID for the DIV container of this component
	// /// </summary>
	// public string ContainerId
	// {
	// 	get => containerId ??= reactIdGenerator.Generate();
	// 	set => containerId = value;
	// }

	/// <summary>
	/// Gets or sets the HTML tag the component is wrapped in
	/// </summary>
	// public string ContainerTag { get; set; } = "div";

	/// <summary>
	/// Gets or sets the HTML class for the container of this component
	/// </summary>
	// public string ContainerClass { get; set; }

	// TODO: Having both `ServerOnly` and `ClientOnly` makes no sense - need to refactor

	/// <summary>
	/// Get or sets if this components only should be rendered server side
	/// </summary>
	public bool ServerOnly { get; set; }

	/// <summary>
	/// Get or sets if this components only should be rendered client side
	/// </summary>
	public bool ClientOnly { get; set; }

	/// <summary>
	/// Sets the props for this component
	/// </summary>
	public object? Props { get; set; }

	// TODO: There shouldn't be a need for setting nonces or bootstrap scripts - maybe get rid of these

	// public Func<string> NonceProvider { get; set; }

	// public bool BootstrapInPlace { get; set; }

	// public delegate string BootstrapScriptContent(string componentId);

	// /// <summary>
	// /// If specified, this string will be placed in an inline &lt;script&gt; tag after window.__nrp props
	// /// </summary>
	// public BootstrapScriptContent BootstrapScriptContentProvider { get; set; }
}
