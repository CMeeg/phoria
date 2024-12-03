using Phoria;
using Phoria.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

builder.Services.AddHttpClient<WeatherApiClient>(client =>
	{
		// This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
		// Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
		client.BaseAddress = new("https+http://apiservice");
	});

// Add services to the container

builder.Services.AddResponseCompression(options =>
{
	options.EnableForHttps = true;
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
}

app.UseHttpsRedirection();

app.UseResponseCompression();

/* TODO: Http cache headers
   * https://developer.mozilla.org/en-US/docs/Web/HTTP/Caching
   * Would love a library like cdn-cache-control, but can't find anything similar for dotnet
     * https://developers.netlify.com/guides/advanced-caching-made-easy/
     * https://developers.netlify.com/guides/how-to-do-advanced-caching-and-isr-with-astro/
   * Anything from Phoria server should be cacheable for a long time
   * Static files from wwwroot should be cacheable for a long time if `asp-append-version` is used
   * Ultimately it should be configurable by the user, but we should have good examples to base it on
*/

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

if (app.Environment.IsDevelopment())
{
	// WebSockets support is required for Vite HMR (hot module reload)
	app.UseWebSockets();
}

// The order of the Phoria middleware matters so we will place it last
// TODO: Does the order still matter?
app.UsePhoria();

app.Run();
