using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Phoria.Server;
using Phoria.Tests.TestUtilities;
using Xunit;

namespace Phoria.Tests.Diagnostics.HealthChecks;

// The status code a probe sees is produced by the endpoint, not by PhoriaServerHealthCheck, so
// the check-level tests cannot cover it. The example e2e suites only ever probe a healthy server,
// leaving a regression that mapped Unhealthy to 200 invisible to every suite.
public class PhoriaServerHealthCheckEndpointTests
{
	[Fact]
	public async Task Health_ServerAvailable_RespondsOk()
	{
		await using WebApplication app = await StartAsync(new StubServerMonitor(PhoriaServerHealth.Healthy));
		using HttpClient client = CreateClient(app);

		using HttpResponseMessage response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Health_ServerUnavailableUnderFailPolicy_RespondsServiceUnavailable()
	{
		await using WebApplication app = await StartAsync(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			PhoriaServerUnavailableBehavior.Fail);
		using HttpClient client = CreateClient(app);

		using HttpResponseMessage response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
		Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Health_ServerUnavailableUnderDegradePolicy_RespondsOkSoTheProbeDoesNotRestartIt()
	{
		await using WebApplication app = await StartAsync(
			new StubServerMonitor(PhoriaServerHealth.Unhealthy),
			PhoriaServerUnavailableBehavior.Degrade);
		using HttpClient client = CreateClient(app);

		using HttpResponseMessage response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Degraded", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
	}

	private static async Task<WebApplication> StartAsync(
		IPhoriaServerMonitor monitor,
		PhoriaServerUnavailableBehavior? unavailableBehavior = null)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Services.AddSingleton(monitor);
		builder.Services.AddHealthChecks().AddPhoriaServerHealthCheck();

		if (unavailableBehavior is { } behavior)
		{
			builder.Services.Configure<PhoriaOptions>(options => options.Server.UnavailableBehavior = behavior);
		}

		WebApplication app = builder.Build();

		// Deliberately unconfigured: the examples call MapHealthChecks("/health") with no
		// options, so the ASP.NET Core default status mapping is the contract under test.
		app.MapHealthChecks("/health");

		await app.StartAsync(TestContext.Current.CancellationToken);

		return app;
	}

	private static HttpClient CreateClient(WebApplication app)
	{
		IServer server = app.Services.GetRequiredService<IServer>();
		string? baseAddress = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

		return new HttpClient
		{
			BaseAddress = new Uri(baseAddress ?? throw new InvalidOperationException("Kestrel reported no bound address."))
		};
	}
}
