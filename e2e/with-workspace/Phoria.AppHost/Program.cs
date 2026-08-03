using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var webAppDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "WebApp"));
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

var webAppConfiguration = new ConfigurationBuilder()
	.SetBasePath(webAppDirectory)
	.AddJsonFile("appsettings.json", optional: false)
	.AddJsonFile($"appsettings.{environment}.json", optional: true)
	.Build();

var port = int.TryParse(webAppConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : 5173;
var nodeCommand = "tsx";
var nodeArguments = new[] { Path.Combine(webAppDirectory, "ui", "src", "server.ts") };
var nodeEnvironment = "development";
// The Vite dev server resolves the root from `process.cwd()` and only discovers the vite config in the current directory,
// so in development it must be started from the WebApp directory where the config lives.
var nodeWorkingDirectory = webAppDirectory;

if (!isDevelopment)
{
	var processConfiguration = webAppConfiguration.GetSection("Phoria:Server:Process");
	nodeCommand = processConfiguration["Command"] ?? throw new InvalidOperationException("Phoria server command is not configured.");
	nodeArguments = processConfiguration.GetSection("Arguments").GetChildren().Select(section => section.Value ?? string.Empty).ToArray();
	nodeEnvironment = "production";
	// In production the root is the `ui` directory; a non-development profile cannot use the WebApp directory.
	nodeWorkingDirectory = Path.Combine(webAppDirectory, "ui");
}

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: 5247, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.AddExecutable("phoria-server", nodeCommand, nodeWorkingDirectory, nodeArguments)
	.WithWorkingDirectory(nodeWorkingDirectory)
	.WithHttpEndpoint(port: port, name: "http", isProxied: false)
	.WithEnvironment("NODE_ENV", nodeEnvironment)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.Build().Run();
