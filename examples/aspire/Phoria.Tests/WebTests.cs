using System.Net;

namespace Phoria.Tests;

public class WebTests
{
	[Fact]
	public async Task GetWebResourceRootReturnsOkStatusCode()
	{
		// Arrange
		IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Phoria_AppHost>();
		await using var app = await appHost.BuildAsync();
		await app.StartAsync();

		// Act
		HttpClient httpClient = app.CreateHttpClient("webfrontend");
		HttpResponseMessage response = await httpClient.GetAsync("/");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
	}
}
