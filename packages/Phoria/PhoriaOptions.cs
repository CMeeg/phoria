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
	public string? Base { get; init; } = "/phoria";

	// TODO: Is this still a sensible default?

	/// <summary>
	/// The Vite manifest file name. Default is ".vite/manifest.json".
	/// </summary>
	public required string Manifest { get; init; } = Path.Combine(".vite", "manifest.json");

	public PhoriaServerOptions Server { get; init; } = new();
}

public record PhoriaServerOptions
{
	public string Host { get; init; } = "localhost";
	public ushort? Port { get; init; } = 5173;
	public bool Https { get; init; }
	/// <summary>
	/// The interval in seconds at which the server monitor checks the server status.
	/// Default is 5 seconds. Setting this to null or a negative number disables the server monitor.
	/// </summary>
	public int HealthCheckInterval { get; init; } = 5;
	public int HealthCheckTimeout { get; init; } = 5;
}
