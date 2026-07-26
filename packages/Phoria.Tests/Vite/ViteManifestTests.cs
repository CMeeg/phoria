using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Vite;

public class ViteManifestTests
{
	private static ViteManifest CreateManifest()
	{
		var chunks = new Dictionary<string, ViteChunk>
		{
			["main.ts"] = new ViteChunk { File = "assets/main-abc123.js" },
			["style.css"] = new ViteChunk { File = "assets/style-def456.css" }
		};

		return new ViteManifest(chunks);
	}

	[Fact]
	public void Indexer_WithKnownKey_ReturnsChunk()
	{
		var manifest = CreateManifest();

		Assert.NotNull(((IViteManifest)manifest)["main.ts"]);
	}

	[Fact]
	public void Indexer_WithUnknownKey_ReturnsNull()
	{
		var manifest = CreateManifest();

		Assert.Null(((IViteManifest)manifest)["missing.ts"]);
	}

	[Fact]
	public void ContainsKey_WithKnownKey_ReturnsTrue()
	{
		IViteManifest manifest = CreateManifest();

		Assert.True(manifest.ContainsKey("main.ts"));
	}

	[Fact]
	public void ContainsKey_WithUnknownKey_ReturnsFalse()
	{
		IViteManifest manifest = CreateManifest();

		Assert.False(manifest.ContainsKey("missing.ts"));
	}

	[Fact]
	public void Keys_ReturnsAllChunkKeys()
	{
		IViteManifest manifest = CreateManifest();

		Assert.Contains("main.ts", manifest.Keys);
		Assert.Contains("style.css", manifest.Keys);
		Assert.Equal(2, manifest.Keys.Count());
	}

	[Fact]
	public void Enumeration_YieldsAllChunks()
	{
		IViteManifest manifest = CreateManifest();

		var files = manifest.Select(c => c.File).ToList();

		Assert.Equal(2, files.Count);
		Assert.Contains("assets/main-abc123.js", files);
		Assert.Contains("assets/style-def456.css", files);
	}
}
