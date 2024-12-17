using Microsoft.AspNetCore.Mvc;

namespace Phoria.Islands;

public class PhoriaIslandUrlHelper(IUrlHelper urlHelper, PhoriaOptions options)
{
	private readonly IUrlHelper urlHelper = urlHelper;
	private readonly string? basePath = options.Base?.Trim('/');

	public string GetContentUrl(string filePath)
	{
		// If the base path is not null, remove it from the value

		filePath = filePath.TrimStart('/');

		if (!string.IsNullOrEmpty(basePath)
			&& filePath.StartsWith(basePath, StringComparison.InvariantCulture))
		{
			filePath = filePath[basePath.Length..].TrimStart('/');
		}

		// Get the absolute path (URL) to the file

		return urlHelper.Content(
			$"~/{(string.IsNullOrEmpty(basePath) ? string.Empty : $"{basePath}/")}{filePath}"
		);
	}
}
