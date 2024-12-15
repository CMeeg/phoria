using Microsoft.AspNetCore.ResponseCompression;
using Phoria;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container

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

// Configure the HTTP request pipeline

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");

	app.UseHsts();
}

app.UseHttpsRedirection();

app.UseResponseCompression();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
	.WithStaticAssets();

if (app.Environment.IsDevelopment())
{
	// WebSockets support is required for Vite HMR (hot module reload)
	app.UseWebSockets();
}

// The order of the Phoria middleware matters so we will place it last
app.UsePhoria();

app.Run();
