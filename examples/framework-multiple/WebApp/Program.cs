using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Logs;
using Phoria;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddOpenTelemetry(options =>
{
	options.IncludeFormattedMessage = true;
	options.IncludeScopes = true;
	options.ParseStateValues = true;

	if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
	{
		options.AddOtlpExporter();
	}
});

IMvcBuilder mvcBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
	mvcBuilder.AddRazorRuntimeCompilation();
}
else
{
	builder.Services.AddResponseCompression(options =>
	{
		options.EnableForHttps = true;
		options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"]);
	});
}

builder.Services.AddPhoria();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseWebSockets();
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
	app.UseResponseCompression();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.UsePhoria();

app.Run();
