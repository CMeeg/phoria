using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var webAppDirectory = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, ".."));
var previewConfiguration = new ConfigurationBuilder()
	.SetBasePath(webAppDirectory)
	.AddJsonFile("appsettings.Preview.json", optional: false)
	.Build();

var processConfiguration = previewConfiguration.GetSection("Phoria:Server:Process");
var command = processConfiguration["Command"] ?? throw new InvalidOperationException("Phoria server command is not configured.");
var arguments = processConfiguration.GetSection("Arguments").GetChildren().Select(section => section.Value ?? string.Empty).ToArray();
var port = int.TryParse(previewConfiguration["Phoria:Server:Port"], out var configuredPort) ? configuredPort : 5173;
var isDev = builder.Environment.IsDevelopment();
var nodeCommand = isDev ? "tsx" : command;
var nodeArguments = isDev
	? new[] { Path.Combine(webAppDirectory, "ui", "src", "server.ts") }
	: arguments;
var nodeEnvironment = isDev ? "development" : "production";

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: 5247, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", "Preview")
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Preview")
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.AddExecutable("phoria-server", nodeCommand, Path.Combine(webAppDirectory, "ui"), nodeArguments)
	.WithWorkingDirectory(Path.Combine(webAppDirectory, "ui"))
	.WithHttpEndpoint(port: port, name: "http", isProxied: false)
	.WithEnvironment("NODE_ENV", nodeEnvironment)
	.WithEnvironment("DOTNET_ENVIRONMENT", "Preview")
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.Build().Run();
