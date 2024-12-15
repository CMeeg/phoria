namespace Phoria.Islands;

public interface IPhoriaIslandScopedContext
{
	IReadOnlyList<PhoriaIslandComponent> Components { get; }

	void AddComponent(PhoriaIslandComponent component);
}

public sealed class PhoriaIslandScopedContext
	: IPhoriaIslandScopedContext
{
	private readonly List<PhoriaIslandComponent> components = [];

	public IReadOnlyList<PhoriaIslandComponent> Components => components;

	public void AddComponent(PhoriaIslandComponent component) => components.Add(component);
}
