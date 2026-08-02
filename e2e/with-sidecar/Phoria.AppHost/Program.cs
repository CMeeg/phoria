var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WebApp>("webapp")
	.WithHttpEndpoint(port: 5248, name: "http", isProxied: false)
	.WithEnvironment("DOTNET_ENVIRONMENT", "Production")
	.WithEnvironment("NODE_ENV", "production")
	.WithOtlpExporter(Aspire.Hosting.OtlpProtocol.HttpProtobuf);

builder.Build().Run();
