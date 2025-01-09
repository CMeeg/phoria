namespace Phoria.Islands;

public interface IPhoriaIslandScopedContext
{
	IReadOnlyList<PhoriaIsland> Islands { get; }

	void AddIsland(PhoriaIsland island);
}

public sealed class PhoriaIslandScopedContext
	: IPhoriaIslandScopedContext
{
	private readonly List<PhoriaIsland> islands = [];

	public IReadOnlyList<PhoriaIsland> Islands => islands;

	public void AddIsland(PhoriaIsland island) => islands.Add(island);
}
