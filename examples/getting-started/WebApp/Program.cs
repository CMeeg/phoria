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

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["image/svg+xml"]);
});

IMvcBuilder mvcBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
	mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddPhoria();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    // WebSockets support is required for Vite HMR (hot module reload)
	app.UseWebSockets();
}

// The order of the Phoria middleware matters so we will place it last
app.UsePhoria();

app.Run();
