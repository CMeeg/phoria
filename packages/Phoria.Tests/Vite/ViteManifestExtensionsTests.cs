using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Vite;

public class ViteManifestExtensionsTests
{
	[Fact]
	public void GetRecursiveCssFiles_CollectsCssFromImportedChunks()
	{
		IViteManifest manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["entry"] = new() { File = "entry.js", Css = ["entry.css"], Imports = ["shared"] },
			["shared"] = new() { File = "shared.js", Css = ["shared.css"] }
		});

		Assert.Equal(new HashSet<string> { "entry.css", "shared.css" }, manifest.GetRecursiveCssFiles("entry").ToHashSet());
	}

	[Fact]
	public void GetRecursiveCssFiles_TerminatesOnCyclicImports()
	{
		IViteManifest manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["a"] = new() { File = "a.js", Css = ["a.css"], Imports = ["b"] },
			["b"] = new() { File = "b.js", Css = ["b.css"], Imports = ["a"] }
		});

		Assert.Equal(new HashSet<string> { "a.css", "b.css" }, manifest.GetRecursiveCssFiles("a").ToHashSet());
	}
}
