using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Phoria.Islands;

namespace Phoria;

/// <summary>
/// Options for Phoria.
/// </summary>
public record PhoriaOptions
{
	public const string SectionName = "Phoria";

	// TODO: Is this a sensible default?
	// TODO: Include leading slash to match Vite?

	/// <summary>
	/// The subfolder where your assets will be located, including the manifest file.
	/// This value is relative to the content root in development, and the web root in production.
	/// Default is "/phoria".
	/// </summary>
	public string? Base { get; set; } = "/phoria";

	// TODO: Is this a sensible default?

	/// <summary>
	/// The Vite manifest file name. Default is "client/.vite/manifest.json".
	/// </summary>
	public string Manifest { get; set; } = Path.Combine("client", ".vite", "manifest.json");

	// TODO: Is this a sensible default?

	/// <summary>
	/// The Vite SSR manifest file name. Default is "client/.vite/ssr-manifest.json".
	/// </summary>
	public string SsrManifest { get; set; } = Path.Combine("client", ".vite", "ssr-manifest.json");

	public PhoriaServerOptions Server { get; set; } = new();

	public PhoriaIslandsOptions Islands { get; set; } = new();
}

public record PhoriaServerOptions
{
	public string Host { get; set; } = "localhost";
	public ushort? Port { get; set; } = 5173;
	public bool Https { get; set; }
	/// <summary>
	/// The interval in seconds at which the server monitor checks the server status.
	/// Default is 5 seconds. Setting this to null or a negative number disables the server monitor.
	/// </summary>
	public int HealthCheckInterval { get; set; } = 5;
	public int HealthCheckTimeout { get; set; } = 5;
}

public record PhoriaIslandsOptions
{
	private static readonly IPhoriaIslandPropsSerializer defaultPropsSerializer = new SystemTextJsonPropsSerializer(
		new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			PropertyNameCaseInsensitive = true,
		});

	public IPhoriaIslandPropsSerializer PropsSerializer { get; set; } = defaultPropsSerializer;
}
