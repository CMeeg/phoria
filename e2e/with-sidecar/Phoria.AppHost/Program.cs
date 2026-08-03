using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);
var environment = builder.Environment.EnvironmentName;
var isDevelopment = builder.Environment.IsDevelopment();

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: 5248, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", environment)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", environment)
	.WithEnvironment("NODE_ENV", isDevelopment ? "development" : "production")
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.Build().Run();
