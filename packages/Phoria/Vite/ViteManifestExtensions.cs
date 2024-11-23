// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Phoria.Vite;

public static class ViteManifestExtensions
{
	public static IEnumerable<string> GetRecursiveCssFiles(
		this IViteManifest manifest,
		string chunkName) => GetRecursiveCssFiles(manifest, chunkName, new HashSet<string>());

	private static IEnumerable<string> GetRecursiveCssFiles(
		IViteManifest manifest,
		string chunkName,
		ICollection<string> proccessedChunks)
	{
		if (proccessedChunks.Contains(chunkName))
		{
			return [];
		}

		IViteChunk? chunk = manifest[chunkName];

		var cssFiles = new HashSet<string>(chunk?.Css ?? []);

		if (chunk?.Imports?.Any() == true)
		{
			proccessedChunks.Add(chunkName);

			foreach (string import in chunk.Imports)
			{
				IEnumerable<string> otherCssFiles = GetRecursiveCssFiles(manifest, import, proccessedChunks);

				cssFiles.UnionWith(otherCssFiles);
			}
		}

		return cssFiles.Distinct();
	}
}
