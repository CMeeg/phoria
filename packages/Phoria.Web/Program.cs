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

IMvcBuilder mvcBuilder = builder.Services.AddRazorPages();

if (builder.Environment.IsDevelopment())
{
	mvcBuilder.AddRazorRuntimeCompilation();
}

// TODO: Configure https on the dev server
builder.Services.AddPhoria();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

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
