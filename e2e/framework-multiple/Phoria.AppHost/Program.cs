using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var webAppDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "WebApp"));
var workspaceRoot = Path.GetFullPath(Path.Combine(webAppDirectory, ".."));
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

var webAppConfiguration = new ConfigurationBuilder()
	.SetBasePath(webAppDirectory)
	.AddJsonFile("appsettings.json", optional: false)
	.AddJsonFile($"appsettings.{environment}.json", optional: true)
	.Build();

var port = int.TryParse(webAppConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : 5173;

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: 5247, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

// The Vite dev server resolves the root from `process.cwd()` and only discovers the vite config in the current
// directory, so both scripts must run from the workspace root where `vite.config.ts` lives.
builder.AddJavaScriptApp("phoria-server", workspaceRoot)
	.WithRunScript(isDevelopment ? "dev:server" : "preview:server")
	.WithPnpm(install: false)
	.WithHttpEndpoint(port: port, name: "http", isProxied: false)
	.WithEnvironment("NODE_ENV", isDevelopment ? "development" : "production")
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.Build().Run();
