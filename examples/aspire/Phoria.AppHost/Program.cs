IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ProjectResource> apiService = builder.AddProject<Projects.Phoria_ApiService>("apiservice");

// TODO: I just want the web project to use my docker file :( Raise issue upstream
if (builder.ExecutionContext.IsRunMode)
{
	builder.AddProject<Projects.Phoria_Web>("webfrontend")
		.WithExternalHttpEndpoints()
		.WithReference(apiService);
}
else
{
	builder.AddDockerfile(
		"webfrontend",
		"../",
		"Dockerfile.phoria-web")
		.WithExternalHttpEndpoints()
		.WithReference(apiService);
}

builder.Build().Run();
