using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Phoria.Islands;
using Phoria.Server;
using Phoria.Vite;
using Xunit;

namespace Phoria.Tests.Islands;

public class PhoriaIslandEntryTagHelperTests
{
	[Fact]
	public void Process_LinkWithoutCssChunks_LogsWarningAndSuppressesOutput()
	{
		// Arrange
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			serverMonitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaHref = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = CreateTagHelperContext();
		TagHelperOutput output = CreateTagHelperOutput("link", new TagHelperAttribute("rel", "stylesheet"));

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Contains(logger.Messages, message => message.Contains("doesn't have CSS chunks", StringComparison.Ordinal));
		Assert.Empty(output.Content.GetContent());
	}

	[Fact]
	public void Process_LinkWithCssChunks_RendersStylesheetLink()
	{
		// Arrange
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js", Css = ["assets/entry.css"] }
		});
		var manifestReader = new StubManifestReader(manifest);
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			serverMonitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaHref = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = CreateTagHelperContext();
		TagHelperOutput output = CreateTagHelperOutput("link", new TagHelperAttribute("rel", "stylesheet"));

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Equal("/ui/assets/entry.css", output.Attributes["href"]?.Value.ToString());
	}

	[Fact]
	public void Process_EntryStylesWithoutCssChunks_DoesNotLogWarning()
	{
		// Arrange
		var logger = new ListLogger<PhoriaIslandEntryStylesTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var serverMonitor = new StubServerMonitor(PhoriaServerMode.Production);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui", Entry = "src/entry.ts" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryStylesTagHelper(
			logger,
			manifestReader,
			serverMonitor,
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory);
		tagHelper.ViewContext = new ViewContext();

		TagHelperContext context = CreateTagHelperContext();
		TagHelperOutput output = CreateTagHelperOutput("phoria-island-styles");

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.DoesNotContain("doesn't have CSS chunks", logger.Messages);
		Assert.Empty(output.Content.GetContent());
	}

	[Fact]
	public void Process_ServerUnhealthy_SuppressesOutputAndLogsWarning()
	{
		var logger = new ListLogger<PhoriaIslandEntryTagHelper>();
		var manifest = new ViteManifest(new Dictionary<string, ViteChunk>
		{
			["src/entry.ts"] = new() { File = "assets/entry.js" }
		});
		var manifestReader = new StubManifestReader(manifest);
		var options = Options.Create(new PhoriaOptions { Root = "ui", Base = "/ui" });
		var urlHelperFactory = new StubUrlHelperFactory(new StubUrlHelper());

		var tagHelper = new PhoriaIslandEntryTagHelper(
			logger,
			manifestReader,
			new UnhealthyServerMonitor(),
			new PhoriaIslandEntryTagHelperMonitor(),
			options,
			urlHelperFactory)
		{
			PhoriaSrc = "src/entry.ts"
		};
		tagHelper.ViewContext = new ViewContext { View = new StubView() };

		TagHelperContext context = CreateTagHelperContext();
		TagHelperOutput output = CreateTagHelperOutput("script");

		// Act
		tagHelper.Process(context, output);

		// Assert
		Assert.Contains(logger.Messages, message => message.Contains("suppressing", StringComparison.Ordinal));
		Assert.Empty(output.Content.GetContent());
		Assert.Null(output.Attributes["src"]);
	}

	private static TagHelperContext CreateTagHelperContext() =>
		new(
			new TagHelperAttributeList(),
			new Dictionary<object, object?>(),
			Guid.NewGuid().ToString("N"));

	private static TagHelperOutput CreateTagHelperOutput(string tagName, params TagHelperAttribute[] attributes) =>
		new(
			tagName,
			new TagHelperAttributeList(attributes),
			(childContent, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

	// --- Stubs ---

	private sealed class ListLogger<T>(IList<string>? messages = null) : ILogger<T>
	{
		private readonly IList<string> messages = messages ?? new List<string>();

		public IList<string> Messages => messages;

		public IDisposable? BeginScope<TState>(TState state)
			where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			messages.Add(formatter(state, exception));
		}
	}

	private sealed class StubServerMonitor(PhoriaServerMode mode) : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Healthy,
			Mode = mode,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}

	private sealed class UnhealthyServerMonitor : IPhoriaServerMonitor
	{
		public PhoriaServerStatus ServerStatus { get; } = new()
		{
			Health = PhoriaServerHealth.Unhealthy,
			Url = "http://localhost:5173"
		};

		public Task StartMonitoring(CancellationToken cancellationToken) => Task.CompletedTask;
		public Task StopMonitoring() => Task.CompletedTask;
	}

	private sealed class StubView : IView
	{
		public string Path => "test";

		public Task RenderAsync(ViewContext context) => Task.CompletedTask;
	}

	private sealed class StubManifestReader(IViteManifest manifest) : IViteManifestReader
	{
		public IViteManifest ReadManifest() => manifest;
	}

	private sealed class StubUrlHelper : IUrlHelper
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

	private sealed class StubUrlHelperFactory(IUrlHelper urlHelper) : IUrlHelperFactory
	{
		public IUrlHelper GetUrlHelper(ActionContext context) => urlHelper;
	}
}
