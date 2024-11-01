// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Collections;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Phoria.Vite.DevServer;
using Phoria.Vite.Logging;

namespace Phoria.Vite.Manifest;

/// <summary>
/// This class is used to read the manifest.json file generated by Vite.
/// </summary>
public sealed class ViteManifest : IViteManifest, IDisposable
{
	private static bool warnAboutManifestOnce = true;
	private readonly ILogger<ViteManifest> logger;
	private IReadOnlyDictionary<string, ViteChunk> chunks = null!; // chunks is always initialized, just indirectly from the constructor
	private readonly IViteDevServerStatus devServerStatus;
	private readonly string? basePath;
	private readonly ViteOptions viteOptions;

	private readonly PhysicalFileProvider? fileProvider;
	private IChangeToken? changeToken;
	private IDisposable? changeTokenDispose;

	private static readonly JsonSerializerOptions jsonDeserializeOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	/// <summary>
	/// Initializes a new instance of the <see cref="ViteManifest"/> class.
	/// </summary>
	/// <param name="logger">The service used to log messages.</param>
	/// <param name="options">The Vite configuration options.</param>
	/// <param name="environment">Information about the web hosting environment.</param>
	public ViteManifest(
		ILogger<ViteManifest> logger,
		IOptions<ViteOptions> options,
		IViteDevServerStatus viteDevServer,
		IWebHostEnvironment environment
	)
	{
		this.logger = logger;
		devServerStatus = viteDevServer;
		viteOptions = options.Value;

		// If the middleware is enabled, don't read the manifest.json file.
		if (viteDevServer.IsEnabled)
		{
			if (warnAboutManifestOnce)
			{
				logger.LogManifestFileWontBeRead();
				warnAboutManifestOnce = false;
			}

			chunks = new Dictionary<string, ViteChunk>();
			return;
		}

		// If the manifest file is in a subfolder, get the subfolder path.
		basePath = viteOptions.Base?.TrimStart('/');

		// Get the manifest.json file path
		string rootDir = Path.Combine(environment.WebRootPath, basePath ?? string.Empty);
		fileProvider = new(rootDir);
		InitializeManifest();
	}

	/// <summary>
	/// Gets the Vite chunk for the specified entry point if it exists.
	/// If Dev Server is enabled, this will always return <see langword="null"/>.
	/// </summary>
	/// <param name="key"></param>
	/// <returns>The chunk if it exists, otherwise <see langword="null"/>.</returns>
	public IViteChunk? this[string key]
	{
		get
		{
			// TODO: It doesn't seem "right" that the manifest needs to know about the dev server status. Maybe we should move this logic to the caller?
			if (devServerStatus.IsEnabled)
			{
				logger.LogManifestFileReadAttempt(key);
				return null;
			}

			// If proactive callbacks are disabled, then we need to check the token
			if (changeToken?.HasChanged ?? false)
			{
				OnManifestChanged();
			}

			if (!string.IsNullOrEmpty(basePath))
			{
				string basePath = this.basePath.Trim('/');
				// If the key starts with the base path, remove it and warn the user.
				if (key.StartsWith(basePath, StringComparison.InvariantCultureIgnoreCase))
				{
					logger.LogRequestingChunkWithBasePath(key);
					key = key[basePath.Length..].TrimStart('/');
				}
			}

			// Try to get the chunk from the dictionary.
			if (!chunks.TryGetValue(key, out ViteChunk? chunk))
			{
				logger.LogChunkNotFound(key);
			}

			return chunk;
		}
	}

	private void InitializeManifest()
	{
		// Read the name of the manifest file from the configuration.
		string manifestName = viteOptions.Manifest;
		IFileInfo manifestFile = fileProvider!.GetFileInfo(manifestName); // Not null if we get to this point

		// If the manifest file doesn't exist, try to remove the ".vite/" prefix from the manifest file name. The default name for Vite 5 is ".vite/manifest.json" but for Vite 4 is "manifest.json".
		if (!manifestFile.Exists && manifestName.StartsWith(".vite", StringComparison.InvariantCultureIgnoreCase))
		{
			// Get the manifest.json file name without the ".vite/" prefix.
			string legacyManifestName = Path.GetFileName(manifestName);

			// Get the manifest.json file path
			manifestFile = fileProvider.GetFileInfo(legacyManifestName);
		}

		// If the manifest.json file exists, deserialize it into a dictionary.
		changeTokenDispose?.Dispose();
		if (manifestFile.Exists)
		{
			// Read the manifest.json file and deserialize it into a dictionary
			using Stream readStream = manifestFile.CreateReadStream();
			chunks = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ViteChunk>>(
				readStream,
				jsonDeserializeOptions
			)!;

			changeToken = fileProvider.Watch(manifestName);
			if (changeToken.ActiveChangeCallbacks)
			{
				changeTokenDispose = changeToken.RegisterChangeCallback(
					state =>
					{
						OnManifestChanged();
					},
					null
				);
			}
		}
		else
		{
			if (warnAboutManifestOnce)
			{
				logger.LogManifestFileNotFound();
				warnAboutManifestOnce = false;
			}

			// Create an empty dictionary.
			chunks = new Dictionary<string, ViteChunk>();
		}
	}

	private void OnManifestChanged()
	{
		logger.LogDetectedChangeInManifest();
		InitializeManifest();
	}

	/// <inheritdoc/>
	IEnumerator<IViteChunk> IEnumerable<IViteChunk>.GetEnumerator()
	{
		return chunks.Values.GetEnumerator();
	}

	/// <inheritdoc/>
	IEnumerator IEnumerable.GetEnumerator() => chunks.Values.GetEnumerator();

	/// <inheritdoc/>
	IEnumerable<string> IViteManifest.Keys => chunks.Keys;

	/// <inheritdoc/>
	bool IViteManifest.ContainsKey(string key) => chunks.ContainsKey(key);

	public void Dispose()
	{
		fileProvider?.Dispose();
		changeTokenDispose?.Dispose();
	}
}