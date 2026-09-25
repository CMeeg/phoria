using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Phoria.Vite;

namespace Phoria.Tests.TestUtilities;

internal sealed class StubManifestReader : IViteManifestReader, IViteSsrManifestReader
{
	private readonly IViteManifest? manifest;
	private readonly IViteSsrManifest? ssrManifest;

	public StubManifestReader(IViteManifest manifest) => this.manifest = manifest;

	public StubManifestReader(IViteSsrManifest ssrManifest) => this.ssrManifest = ssrManifest;

	public IViteManifest ReadManifest() =>
		manifest ?? throw new InvalidOperationException($"{nameof(StubManifestReader)} was not constructed with a client manifest.");

	public IViteSsrManifest ReadSsrManifest() =>
		ssrManifest ?? throw new InvalidOperationException($"{nameof(StubManifestReader)} was not constructed with an SSR manifest.");
}

internal sealed class StubUrlHelper : IUrlHelper
{
	public ActionContext ActionContext => throw new NotSupportedException($"{nameof(StubUrlHelper)} does not support {nameof(ActionContext)}.");

	public string? ActionName => null;

	public RouteValueDictionary? RouteValues => null;

	public string? RequestScheme => "http";

	public bool IsLocalUrl(string? path) => true;

	public string? Content(string? contentPath)
	{
		if (contentPath == null) return null;
		return contentPath.StartsWith("~/") ? contentPath[1..] : contentPath;
	}

	public string? Action(UrlActionContext context) => null;

	public string? RouteUrl(UrlRouteContext context) => null;

	public string? RouteUrl(object? routeValues) => null;

	public string? RouteUrl(string? routeName, object? routeValues) => null;

	public string? RouteUrl(string? routeName, object? routeValues, string? protocol, string? host, string? fragment) => null;

	public string? Link(string? routeName, object? values) => null;

	public string? PageUrl(string? pageName, object? routeValues) => null;

	public string? Page(string? pageName, object? routeValues) => null;

	public string? Page(string? pageName, string? pageHandler, object? routeValues, string? protocol, string? host, string? fragment) => null;

	public string? Action(string? actionName) => null;

	public string? Action(string? actionName, object? routeValues) => null;

	public string? Action(string? actionName, string? controllerName, object? routeValues, string? protocol, string? host, string? fragment) => null;

	public string? Action(string? actionName, string? controllerName, RouteValueDictionary? routeValues, string? protocol, string? host, string? fragment) => null;

	public bool IsValidRouteValue(object? value) => true;

	public string? GetRouteUrl(string? routeName, object? routeValues) => null;

	public string? GetRouteUrl(string? routeName, object? routeValues, string? protocol, string? host, string? fragment) => null;
}

internal sealed class StubUrlHelperFactory(IUrlHelper urlHelper) : IUrlHelperFactory
{
	public IUrlHelper GetUrlHelper(ActionContext context) => urlHelper;
}

internal static class TagHelperTestFactory
{
	public static TagHelperContext CreateTagHelperContext() =>
		new(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

	public static TagHelperOutput CreateTagHelperOutput(string tagName, params TagHelperAttribute[] attributes) =>
		new(
			tagName,
			new TagHelperAttributeList(attributes),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
}

internal sealed class StubWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
{
	public string ApplicationName { get; set; } = "Phoria.Tests";
	public string EnvironmentName { get; set; } = "Production";
	public string ContentRootPath { get; set; } = contentRootPath;
	public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
	public string WebRootPath { get; set; } = contentRootPath;
	public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
}
