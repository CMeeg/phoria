namespace Phoria.Islands;

public enum PhoriaIslandRenderMode
{
	ServerOnly,
	ClientOnly,
	Isomorphic
}

public static class Client
{
	public static PhoriaIslandClientDirective Only => new PhoriaIslandClientOnlyDirective();
	public static PhoriaIslandClientDirective Load => new PhoriaIslandClientLoadDirective();
	public static PhoriaIslandClientDirective Idle(int? timeout = null) => new PhoriaIslandClientIdleDirective(timeout);
	public static PhoriaIslandClientDirective Visible(string? rootMargin = null) => new PhoriaIslandClientVisibleDirective(rootMargin);
	public static PhoriaIslandClientDirective Media(string mediaQuery) => new PhoriaIslandClientMediaDirective(mediaQuery);
}

public class PhoriaIsland
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
	public PhoriaIslandClientDirective? Client { get; set; }
	public string? Framework { get; set; }
	public string? ComponentPath { get; set; }
}
