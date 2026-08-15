using Microsoft.Extensions.Options;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Vite;

public class ViteSsrManifestReaderTests
{
	[Fact]
	public void ReadSsrManifest_ReadsComponentPathMapping()
	{
		using var fixture = new Fixture("{\"/src/components/Counter.tsx\":[\"assets/Counter.js\",\"assets/Counter.css\"]}");
		using var reader = new ViteSsrManifestReader(
			new ListLogger<ViteSsrManifestReader>(),
			Options.Create(fixture.Options),
			new StubServerMonitor(PhoriaServerMode.Production),
			fixture.Environment);

		IViteSsrManifest manifest = reader.ReadSsrManifest();

		Assert.NotNull(manifest["/src/components/Counter.tsx"]);
		Assert.Equal(["assets/Counter.js", "assets/Counter.css"], manifest["/src/components/Counter.tsx"]!);
	}

	[Fact]
	public void ReadSsrManifest_WhenFileIsMissing_ReturnsEmptyManifest()
	{
		using var fixture = new Fixture(null);
		using var reader = new ViteSsrManifestReader(
			new ListLogger<ViteSsrManifestReader>(),
			Options.Create(fixture.Options),
			new StubServerMonitor(PhoriaServerMode.Production),
			fixture.Environment);

		Assert.Empty(reader.ReadSsrManifest().Keys);
	}

	private sealed class Fixture : IDisposable
	{
		private readonly string root = Path.Combine(Path.GetTempPath(), $"phoria-ssr-manifest-{Guid.NewGuid():N}");

		public Fixture(string? content)
		{
			string directory = Path.Combine(root, "ui", "dist", "phoria", "client", ".vite");
			Directory.CreateDirectory(directory);
			if (content is not null) File.WriteAllText(Path.Combine(directory, "ssr-manifest.json"), content);
			Options = new PhoriaOptions();
			Environment = new StubWebHostEnvironment(root);
		}

		public PhoriaOptions Options { get; }
		public StubWebHostEnvironment Environment { get; }

		public void Dispose() => Directory.Delete(root, true);
	}
}
