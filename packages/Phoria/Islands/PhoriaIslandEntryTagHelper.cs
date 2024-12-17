// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Logging;
using Phoria.Server;
using Phoria.Vite;

namespace Phoria.Islands;

/// <summary>
/// This tag helper is used to replace the phoria-src and phoria-href attributes with the correct client entry file path according to the Vite manifest.
/// </summary>
/// <param name="logger">An ILogger instance.</param>
/// <param name="manifestReader">The Vite manifest reader.</param>
/// <param name="serverMonitor">The Phoria server monitor.</param>
/// <param name="tagHelperMonitor">The tag helper monitor.</param>
/// <param name="options">Phoria options.</param>
/// <param name="urlHelperFactory">Url helper factory to build the file path.</param>
[HtmlTargetElement(ScriptTag, Attributes = PhoriaSrcAttribute)]
[HtmlTargetElement(LinkTag, Attributes = PhoriaHrefAttribute)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class PhoriaIslandEntryTagHelper(
	ILogger<PhoriaIslandEntryTagHelper> logger,
	IViteManifestReader manifestReader,
	IPhoriaServerMonitor serverMonitor,
	PhoriaIslandEntryTagHelperMonitor tagHelperMonitor,
	IOptions<PhoriaOptions> options,
	IUrlHelperFactory urlHelperFactory)
	: TagHelper
{
	private static readonly Regex scriptRegex =
		new(@"\.(js|ts|jsx|tsx|cjs|cts|mjs|mts)$", RegexOptions.Compiled);

	private const string ScriptTag = "script";
	private const string LinkTag = "link";
	private const string IdAttribute = "href";
	private const string SrcAttribute = "src";
	private const string HrefAttribute = "href";
	private const string PhoriaSrcAttribute = "phoria-src";
	private const string PhoriaHrefAttribute = "phoria-href";
	private const string LinkRelAttribute = "rel";
	private const string LinkRelStylesheet = "stylesheet";
	private const string LinkAsAttribute = "as";
	private const string LinkAsStyle = "style";

	private readonly ILogger<PhoriaIslandEntryTagHelper> logger = logger;
	private readonly IViteManifestReader manifestReader = manifestReader;
	private readonly IPhoriaServerMonitor serverMonitor = serverMonitor;
	private readonly PhoriaIslandEntryTagHelperMonitor tagHelperMonitor = tagHelperMonitor;
	private readonly PhoriaOptions options = options.Value;
	private readonly IUrlHelperFactory urlHelperFactory = urlHelperFactory;

	/// <summary>
	/// The path to your Phoria client entry file.
	/// </summary>
	[HtmlAttributeName(PhoriaSrcAttribute)]
	public string? PhoriaSrc { get; set; }

	/// <summary>
	/// The path to your Phoria client entry file.
	/// </summary>
	[HtmlAttributeName(PhoriaHrefAttribute)]
	public string? PhoriaHref { get; set; }

	/// <inheritdoc />
	[ViewContext]
	[HtmlAttributeNotBound]
	public ViewContext ViewContext { get; set; } = default!;

	// Set the Order property to int.MinValue to ensure this tag helper is executed before any other tag helpers with a higher Order value
	public override int Order => int.MinValue;

	/// <inheritdoc />
	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		string tagName = output.TagName.ToLowerInvariant();

		(string attribute, string? value) = tagName switch
		{
			ScriptTag => (attribute: SrcAttribute, value: PhoriaSrc),
			LinkTag => (attribute: HrefAttribute, value: PhoriaHref),
			_ => throw new NotImplementedException("Attribute is only valid on script and link tags.")
		};

		// Remove the Phoria attribute from the output

		output.Attributes.RemoveAll($"phoria-{attribute}");

		// If the value is empty or null, we don't need to do anything

		if (string.IsNullOrWhiteSpace(value))
		{
			logger.LogEntryAttributeMissing(attribute, ViewContext.View.Path);
			return;
		}

		// Removes the leading '~/' from the value. This is needed because the manifest file doesn't contain the leading '~/' or '/'.
		value = value.TrimStart('~', '/');

		// If the base path is not null, remove it from the value

		string? basePath = options.Base?.Trim('/');

		if (!string.IsNullOrEmpty(basePath)
			&& value.StartsWith(basePath, StringComparison.InvariantCulture))
		{
			value = value[basePath.Length..].TrimStart('/');
		}

		string file;

		// TODO: When the server was unhealthy in prod this was spitting out the original atttribute value, but we prob want to suppress output in that case
		// TODO: Actually, it was spitting out the vite/client script tag also so not sure what was going on! It was caused by the path to the server js being an invalid path, and the server was returning "connection refused"

		// If the server is running in development mode, don't load the files from the manifest

		if (serverMonitor.ServerStatus.Mode == PhoriaServerMode.Development)
		{
			// If the tagName is a link and the file is a script, destroy the element

			if (tagName == LinkTag && scriptRegex.IsMatch(value))
			{
				output.SuppressOutput();
				return;
			}

			string serverUrl = options.GetServerUrlWithBasePath();

			// If the Vite client script was not inserted, it will be prepended to the current element tag

			// TODO: Don't render the client script if no components in scoped context

			if (!tagHelperMonitor.IsViteClientScriptInjected)
			{
				string viteClientUrl = $"{serverUrl}/@vite/client";

				// Add the script tag to the output

				if (serverMonitor.ServerStatus.Frameworks.Contains(
					"react",
					StringComparer.InvariantCultureIgnoreCase))
				{
					string viteReactRefreshUrl = $"{serverUrl}/@react-refresh";

					var script = new StringBuilder();
					script.AppendLine("<script type=\"module\">");
					script.AppendLine(CultureInfo.InvariantCulture, $"import RefreshRuntime from \"{viteReactRefreshUrl}\";");
					script.AppendLine("RefreshRuntime.injectIntoGlobalHook(window);");
					script.AppendLine("window.$RefreshReg$ = () => { };");
					script.AppendLine("window.$RefreshSig$ = () => (type) => type;");
					script.AppendLine("window.__vite_plugin_react_preamble_installed__ = true;");
					script.AppendLine("</script>");

					output.PreElement.AppendHtml(script.ToString());
				}

				output.PreElement.AppendHtml(
					$"<script type=\"module\" src=\"{viteClientUrl}\"></script>"
				);

				// Set the flag to true to avoid adding the script tag multiple times

				tagHelperMonitor.IsViteClientScriptInjected = true;
			}

			// Build the url to the file path

			file = $"{serverUrl}/{value}";
		}
		else
		{
			// TODO: Check that this output includes all recommended output as described in step 4 of https://vite.dev/guide/backend-integration

			// Get the entry chunk from the manifest file

			IViteManifest manifest = manifestReader.ReadManifest();

			// If the entry is not found, log an error and return

			if (!manifest.ContainsKey(value))
			{
				logger.LogViteManifestKeyNotFound(value, ViewContext.View.Path);
				output.SuppressOutput();
				return;
			}

			// If the entry name looks like a script and the tagName is a 'link' of kind 'stylesheet', render the css file

			var urlHelper = new PhoriaIslandUrlHelper(
				urlHelperFactory.GetUrlHelper(ViewContext),
				options);

			string? relAttr = output.Attributes[LinkRelAttribute]?.Value.ToString();
			string? asAttr = output.Attributes[LinkAsAttribute]?.Value.ToString();

			if (tagName == LinkTag
				&& (relAttr == LinkRelStylesheet || asAttr == LinkAsStyle)
				&& scriptRegex.IsMatch(value))
			{
				// Get css files from the entry chunk

				IEnumerable<string>? cssFiles = manifest.GetRecursiveCssFiles(value).Reverse();

				int count = cssFiles?.Count() ?? 0;

				// If the chunk doesn't have css files, destroy it

				if (count == 0)
				{
					logger.LogManifestEntryDoesntHaveCssChunks(value);
					output.SuppressOutput();
					return;
				}

				// Get the file path from the manifest

				file = urlHelper.GetContentUrl(cssFiles!.First());

				// If there are more than one css files, create clones of the element keeping all attributes

				if (count > 1)
				{
					cssFiles = cssFiles!.Skip(1).Reverse();

					var sharedAttributes = new TagHelperAttributeList(output.Attributes);

					// If the attribute 'id' exists, remove it, otherwise it will be duplicated
					TagHelperAttribute idAttr = sharedAttributes[IdAttribute];
					if (idAttr != null)
					{
						sharedAttributes.Remove(idAttr);
					}

					foreach (string? cssFile in cssFiles)
					{
						// Get the file path from the manifest

						string filePath = urlHelper.GetContentUrl(cssFile);

						var linkOutput = new TagHelperOutput(
							LinkTag,
							[.. sharedAttributes],
							(useCachedResult, encoder) =>
								Task.Factory.StartNew<TagHelperContent>(
									() => new DefaultTagHelperContent()
								)
						);

						linkOutput.Attributes.SetAttribute(HrefAttribute, filePath);

						output.PreElement.AppendHtml(linkOutput);
					}
				}
			}
			else
			{
				// Script file

				IViteChunk entry = manifest[value]!;

				file = urlHelper.GetContentUrl(entry.File);
			}
		}

		// Set the entry attribute

		output.Attributes.SetAttribute(
			new TagHelperAttribute(attribute, file, HtmlAttributeValueStyle.DoubleQuotes)
		);
	}
}

