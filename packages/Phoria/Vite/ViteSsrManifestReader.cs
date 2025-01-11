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
/// Provides access to the Vite SSR manifest file.
/// </summary>
public interface IViteSsrManifestReader
{
	/// <summary>
	/// Reads the Vite SSR manifest file.
	/// </summary>
	/// <returns>If the Vite dev server is running an empty SSR manifest, otherwise the SSR manifest from the build output.</returns>
	IViteSsrManifest ReadSsrManifest();
}

/// <inheritdoc cref="IViteSsrManifestReader">
/// <param name="logger">The service used to log messages.</param>
/// <param name="options">Phoria configuration options.</param>
/// <param name="viteDevServer">Vite Dev Server status.</param>
/// <param name="environment">Information about the web hosting environment.</param>
public sealed class ViteSsrManifestReader(
	ILogger<ViteSsrManifestReader> logger,
	IOptions<PhoriaOptions> options,
	IPhoriaServerMonitor serverMonitor,
	IWebHostEnvironment environment)
		: IViteSsrManifestReader, IDisposable
{
	private const string ManifestName = "ssr-manifest.json";

	private static bool warnAboutManifestOnce = true;

	private readonly ILogger<ViteSsrManifestReader> logger = logger;
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly IWebHostEnvironment environment = environment;
	private readonly PhoriaOptions options = options.Value;

	private PhysicalFileProvider? fileProvider;
	private IChangeToken? changeToken;
	private IDisposable? changeTokenDispose;
	private IViteSsrManifest? viteSsrManifest;

	private static readonly JsonSerializerOptions jsonDeserializeOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public IViteSsrManifest ReadSsrManifest()
	{
		if (viteSsrManifest != null)
		{
			return viteSsrManifest;
		}

		// If the server is in development mode, return an empty SSR manifest

		if (serverMonitor.ServerStatus.Mode == PhoriaServerMode.Development)
		{
			if (warnAboutManifestOnce)
			{
				logger.LogSsrManifestFileWontBeRead();
				warnAboutManifestOnce = false;
			}

			viteSsrManifest = new ViteSsrManifest();

			return viteSsrManifest;
		}

		// Try to read the manifest from the file provider

		viteSsrManifest = TryReadSsrManifestFromFile();

		if (viteSsrManifest == null)
		{
			if (warnAboutManifestOnce)
			{
				logger.LogSsrManifestFileNotFound();
				warnAboutManifestOnce = false;
			}

			viteSsrManifest = new ViteSsrManifest();
		}

		return viteSsrManifest;
	}

	private IViteSsrManifest? TryReadSsrManifestFromFile()
	{
		// Read the name of the SSR manifest file from the configuration

		// TODO: Can this be injected? ViteManifestReader and ViteSsrManifestReader can use the same fileprovider
		string viteRoot = options.Root.Substring(options.Root.LastIndexOf('/') + 1);
		fileProvider ??= new PhysicalFileProvider(Path.Combine(environment.ContentRootPath, viteRoot, options.Build.OutDir, "phoria", "client", ".vite"));

		IFileInfo ssrManifestFile = fileProvider.GetFileInfo(ManifestName);

		// If the SSR manifest file exists, deserialize it into a dictionary

		changeTokenDispose?.Dispose();

		if (ssrManifestFile.Exists)
		{
			// Read the SSR manifest file and deserialize it into a dictionary

			using Stream readStream = ssrManifestFile.CreateReadStream();
			IReadOnlyDictionary<string, string[]> files = JsonSerializer.Deserialize<IReadOnlyDictionary<string, string[]>>(
				readStream,
				jsonDeserializeOptions
			)!;

			viteSsrManifest = new ViteSsrManifest(files);

			// Watch the SSR manifest file for changes

			changeToken = fileProvider.Watch(ManifestName);

			if (changeToken.ActiveChangeCallbacks)
			{
				changeTokenDispose = changeToken.RegisterChangeCallback(
					state =>
					{
						OnSsrManifestChanged();
					},
					null
				);
			}
		}

		return viteSsrManifest;
	}

	private void OnSsrManifestChanged()
	{
		logger.LogDetectedChangeInSsrManifest();

		IViteSsrManifest? updatedSsrManifest = TryReadSsrManifestFromFile();

		if (updatedSsrManifest == null)
		{
			// The SSR manifest file must have been read previously if we are monitoring for changes so this should not happen - if it does it's an exception

			logger.LogSsrManifestFileNotFound();

			throw new FileNotFoundException("The SSR manifest file was not found.");
		}

		viteSsrManifest = updatedSsrManifest;
	}

	public void Dispose()
	{
		fileProvider?.Dispose();
		changeTokenDispose?.Dispose();
	}
}

internal static partial class ViteSsrManifestReaderLogMessages
{
	[LoggerMessage(
		EventId = EventFeature.Vite + 4,
		Message = "The SSR manifest file won't be read because the vite development service is enabled. The service will always return null files",
		Level = LogLevel.Information)]
	internal static partial void LogSsrManifestFileWontBeRead(this ILogger logger);

	[LoggerMessage(
		EventId = EventFeature.Vite + 5,
		Message = "Detected change in Vite SSR manifest - refreshing",
		Level = LogLevel.Information)]
	internal static partial void LogDetectedChangeInSsrManifest(this ILogger logger);

	[LoggerMessage(
		EventId = EventFeature.Vite + 6,
		Message = "The SSR manifest file was not found. Has the build process been executed?",
		Level = LogLevel.Error)]
	internal static partial void LogSsrManifestFileNotFound(this ILogger logger);
}
