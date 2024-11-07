// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Collections;

namespace Phoria.Vite;

// TODO: Is the interface necessary?

/// <summary>
/// Represents a Vite manifest file.
/// </summary>
public interface IViteManifest
	: IEnumerable<IViteChunk>
{
	/// <summary>
	/// Gets the Vite chunk for the specified entry point if it exists.
	/// If Dev Server is enabled, this will always return <see langword="null"/>.
	/// </summary>
	/// <param name="key"></param>
	/// <returns>The chunk if it exists, otherwise <see langword="null"/>.</returns>
	IViteChunk? this[string key] { get; }

	/// <summary>
	/// Gets an enumerable collection that contains the chunk keys in the manifest.
	/// </summary>
	/// <returns>An enumerable collection that contains the chunk keys in the manifest.</returns>
	IEnumerable<string> Keys { get; }

	/// <summary>
	/// Determines whether the manifest contains a chunk with the specified key entry.
	/// </summary>
	/// <param name="key">The key entry to locate.</param>
	/// <returns>true if the manifest contains a chunk with the specified key entry; otherwise, false.</returns>
	bool ContainsKey(string key);
}

/// <inheritdoc cref="IViteManifest" />
public sealed class ViteManifest
	: IViteManifest
{
	private readonly IReadOnlyDictionary<string, ViteChunk> chunks;


	public IViteChunk? this[string key]
	{
		get
		{
			if (!chunks.TryGetValue(key, out ViteChunk? chunk))
			{
				return null;
			}

			return chunk;
		}
	}

	IEnumerable<string> IViteManifest.Keys => chunks.Keys;

	/// <summary>
	/// Initializes a new instance of the <see cref="ViteManifest"/> class.
	/// </summary>
	public ViteManifest()
	{
		chunks = new Dictionary<string, ViteChunk>();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ViteManifest"/> class.
	/// </summary>
	/// <param name="chunks">Vite manfest chunks.</param>
	public ViteManifest(IReadOnlyDictionary<string, ViteChunk> chunks)
	{
		this.chunks = chunks;
	}

	IEnumerator<IViteChunk> IEnumerable<IViteChunk>.GetEnumerator()
	{
		return chunks.Values.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() => chunks.Values.GetEnumerator();

	bool IViteManifest.ContainsKey(string key) => chunks.ContainsKey(key);
}
