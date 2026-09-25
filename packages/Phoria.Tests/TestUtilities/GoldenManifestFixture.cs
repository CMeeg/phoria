using System.Text.Json;
using Phoria.Vite;

namespace Phoria.Tests.TestUtilities;

internal static class GoldenManifestFixture
{
	private static readonly JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

	public static ViteSsrManifest LoadGoldenManifest()
	{
		string path = Path.Combine(AppContext.BaseDirectory, "TestData", "ssr-manifest.json");
		using FileStream stream = File.OpenRead(path);

		IReadOnlyDictionary<string, string[]> files =
			JsonSerializer.Deserialize<IReadOnlyDictionary<string, string[]>>(stream, jsonOptions)!;

		return new ViteSsrManifest(files);
	}
}
