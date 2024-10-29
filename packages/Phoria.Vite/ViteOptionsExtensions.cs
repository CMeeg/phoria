// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

namespace Phoria.Vite;

/// <summary>
/// Define extension methods for <see cref="ViteOptions"/>
/// </summary>
internal static class ViteOptionsExtensions
{
	internal static string GetViteDevServerUrl(this ViteOptions options)
	{
		// Get the port and host from the configuration.
		string host = options.Server.Host;
		ushort? port = options.Server.Port;
		// Check if https is enabled.
		bool https = options.Server.Https;

		string serverUrl = $"{(https ? "https" : "http")}://{host}";
		if (port is not null)
		{
			serverUrl += $":{port}";
		}

		// Return the url.
		return serverUrl;
	}

	internal static string GetViteDevServerUrlWithBasePath(this ViteOptions options)
	{
		string serverUrl = options.GetViteDevServerUrl();
		string basePath = options.GetViteBasePath();
		return $"{serverUrl}{basePath}";
	}

	internal static string GetViteBasePath(this ViteOptions options)
	{
		string? basePath = options.Base?.Trim('/');
		return string.IsNullOrEmpty(basePath)
			? string.Empty
			: $"/{basePath}";
	}
}