/// <summary>
/// Service used by the PhoriaIslandEntryTagHelper to track the injection of the Vite client script when the server is in development mode.
/// </summary>
public class PhoriaIslandEntryTagHelperMonitor
{
	/// <summary>
	/// True if the Vite client script has been injected.
	/// </summary>
	public bool IsViteClientScriptInjected { get; set; }
}

internal static partial class PhoriaIslandEntryTagHelperLogMessages
{
	[LoggerMessage(
		EventId = EventFeature.Islands + 1,
		Message = "entry-{Attribute} value missing (check {View})",
		Level = LogLevel.Warning)]
	internal static partial void LogEntryAttributeMissing(
		this ILogger logger,
		string attribute,
		string view);

	[LoggerMessage(
		EventId = EventFeature.Islands + 2,
		Message = "'{Key}' was not found in Vite manifest file (check {View})",
		Level = LogLevel.Error)]
	internal static partial void LogViteManifestKeyNotFound(
		this ILogger logger,
		string key,
		string view);

	[LoggerMessage(
		EventId = EventFeature.Islands + 3,
		Message = "The entry '{Entry}' doesn't have CSS chunks",
		Level = LogLevel.Warning)]
	internal static partial void LogManifestEntryDoesntHaveCssChunks(this ILogger logger, string entry);
}
