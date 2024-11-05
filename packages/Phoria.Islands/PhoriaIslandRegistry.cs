using Phoria.Islands.Components;

namespace Phoria.Islands;

public interface IPhoriaIslandRegistry
{
	IReadOnlyList<PhoriaIslandComponent> Components { get; }

	void RegisterComponent(PhoriaIslandComponent component);
}

public sealed class PhoriaIslandRegistry : IPhoriaIslandRegistry
{
	private readonly IComponentNameValidator componentNameValidator;

	private readonly List<PhoriaIslandComponent> components = [];

	public IReadOnlyList<PhoriaIslandComponent> Components => components;

	public PhoriaIslandRegistry(IComponentNameValidator componentNameValidator)
	{
		this.componentNameValidator = componentNameValidator;
	}

	public void RegisterComponent(PhoriaIslandComponent component)
	{
		if (!componentNameValidator.IsValid(component.ComponentName))
		{
			throw new ArgumentException($"Invalid component name '{component.ComponentName}'");
		}

		components.Add(component);
	}
}
