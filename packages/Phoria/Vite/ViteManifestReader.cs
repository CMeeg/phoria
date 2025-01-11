// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Phoria.Logging;
using Phoria.Server;

namespace Phoria.Vite;

/// <summary>
/// Provides access to the Vite manifest file.
/// </summary>
public interface IViteManifestReader
{
	/// <summary>
	/// Reads the Vite manifest file.
	/// </summary>
	/// <returns>If the Vite dev server is running an empty manifest, otherwise the manifest from the build output.</returns>
	IViteManifest ReadManifest();
}

/// <inheritdoc cref="IViteManifestReader">
/// <param name="logger">The service used to log messages.</param>
/// <param name="options">Phoria configuration options.</param>
/// <param name="viteDevServer">Vite Dev Server status.</param>
/// <param name="environment">Information about the web hosting environment.</param>
public sealed class ViteManifestReader(
	ILogger<ViteManifestReader> logger,
	IOptions<PhoriaOptions> options,
	IPhoriaServerMonitor serverMonitor,
	IWebHostEnvironment environment)
		: IViteManifestReader, IDisposable
{
	private const string ManifestName = "manifest.json";

	private static bool warnAboutManifestOnce = true;

	private readonly ILogger<ViteManifestReader> logger = logger;
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IWebHostEnvironment environment = environment;
	private readonly PhoriaOptions options = options.Value;

	private PhysicalFileProvider? fileProvider;
	private IChangeToken? changeToken;
	private IDisposable? changeTokenDispose;
	private IViteManifest? viteManifest;

	private static readonly JsonSerializerOptions jsonDeserializeOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public IViteManifest ReadManifest()
	{
		if (viteManifest != null)
		{
			return viteManifest;
		}

		// If the server is in development mode, return an empty manifest

		if (serverMonitor.ServerStatus.Mode == PhoriaServerMode.Development)
		{
			if (warnAboutManifestOnce)
			{
				logger.LogManifestFileWontBeRead();
				warnAboutManifestOnce = false;
			}

			viteManifest = new ViteManifest();

			return viteManifest;
		}

		// Try to read the manifest from the file provider

		viteManifest = TryReadManifestFromFile();

		if (viteManifest == null)
		{
			if (warnAboutManifestOnce)
			{
				logger.LogManifestFileNotFound();
				warnAboutManifestOnce = false;
			}

			viteManifest = new ViteManifest();
		}

		return viteManifest;
	}

	private IViteManifest? TryReadManifestFromFile()
	{
		// Read the name of the manifest file from the configuration

		// TODO: Can this be injected? ViteManifestReader and ViteSsrManifestReader can use the same fileprovider
		string viteRoot = options.Root.Substring(options.Root.LastIndexOf('/') + 1);
		fileProvider ??= new PhysicalFileProvider(Path.Combine(environment.ContentRootPath, viteRoot, options.Build.OutDir, "phoria", "client", ".vite"));

		IFileInfo manifestFile = fileProvider.GetFileInfo(ManifestName);

		// If the manifest file exists, deserialize it into a dictionary

		changeTokenDispose?.Dispose();

		if (manifestFile.Exists)
		{
			// Read the manifest file and deserialize it into a dictionary

			using Stream readStream = manifestFile.CreateReadStream();
			IReadOnlyDictionary<string, ViteChunk> chunks = JsonSerializer.Deserialize<IReadOnlyDictionary<string, ViteChunk>>(
				readStream,
				jsonDeserializeOptions
			)!;

			viteManifest = new ViteManifest(chunks);

			// Watch the manifest file for changes

			changeToken = fileProvider.Watch(ManifestName);

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

		return viteManifest;
	}

	private void OnManifestChanged()
	{
		logger.LogDetectedChangeInManifest();

		IViteManifest? updatedManifest = TryReadManifestFromFile();

		if (updatedManifest == null)
		{
			// The manifest file must have been read previously if we are monitoring for changes so this should not happen - if it does it's an exception

			logger.LogManifestFileNotFound();

			throw new FileNotFoundException("The manifest file was not found.");
		}

		viteManifest = updatedManifest;
	}

	public void Dispose()
	{
		fileProvider?.Dispose();
		changeTokenDispose?.Dispose();
	}
}

internal static partial class ViteManifestReaderLogMessages
{
	[LoggerMessage(
		EventId = EventFeature.Vite + 1,
		Message = "The manifest file won't be read because the vite development service is enabled. The service will always return null chunks",
		Level = LogLevel.Information)]
	internal static partial void LogManifestFileWontBeRead(this ILogger logger);

	[LoggerMessage(
		EventId = EventFeature.Vite + 2,
		Message = "Detected change in Vite manifest - refreshing",
		Level = LogLevel.Information)]
	internal static partial void LogDetectedChangeInManifest(this ILogger logger);

	[LoggerMessage(
		EventId = EventFeature.Vite + 3,
		Message = "The manifest file was not found. Has the build process been executed?",
		Level = LogLevel.Error)]
	internal static partial void LogManifestFileNotFound(this ILogger logger);
}
