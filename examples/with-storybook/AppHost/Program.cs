using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var workspaceRoot = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, ".."));
var webAppDirectory = Path.GetFullPath(Path.Combine(workspaceRoot, "WebApp"));
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

var webAppConfiguration = new ConfigurationBuilder()
	.SetBasePath(webAppDirectory)
	.AddJsonFile("appsettings.json", optional: false)
	.AddJsonFile($"appsettings.{environment}.json", optional: true)
	.Build();

var webAppPort = 5973;

// The Vite dev server resolves the root from `process.cwd()` and only discovers the vite config in the current
// directory, so package.json scripts must run from the same directory where `vite.config.ts` lives

var phoriaServerPort = int.TryParse(webAppConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : 5873;
var phoriaServerHttps = bool.TryParse(webAppConfiguration["Phoria:Server:Https"], out var configuredHttps) && configuredHttps;

var phoriaServer = builder.AddJavaScriptApp("phoria-server", webAppDirectory)
	.WithRunScript(isDevelopment ? "dev:server" : "preview:server")
	.WithPnpm(install: false);

if (phoriaServerHttps)
{
	phoriaServer.WithHttpsEndpoint(port: phoriaServerPort, name: "https", isProxied: false);
}
else
{
	phoriaServer.WithHttpEndpoint(port: phoriaServerPort, name: "http", isProxied: false);
}

phoriaServer
	.WithHttpHealthCheck("/hc")
	.WithEnvironment("NODE_ENV", isDevelopment ? "development" : "production")
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(OtlpProtocol.HttpProtobuf);

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: webAppPort, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(OtlpProtocol.HttpProtobuf)
	.WaitFor(phoriaServer);

builder.Build().Run();
