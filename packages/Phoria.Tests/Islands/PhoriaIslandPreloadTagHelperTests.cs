using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Vite;
using Xunit;
using static Phoria.Tests.TestUtilities.GoldenManifestFixture;

namespace Phoria.Tests.Islands;

public class PhoriaIslandPreloadTagHelperTests
{
	[Fact]
	public void Process_ProductionMode_EmitsModulepreloadLinks()
	{
		// Arrange
		IViteSsrManifest manifest = LoadGoldenManifest();

		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);

		var scopedContext = new StubScopedContext(
			new PhoriaIsland
			{
				ComponentName = "ReactCounter",
				ComponentPath = "/src/components/Counter/Counter.tsx",
				Framework = "react"
			});

		var manifestReader = new StubManifestReader(manifest);

		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });

		var urlHelper = new StubUrlHelper();
		var urlHelperFactory = new StubUrlHelperFactory(urlHelper);

		var tagHelper = new PhoriaIslandPreloadTagHelper(
			serverMonitor,
			scopedContext,
			manifestReader,
			options,
			urlHelperFactory);

		tagHelper.ViewContext = new ViewContext();

		var context = new TagHelperContext(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

		var output = new TagHelperOutput(
			"phoria-island-preload",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

		// Act
		tagHelper.Process(context, output);

		// Assert
		string html = output.Content.GetContent();

		// Counter.tsx maps to ["/ui/assets/Counter-D9HeyafA.js", "/ui/assets/Counter-DXes5fBr.css"]
		// For .js files: <link rel="modulepreload" crossorigin href="...">
		// For .css files: <link rel="stylesheet" href="...">
		Assert.Contains("rel=\"modulepreload\"", html);
		Assert.Contains("Counter-D9HeyafA.js", html);
		Assert.Contains("Counter-DXes5fBr.css", html);
		Assert.Contains("rel=\"stylesheet\"", html);
	}

	[Fact]
	public void Process_DevMode_DoesNotEmitAnything()
	{
		// Arrange
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Development);

		var scopedContext = new StubScopedContext(
			new PhoriaIsland
			{
				ComponentName = "ReactCounter",
				ComponentPath = "/src/components/Counter/Counter.tsx",
				Framework = "react"
			});

		var manifest = LoadGoldenManifest();
		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions());
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandPreloadTagHelper(
			serverMonitor,
			scopedContext,
			manifestReader,
			options,
			urlHelperFactory);

		tagHelper.ViewContext = new ViewContext();

		var context = new TagHelperContext(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

		var output = new TagHelperOutput(
			"phoria-island-preload",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

		// Act
		tagHelper.Process(context, output);

		// Assert
		string html = output.Content.GetContent();
		Assert.DoesNotContain("modulepreload", html);
	}

	[Fact]
	public void Process_EmptyIslands_DoesNotEmitAnything()
	{
		// Arrange
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var scopedContext = new StubScopedContext();
		var manifest = LoadGoldenManifest();
		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions());
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandPreloadTagHelper(
			serverMonitor,
			scopedContext,
			manifestReader,
			options,
			urlHelperFactory);

		tagHelper.ViewContext = new ViewContext();

		var context = new TagHelperContext(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

		var output = new TagHelperOutput(
			"phoria-island-preload",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

		// Act
		tagHelper.Process(context, output);

		// Assert
		string html = output.Content.GetContent();
		Assert.DoesNotContain("modulepreload", html);
	}

	[Fact]
	public void Process_NoDuplicates_EmitsEachFileOnce()
	{
		// Arrange - two islands mapping to the same component path
		IViteSsrManifest manifest = LoadGoldenManifest();

		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);

		var scopedContext = new StubScopedContext(
			new PhoriaIsland
			{
				ComponentName = "ReactCounter1",
				ComponentPath = "/src/components/Counter/Counter.tsx",
				Framework = "react"
			},
			new PhoriaIsland
			{
				ComponentName = "ReactCounter2",
				ComponentPath = "/src/components/Counter/Counter.tsx",
				Framework = "react"
			});

		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandPreloadTagHelper(
			serverMonitor,
			scopedContext,
			manifestReader,
			options,
			urlHelperFactory);

		tagHelper.ViewContext = new ViewContext();

		var context = new TagHelperContext(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

		var output = new TagHelperOutput(
			"phoria-island-preload",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

		// Act
		tagHelper.Process(context, output);

		// Assert - each file should appear exactly once (no duplicates)
		string html = output.Content.GetContent();
		int modulepreloadCount = CountOccurrences(html, "rel=\"modulepreload\"");
		int stylesheetCount = CountOccurrences(html, "rel=\"stylesheet\"");
		Assert.Equal(1, modulepreloadCount); // Counter-D9HeyafA.js
		Assert.Equal(1, stylesheetCount);    // Counter-DXes5fBr.css
	}

	[Fact]
	public void Process_SvelteComponent_EmitsCorrectLinks()
	{
		// Arrange
		IViteSsrManifest manifest = LoadGoldenManifest();

		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);

		var scopedContext = new StubScopedContext(
			new PhoriaIsland
			{
				ComponentName = "SvelteCounter",
				ComponentPath = "/src/components/Counter/Counter.svelte",
				Framework = "svelte"
			});

		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandPreloadTagHelper(
			serverMonitor,
			scopedContext,
			manifestReader,
			options,
			urlHelperFactory);

		tagHelper.ViewContext = new ViewContext();

		var context = new TagHelperContext(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

		var output = new TagHelperOutput(
			"phoria-island-preload",
			new TagHelperAttributeList(),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

		// Act
		tagHelper.Process(context, output);

		// Assert
		string html = output.Content.GetContent();
		Assert.Contains("Counter-RVJx8xKX.js", html);
		Assert.Contains("Counter-Bz3VW5_o.css", html);
	}

	private static int CountOccurrences(string text, string substring)
	{
		int count = 0;
		int index = 0;
		while ((index = text.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
		{
			count++;
			index += substring.Length;
		}
		return count;
	}

	// --- Stubs ---

	private sealed class StubServerMonitor(PhoriaServerMode mode) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Mode = mode,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}

	private sealed class StubScopedContext(params PhoriaIsland[] islands) : IPhoriaIslandScopedContext
	{
		public IReadOnlyList<PhoriaIsland> Islands { get; } = islands;
		public void AddIsland(PhoriaIsland island) { }
	}

	private sealed class StubManifestReader(IViteSsrManifest manifest) : IViteSsrManifestReader
	{
		public IViteSsrManifest ReadSsrManifest() => manifest;
	}

	private sealed class StubUrlHelper : IUrlHelper
	{
		public ActionContext ActionContext => null!;

		public string? ActionName => null;

		public RouteValueDictionary? RouteValues => null;

		public string? RequestScheme => "http";

		public bool IsLocalUrl(string? path) => true;

		public string? Content(string? contentPath)
		{
			// Mimic ASP.NET Core UrlHelper.Content: strip ~/
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

	private sealed class StubUrlHelperFactory(IUrlHelper urlHelper) : IUrlHelperFactory
	{
		public IUrlHelper GetUrlHelper(ActionContext context) => urlHelper;
	}
}
