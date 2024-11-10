// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.ComponentModel;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Vite;

[assembly: MetadataUpdateHandler(typeof(PhoriaIslandsEntryTagHelperMonitor))]

namespace Phoria.Islands;

[HtmlTargetElement(ScriptTag, Attributes = PhoriaSrcAttribute)]
[HtmlTargetElement(LinkTag, Attributes = PhoriaHrefAttribute)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class PhoriaIslandsEntryTagHelper
	: TagHelper
{
	private static readonly Regex scriptRegex =
		new(@"\.(js|ts|jsx|tsx|cjs|cts|mjs|mts)$", RegexOptions.Compiled);

	private const string ScriptTag = "script";
	private const string LinkTag = "link";
	private const string SrcAttribute = "src";
	private const string HrefAttribute = "href";
	private const string PhoriaSrcAttribute = "phoria-src";
	private const string PhoriaHrefAttribute = "phoria-href";
	private const string LinkRelAttribute = "rel";
	private const string LinkRelStylesheet = "stylesheet";
	private const string LinkAsAttribute = "stylesheet";
	private const string LinkAsStyle = "style";

	private readonly ILogger<PhoriaIslandsEntryTagHelper> logger;
	private readonly IViteManifestReader manifestReader;
	private readonly IPhoriaServerMonitor serverMonitor;
	private readonly PhoriaIslandsEntryTagHelperMonitor tagHelperMonitor;
	private readonly PhoriaOptions options;
	private readonly IUrlHelperFactory urlHelperFactory;

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

	/// <summary>
	/// The rel attribute for the link tag.
	/// </summary>
	[HtmlAttributeName(LinkRelAttribute)]
	public string? Rel { get; set; }

	/// <summary>
	/// The as attribute for the link tag.
	/// </summary>
	[HtmlAttributeName(LinkAsAttribute)]
	public string? As { get; set; }

	/// <inheritdoc />
	[ViewContext]
	[HtmlAttributeNotBound]
	public ViewContext ViewContext { get; set; } = default!;

	// Set the Order property to int.MinValue to ensure this tag helper is executed before any other tag helpers with a higher Order value
	public override int Order => int.MinValue;

	/// <summary>
	/// This tag helper is used to replace the phoria-src and phoria-href attributes with the correct client entry file path according to the Vite manifest.
	/// </summary>
	/// <param name="logger">An ILogger instance.</param>
	/// <param name="manifestReader">The Vite manifest reader.</param>
	/// <param name="serverMonitor">The Phoria server monitor.</param>
	/// <param name="tagHelperMonitor">The tag helper monitor.</param>
	/// <param name="options">Phoria options.</param>
	/// <param name="urlHelperFactory">Url helper factory to build the file path.</param>
	public PhoriaIslandsEntryTagHelper(
		ILogger<PhoriaIslandsEntryTagHelper> logger,
		IViteManifestReader manifestReader,
		IPhoriaServerMonitor serverMonitor,
		PhoriaIslandsEntryTagHelperMonitor tagHelperMonitor,
		IOptions<PhoriaOptions> options,
		IUrlHelperFactory urlHelperFactory)
	{
		this.logger = logger;
		this.manifestReader = manifestReader;
		this.serverMonitor = serverMonitor;
		this.tagHelperMonitor = tagHelperMonitor;
		this.options = options.Value;
		this.urlHelperFactory = urlHelperFactory;
	}

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

		IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
		string file;

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

			IViteChunk? entry = manifest[value];

			// If the entry is not found, log an error and return

			if (entry == null)
			{
				logger.LogViteManifestKeyNotFound(value, ViewContext.View.Path);
				output.SuppressOutput();
				return;
			}

			// If the entry name looks like a script and the tagName is a 'link' of kind 'stylesheet', render the css file

			if (tagName == LinkTag
				&& (Rel == LinkRelStylesheet || As == LinkAsStyle)
				&& scriptRegex.IsMatch(value))
			{
				// Get the styles from the entry

				IEnumerable<string>? cssFiles = entry.Css;

				int count = cssFiles?.Count() ?? 0;

				// If the entrypoint doesn't have css files, destroy it

				if (count == 0)
				{
					logger.LogManifestEntryDoesntHaveCssChunks(value);
					output.SuppressOutput();
					return;
				}

				// TODO: Handle multiple css files

				file = GetAbsoluteFilePath(urlHelper, basePath, cssFiles!.First());
			}
			else
			{
				// Script file

				file = GetAbsoluteFilePath(urlHelper, basePath, entry.File);
			}
		}

		// Forwards the rel attribute to the output

		if (!string.IsNullOrEmpty(Rel))
		{
			output.Attributes.SetAttribute(new TagHelperAttribute(LinkRelAttribute, Rel));
		}

		// Forwards the as attribute to the output

		if (!string.IsNullOrEmpty(As))
		{
			output.Attributes.SetAttribute(new TagHelperAttribute(LinkAsAttribute, As));
		}

		// Set the entry attribute

		output.Attributes.SetAttribute(
			new TagHelperAttribute(attribute, file, HtmlAttributeValueStyle.DoubleQuotes)
		);
	}

	private static string GetAbsoluteFilePath(
		IUrlHelper urlHelper,
		string? basePath,
		string filePath)
	{
		// If the base path is not null, remove it from the value

		if (!string.IsNullOrEmpty(basePath)
			&& filePath.StartsWith(basePath, StringComparison.InvariantCulture))
		{
			filePath = filePath[basePath.Length..].TrimStart('/');
		}

		// Get the absoulte path to the manifest file

		return urlHelper.Content(
			$"~/{(string.IsNullOrEmpty(basePath) ? string.Empty : $"{basePath}/")}{filePath}"
		);
	}
}

/// <summary>
/// Service used by the PhoriaIslandsEntryTagHelper to track the injection of the Vite client script when the server is in development mode.
/// </summary>
public class PhoriaIslandsEntryTagHelperMonitor
{
	/// <summary>
	/// True if the Vite client script has been injected.
	/// </summary>
	public bool IsViteClientScriptInjected { get; set; }
}

internal static partial class PhoriaIslandsEntryTagHelperLogMessages
{
	[LoggerMessage(
		EventId = 1101,
		Message = "entry-{Attribute} value missing (check {View})",
		Level = LogLevel.Warning)]
	internal static partial void LogEntryAttributeMissing(
		this ILogger logger,
		string attribute,
		string view);

	[LoggerMessage(
		EventId = 1102,
		Message = "'{Key}' was not found in Vite manifest file (check {View})",
		Level = LogLevel.Error)]
	internal static partial void LogViteManifestKeyNotFound(
		this ILogger logger,
		string key,
		string view);

	[LoggerMessage(
		EventId = 1103,
		Message = "The entry '{Entry}' doesn't have CSS chunks",
		Level = LogLevel.Warning)]
	internal static partial void LogManifestEntryDoesntHaveCssChunks(this ILogger logger, string entry);
}
