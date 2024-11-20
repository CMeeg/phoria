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
	// Defaults here must be in sync with the defaults set in `phoria-islands/src/server/appsettings.ts`

	public const string SectionName = "Phoria";

	public string Root { get; set; } = "ui";

	/// <summary>
	/// The base public path where your assets are located, relative to the content root.
	/// Default is "/ui".
	/// </summary>
	/// <remarks>
	/// This setting is equivalent to Vite's `base` config option.
	/// See https://vite.dev/config/shared-options.html#base
	/// </remarks>
	public string Base { get; set; } = "/ui";
	public string Entry { get; set; } = string.Empty;
	public string SsrBase { get; set; } = "/ssr";
	public string SsrEntry { get; set; } = string.Empty;

	public PhoriaServerOptions Server { get; set; } = new();

	public PhoriaBuildOptions Build { get; set; } = new();

	public PhoriaIslandsOptions Islands { get; set; } = new();
}

public record PhoriaServerOptions
{
	public string Host { get; set; } = "localhost";
	public ushort? Port { get; set; } = 5173;
	public bool Https { get; set; }
	public int HealthCheckTimeout { get; set; } = 5;
	/// <summary>
	/// The interval in seconds at which the server monitor checks the server status.
	/// Default is 5 seconds. Setting this to null or a negative number disables the server monitor.
	/// </summary>
	public int HealthCheckInterval { get; set; } = 5;
	public ProcessOptions? Process { get; set; }

	public record ProcessOptions
	{
		/// <summary>
		/// The command to start the server process.
		/// </summary>
		public required string Command { get; set; }
		/// <summary>
		/// The arguments for the server process command.
		/// </summary>
		public string[]? Arguments { get; set; }
		public int HealthCheckInterval { get; set; } = 10;
	}
}

public record PhoriaBuildOptions
{
	public string OutDir { get; set; } = "dist";
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
