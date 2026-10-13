using Phoria.Vite;
using Xunit;
using static Phoria.Tests.TestUtilities.GoldenManifestFixture;

namespace Phoria.Tests.Vite;

public class ViteSsrManifestTests
{
	[Fact]
	public void Indexer_ComponentPathKey_ReturnsChunkFiles()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		string[]? files = manifest["src/components/Counter/Counter.tsx"];

		Assert.NotNull(files);
		Assert.NotEmpty(files);
		Assert.All(files, f => Assert.StartsWith("/ui/assets/", f, StringComparison.Ordinal));
	}

	[Fact]
	public void Indexer_UnknownKey_ReturnsNull()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		Assert.Null(manifest["does/not/exist.tsx"]);
	}

	[Fact]
	public void Indexer_AssetFilenameKey_ReturnsNull()
	{
		// The golden manifest has no filename-only keys, so the second-level lookup that
		// PhoriaIslandPreloadTagHelper performs with Path.GetFileName(file) yields nothing.
		ViteSsrManifest manifest = LoadGoldenManifest();

		Assert.Null(manifest["Counter-D9HeyafA.js"]);
	}

	[Fact]
	public void Indexer_SvelteComponent_ReturnsChunkFiles()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		string[]? files = manifest["src/components/Counter/Counter.svelte"];

		Assert.NotNull(files);
		Assert.NotEmpty(files);
		Assert.All(files, f => Assert.StartsWith("/ui/assets/", f, StringComparison.Ordinal));
	}

	[Fact]
	public void Indexer_VueComponent_ReturnsChunkFiles()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		string[]? files = manifest["src/components/Counter/Counter.vue"];

		Assert.NotNull(files);
		Assert.NotEmpty(files);
		Assert.All(files, f => Assert.StartsWith("/ui/assets/", f, StringComparison.Ordinal));
	}

	[Fact]
	public void Indexer_CommonjsKey_ReturnsChunkFiles()
	{
		ViteSsrManifest manifest = LoadGoldenManifest();

		// The ?commonjs-exports suffix is exactly what Rolldown changes.
		// This key must exist and resolve on Vite 6.
		string[]? files = manifest[
			"../../\u0000/repo/node_modules/.pnpm/react-dom@19.0.0_react@19.0.0/node_modules/react-dom/cjs/react-dom-client.production.js?commonjs-exports"];

		Assert.NotNull(files);
		Assert.Single(files);

		string file = files[0];
		Assert.StartsWith("/ui/assets/client-", file, StringComparison.Ordinal);
		Assert.EndsWith(".js", file, StringComparison.Ordinal);
	}

	[Fact]
	public void ContainsKey_KnownComponent_ReturnsTrue()
	{
		IViteSsrManifest manifest = LoadGoldenManifest();

		Assert.True(manifest.ContainsKey("src/components/Counter/Counter.tsx"));
	}

	[Fact]
	public void Keys_ContainsComponentKeys()
	{
		IViteSsrManifest manifest = LoadGoldenManifest();

		Assert.Contains("src/components/Counter/Counter.tsx", manifest.Keys);
		Assert.Contains("src/components/Counter/Counter.svelte", manifest.Keys);
		Assert.Contains("src/components/Counter/Counter.vue", manifest.Keys);
	}
}
