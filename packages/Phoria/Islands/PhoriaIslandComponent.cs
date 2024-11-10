namespace Phoria.Islands;

public enum PhoriaIslandRenderMode
{
	ServerOnly,
	ClientOnly,
	Isomorphic
}

public enum ClientMode
{
	OnLoad,
	Only
}

public class PhoriaIslandComponent
{
	/// <summary>
	/// Gets or sets the name of the component
	/// </summary>
	public required string ComponentName { get; set; }

	/// <summary>
	/// Sets the props for this component
	/// </summary>
	public object? Props { get; set; }
	public PhoriaIslandRenderMode RenderMode { get; set; } = PhoriaIslandRenderMode.Isomorphic;
	public ClientMode? Client { get; set; }
	public string? Framework { get; set; }
	public string? ComponentPath { get; set; }
}
