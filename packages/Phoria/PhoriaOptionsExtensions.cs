namespace Phoria;

/// <summary>
/// Define extension methods for <see cref="PhoriaOptions"/>
/// </summary>
internal static class PhoriaOptionsExtensions
{
	internal static string GetServerUrl(this PhoriaOptions options)
	{
		string serverUrl = $"{(options.Server.Https ? "https" : "http")}://{options.Server.Host}";
		if (options.Server.Port is not null)
		{
			serverUrl += $":{options.Server.Port}";
		}

		return serverUrl;
	}

	internal static string GetServerUrlWithBasePath(this PhoriaOptions options)
	{
		return $"{GetServerUrl(options)}{GetBasePath(options)}";
	}

	internal static string GetBasePath(this PhoriaOptions options)
	{
		string path = options.Base.Trim('/');
		return string.IsNullOrEmpty(path)
			? string.Empty
			: $"/{path}";
	}
}
