using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Phoria.Islands;

public class PhoriaIslandPreloadHtmlContent
	: IHtmlContent
{
	private readonly PhoriaIslandUrlHelper urlHelper;
	private readonly string file;

	public PhoriaIslandPreloadHtmlContent(
		PhoriaIslandUrlHelper urlHelper,
		string file)
	{
		this.urlHelper = urlHelper;
		this.file = file;
	}

	public void WriteTo(TextWriter writer, HtmlEncoder encoder)
	{
		string extension = Path.GetExtension(file);

		string html = extension switch
		{
			".js" => $"<link rel=\"modulepreload\" crossorigin href=\"{urlHelper.GetContentUrl(file)}\">",
			".css" => $"<link rel=\"stylesheet\" href=\"{urlHelper.GetContentUrl(file)}\">",
			".woff" => $"<link rel=\"preload\" href=\"{urlHelper.GetContentUrl(file)}\" as=\"font\" type=\"font/woff\" crossorigin>",
			".woff2" => $"<link rel=\"preload\" href=\"{urlHelper.GetContentUrl(file)}\" as=\"font\" type=\"font/woff2\" crossorigin>",
			".gif" => $"<link rel=\"preload\" href=\"{urlHelper.GetContentUrl(file)}\" as=\"image\" type=\"image/gif\">",
			".jpg" or ".jpeg" => $"<link rel=\"preload\" href=\"{urlHelper.GetContentUrl(file)}\" as=\"image\" type=\"image/jpeg\">",
			".png" => $"<link rel=\"preload\" href=\"{urlHelper.GetContentUrl(file)}\" as=\"image\" type=\"image/png\">",
			_ => string.Empty
		};

		if (string.IsNullOrEmpty(html))
		{
			return;
		}

		writer.WriteLine(html);
	}
}
