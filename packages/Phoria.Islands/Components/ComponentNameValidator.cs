using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Phoria.Islands.Components;

public interface IComponentNameValidator
{
	bool IsValid(string componentName);
}

internal class ComponentNameValidator : IComponentNameValidator
{
	/// <summary>
	/// Regular expression used to validate JavaScript identifiers. Used to ensure component
	/// names are valid.
	/// Based off https://gist.github.com/Daniel15/3074365
	/// </summary>
	private static readonly Regex identifierRegex =
		new(@"^[a-zA-Z_$][0-9a-zA-Z_$]*(?:\[(?:"".+""|\'.+\'|\d+)\])*?$", RegexOptions.Compiled);


	private static readonly ConcurrentDictionary<string, bool> componentNameValidCache =
		new(StringComparer.Ordinal);

	public bool IsValid(string componentName)
	{
		return componentNameValidCache.GetOrAdd(
			componentName,
			compName => compName.Split('.').All(segment => identifierRegex.IsMatch(segment)));
	}
}
