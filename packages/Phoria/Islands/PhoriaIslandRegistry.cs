namespace Phoria.Islands;

public interface IPhoriaIslandRegistry
{
	IReadOnlyList<PhoriaIslandComponent> Components { get; }

	void RegisterComponent(PhoriaIslandComponent component);
}

public sealed class PhoriaIslandRegistry
	: IPhoriaIslandRegistry
{
	// TODO: Use a dictionary so we can be more symmetrical with the JS version?
	private readonly List<PhoriaIslandComponent> components = [];

	public IReadOnlyList<PhoriaIslandComponent> Components => components;

	public void RegisterComponent(PhoriaIslandComponent component)
	{
		components.Add(component);
	}
}
