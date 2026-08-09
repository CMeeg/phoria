using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Phoria;
using Phoria.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

PhoriaObservabilityOptions observability = builder.Configuration
	.GetSection(PhoriaObservabilityOptions.SectionName)
	.Get<PhoriaObservabilityOptions>() ?? new();

if (observability.Logging)
{
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
}

if (observability.Tracing.Enabled)
{
	builder.Services.AddOpenTelemetry().WithTracing(tracing =>
	{
		tracing
			.AddSource(PhoriaActivitySource.Name)
			.AddAspNetCoreInstrumentation()
			.AddHttpClientInstrumentation(o => o.FilterHttpRequestMessage =
				request => request.RequestUri?.AbsolutePath != "/hc")
			.SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(observability.Tracing.SamplingRatio)));
		if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
		{
			tracing.AddOtlpExporter();
		}
	});
}

if (observability.Metrics)
{
	builder.Services.AddOpenTelemetry().WithMetrics(metrics =>
	{
		metrics.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
		if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
		{
			metrics.AddOtlpExporter();
		}
	});
}

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
	// WebSockets support is required for Vite HMR (hot module reload)
	app.UseWebSockets();
}
else
{
	app.UseExceptionHandler("/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
	app.UseResponseCompression();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthorization();
app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

// The order of the Phoria middleware matters so we will place it last
app.UsePhoria();

app.Run();
