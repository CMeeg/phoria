namespace Phoria.Server;

public enum PhoriaServerHealth
{
	Unknown,
	Healthy,
	Unhealthy
}

public enum PhoriaServerMode
{
	Unknown,
	Development,
	Production
}

public record PhoriaServerStatus
{
	public PhoriaServerHealth Health { get; init; } = PhoriaServerHealth.Unknown;
	public PhoriaServerMode Mode { get; init; } = PhoriaServerMode.Development;
	public string[] Frameworks { get; init; } = [];
	public required string Url { get; init; }
}
