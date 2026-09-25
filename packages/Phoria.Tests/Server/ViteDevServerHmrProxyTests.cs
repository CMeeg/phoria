using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class ViteDevServerHmrProxyTests
{
	[Fact]
	public async Task ProxyAsync_WhenTargetCannotBeReached_LogsAndCompletes()
	{
		var loggerProvider = new CapturingLoggerProvider();
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddLogging(logging => logging.AddProvider(loggerProvider));
		services.AddPhoria();
		services.Configure<PhoriaOptions>(options => options.Server.Port = 1);

		using var provider = services.BuildServiceProvider();
		var proxy = provider.GetRequiredService<IViteDevServerHmrProxy>();

		var context = new DefaultHttpContext();
		context.Request.Scheme = "http";
		context.Request.Host = new HostString("localhost");

		await proxy.ProxyAsync(context, CancellationToken.None);

		Assert.Contains(loggerProvider.Messages, message => message.Contains("Failed to establish WebSocket proxy", StringComparison.Ordinal));
	}

	private sealed class CapturingLoggerProvider : ILoggerProvider
	{
		public List<string> Messages { get; } = [];

		public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

		public void Dispose() { }
	}

	private sealed class CapturingLogger(List<string> messages) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			messages.Add(formatter(state, exception));
	}
}
