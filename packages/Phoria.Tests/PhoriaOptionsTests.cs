using Phoria;
using Xunit;

namespace Phoria.Tests;

public class PhoriaOptionsTests
{
	[Fact]
	public void DefaultOptions_MatchJavaScriptDefaults()
	{
		var options = new PhoriaOptions();

		Assert.Equal("ui", options.Root);
		Assert.Equal("/ui", options.Base);
		Assert.Equal("/ssr", options.SsrBase);
		Assert.Equal(string.Empty, options.Entry);
		Assert.Equal(string.Empty, options.SsrEntry);
	}

	[Fact]
	public void DefaultServerOptions_MatchJavaScriptDefaults()
	{
		var options = new PhoriaOptions();

		Assert.Equal("localhost", options.Server.Host);
		Assert.Equal((ushort)5173, options.Server.Port);
		Assert.False(options.Server.Https);
		Assert.Equal(5, options.Server.HealthCheckTimeout);
		Assert.Equal(5, options.Server.HealthCheckInterval);
	}

	[Fact]
	public void DefaultBuildOptions_MatchJavaScriptDefaults()
	{
		var options = new PhoriaOptions();

		Assert.Equal("dist", options.Build.OutDir);
	}

	[Fact]
	public void SectionName_IsPhoria()
	{
		Assert.Equal("Phoria", PhoriaOptions.SectionName);
	}
}
