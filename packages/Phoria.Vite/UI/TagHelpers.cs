// Copyright (c) 2024 Quetzal Rivera.
// Licensed under the MIT License, See LICENCE in the project root for license information.

using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Vite.DevServer;
using Phoria.Vite.Logging;
using Phoria.Vite.Manifest;

namespace Phoria.Vite.UI;

/// <summary>
/// This tag helper is used to replace the vite-src and vite-href attributes with the correct file path according to the entry in the "manifest.json" file.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="helperService">The ViteTagHelperService.</param>
/// <param name="manifest">The manifest service.</param>
/// <param name="devServerStatus">The Vite development server status.</param>
/// <param name="urlHelperFactory">Url helper factory to build the file path.</param>
[HtmlTargetElement("script", Attributes = VITE_SRC_ATTRIBUTE)]
[HtmlTargetElement("link", Attributes = VITE_HREF_ATTRIBUTE)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class ViteTagHelper(
	ILogger<ViteTagHelper> logger,
	IViteManifest manifest,
	IViteDevServerStatus devServerStatus,
	ViteTagHelperMonitor helperService,
	IOptions<ViteOptions> viteOptions,
	IUrlHelperFactory urlHelperFactory
) : TagHelper
{
	private static readonly Regex ScriptRegex =
		new(@"\.(js|ts|jsx|tsx|cjs|cts|mjs|mts)$", RegexOptions.Compiled);

	private const string VITE_HREF_ATTRIBUTE = "vite-href";
	private const string VITE_SRC_ATTRIBUTE = "vite-src";
	private const string LINK_AS_ATTRIBUTE = "stylesheet";
	private const string LINK_AS_STYLE = "style";
	private const string LINK_REL_ATTRIBUTE = "rel";
	private const string LINK_REL_STYLESHEET = "stylesheet";

	private readonly ILogger<ViteTagHelper> logger = logger;
	private readonly ViteTagHelperMonitor helperService = helperService;
	private readonly IViteManifest manifest = manifest;
	private readonly IViteDevServerStatus devServerStatus = devServerStatus;
	private readonly IUrlHelperFactory urlHelperFactory = urlHelperFactory;
	private readonly string? basePath = viteOptions.Value.Base?.Trim('/');

	private readonly bool useReactRefresh = viteOptions.Value.Server.UseReactRefresh ?? false;

	/// <summary>
	/// The entry name in the manifest file.
	/// The manifest can only be accessed after building the assets with 'npm run build'.
	/// </summary>
	[HtmlAttributeName(VITE_SRC_ATTRIBUTE)]
	public string? ViteSrc { get; set; }

	/// <summary>
	/// The entry name in the manifest file.
	/// The manifest can only be accessed after building the assets with 'npm run build'.
	/// </summary>
	[HtmlAttributeName(VITE_HREF_ATTRIBUTE)]
	public string? ViteHref { get; set; }

	/// <summary>
	/// The rel attribute for the link tag.
	/// </summary>
	[HtmlAttributeName(LINK_REL_ATTRIBUTE)]
	public string? Rel { get; set; }

	/// <summary>
	/// The as attribute for the link tag.
	/// </summary>
	[HtmlAttributeName(LINK_AS_ATTRIBUTE)]
	public string? As { get; set; }

	/// <inheritdoc />
	[ViewContext]
	[HtmlAttributeNotBound]
	public ViewContext ViewContext { get; set; } = default!;

	// Set the Order property to int.MinValue to ensure this tag helper is executed before any other tag helpers with a higher Order value
	public override int Order => int.MinValue;

	/// <inheritdoc />
	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		// because some people might shout their tag names like SCRIPT and LINK!
		string tagName = output.TagName.ToLowerInvariant();

		(string attribute, string? value) = tagName switch
		{
			"script" => (attribute: "src", value: ViteSrc),
			"link" => (attribute: "href", value: ViteHref),
			_ => throw new NotImplementedException("This case should never happen")
		};

		// Remove the vite attribute from the output
		output.Attributes.RemoveAll($"vite-{attribute}");

		// If the value is empty or null, we don't need to do anything
		if (string.IsNullOrWhiteSpace(value))
		{
			logger.LogViteAttributeMissing(attribute, ViewContext.View.Path);
			return;
		}

		// Removes the leading '~/' from the value. This is needed because the manifest file doesn't contain the leading '~/' or '/'.
		value = value.TrimStart('~', '/');
		// If the base path is not null, remove it from the value.
		if (
			!string.IsNullOrEmpty(basePath)
			&& value.StartsWith(basePath, StringComparison.InvariantCulture)
		)
		{
			value = value[basePath.Length..].TrimStart('/');
		}

		Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper = urlHelperFactory.GetUrlHelper(ViewContext);
		string file;

		// If the Vite development server is enabled, don't load the files from the manifest.
		if (devServerStatus.IsEnabled)
		{
			// If the tagName is a link and the file is a script, destroy the element.
			if (tagName == "link" && ScriptRegex.IsMatch(value))
			{
				output.SuppressOutput();
				return;
			}

			string devBasePath = devServerStatus.ServerUrlWithBasePath;

			// If the Vite script was not inserted, it will be prepended to the current element tag.
			if (!helperService.IsDevScriptInjected)
			{
				string viteClientUrl = devBasePath + "/@vite/client";

				// Add the script tag to the output

				if (useReactRefresh)
				{
					string viteReactRefreshUrl =
						devBasePath + "/@react-refresh";

					output.PreElement.AppendHtml(
						"<script type=\"module\">\n"
							+ "    import RefreshRuntime from \""
							+ viteReactRefreshUrl
							+ "\";\n"
							+ "    RefreshRuntime.injectIntoGlobalHook(window);\n"
							+ "    window.$RefreshReg$ = () => { };\n"
							+ "    window.$RefreshSig$ = () => (type) => type;\n"
							+ "    window.__vite_plugin_react_preamble_installed__ = true;\n"
							+ "</script>\n"
					);
				}

				output.PreElement.AppendHtml(
					$"<script type=\"module\" src=\"{viteClientUrl}\"></script>"
				);

				// Set the flag to true to avoid adding the script tag multiple times
				helperService.IsDevScriptInjected = true;
			}
			// Build the url to the file path.
			file = $"{devBasePath}/{value}";
		}
		else
		{
			// Get the entry chunk from the 'manifest.json' file.
			IViteChunk? entry = manifest[value];

			// If the entry is not found, log an error and return
			if (entry == null)
			{
				logger.LogViteManifestKeyNotFound(value, ViewContext.View.Path);
				output.SuppressOutput();
				return;
			}

			// If the entry name looks like a script and the tagName is a 'link' of kind 'stylesheet', render the css file.
			if (
				tagName == "link"
				&& (Rel == LINK_REL_STYLESHEET || As == LINK_AS_STYLE)
				&& ScriptRegex.IsMatch(value)
			)
			{
				// Get the styles from the entry
				IEnumerable<string>? cssFiles = entry.Css;
				// Get the number of styles
				int count = cssFiles?.Count() ?? 0;
				// If the entrypoint doesn't have css files, destroy it.
				if (count == 0)
				{
					logger.LogEntryDoesntHaveCssChunks(value);
					output.SuppressOutput();
					return;
				}

				string filePath = cssFiles!.First();
				// If the base path is not null, remove it from the value.
				if (
					!string.IsNullOrEmpty(basePath)
					&& filePath.StartsWith(basePath, StringComparison.InvariantCulture)
				)
				{
					filePath = filePath[basePath.Length..].TrimStart('/');
				}

				// Get the file path from the 'manifest.json' file
				file = urlHelper.Content(
					$"~/{(string.IsNullOrEmpty(basePath) ? string.Empty : $"{basePath}/")}{filePath}"
				);

				// TODO: Handle multiple css files
			}
			else
			{
				// TODO: Put this into a method as it's duplicated code
				string filePath = entry.File;
				// If the base path is not null, remove it from the value.
				if (
					!string.IsNullOrEmpty(basePath)
					&& filePath.StartsWith(basePath, StringComparison.InvariantCulture)
				)
				{
					filePath = filePath[basePath.Length..].TrimStart('/');
				}

				// Get the real file path from the 'manifest.json' file
				file = urlHelper.Content(
					$"~/{(string.IsNullOrEmpty(basePath) ? string.Empty : $"{basePath}/")}{filePath}"
				);
			}
		}

		// Forwards the rel attribute to the output.
		if (!string.IsNullOrEmpty(Rel))
		{
			output.Attributes.SetAttribute(new TagHelperAttribute(LINK_REL_ATTRIBUTE, Rel));
		}
		// Forwards the as attribute to the output.
		if (!string.IsNullOrEmpty(As))
		{
			output.Attributes.SetAttribute(new TagHelperAttribute(LINK_AS_ATTRIBUTE, As));
		}

		// Update the attributes.
		output.Attributes.SetAttribute(
			new TagHelperAttribute(attribute, file, HtmlAttributeValueStyle.DoubleQuotes)
		);
	}
}
