using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Vite;

public class ViteManifestReaderTests
{
	[Fact]
	public void ReadManifest_ReadsChunksWithCssAndImports()
	{
		using var fixture = new ManifestFixture("manifest.json", "{\"src/entry.ts\":{\"file\":\"assets/entry.js\",\"css\":[\"assets/entry.css\"],\"imports\":[\"_shared.js\"]}}");
		using var reader = new ViteManifestReader(
			new ListLogger<ViteManifestReader>(),
			Options.Create(fixture.Options),
			new StubServerMonitor(PhoriaServerMode.Production),
			fixture.Environment);

		IViteManifest manifest = reader.ReadManifest();
		IViteChunk chunk = manifest["src/entry.ts"]!;

		Assert.Equal("assets/entry.js", chunk.File);
		Assert.Contains("assets/entry.css", chunk.Css!);
		Assert.Contains("_shared.js", chunk.Imports!);
	}

	[Fact]
	public void ReadManifest_WhenFileIsMissing_ReturnsEmptyManifest()
	{
		using var fixture = new ManifestFixture("other.json", "{}");
		using var reader = new ViteManifestReader(
			new ListLogger<ViteManifestReader>(),
			Options.Create(fixture.Options),
			new StubServerMonitor(PhoriaServerMode.Production),
			fixture.Environment);

		IViteManifest manifest = reader.ReadManifest();

		Assert.Empty(manifest.Keys);
		Assert.False(manifest.ContainsKey("src/entry.ts"));
	}

	private sealed class ManifestFixture : IDisposable
	{
		private readonly string root = Path.Combine(Path.GetTempPath(), $"phoria-manifest-{Guid.NewGuid():N}");

		public ManifestFixture(string fileName, string content)
		{
			string directory = Path.Combine(root, "ui", "dist", "phoria", "client", ".vite");
			Directory.CreateDirectory(directory);
			File.WriteAllText(Path.Combine(directory, fileName), content);
			Options = new PhoriaOptions { Root = "ui", Build = new PhoriaBuildOptions { OutDir = "dist" } };
			Environment = new StubWebHostEnvironment(root);
		}

		public PhoriaOptions Options { get; }
		public StubWebHostEnvironment Environment { get; }

		public void Dispose() => Directory.Delete(root, true);
	}
}
