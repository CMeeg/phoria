using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Phoria.Server;
using Xunit;

namespace Phoria.Tests.Server;

public class ViteDevServerHmrProxyTests
{
	[Fact]
	public async Task ProxyAsync_WhenTargetCannotBeReached_LogsAndCompletes()
	{
		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
		services.AddLogging();
		services.AddPhoria();
		services.Configure<PhoriaOptions>(options => options.Server.Port = 1);

		using var provider = services.BuildServiceProvider();
		var proxy = provider.GetRequiredService<IViteDevServerHmrProxy>();

		var context = new DefaultHttpContext();
		context.Request.Scheme = "http";
		context.Request.Host = new HostString("localhost");

		await proxy.ProxyAsync(context, CancellationToken.None);

	}
}
