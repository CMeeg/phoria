using Phoria.Vite;
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
// TODO(docs): If using React, you must enable React Refresh for HMR to work (you will see this error in the console if you don't https://github.com/vitejs/vite-plugin-react/pull/79)
builder.Services.AddViteServices();

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

// The order of the Vite dev middleware matters so we will place it last
if (app.Environment.IsDevelopment())
{
    // Use the Vite Development Server when the environment is Development

    // WebSockets support is required for HMR (hot module reload)
    app.UseWebSockets();

    // Enable all required features to use the Vite Development Server
    app.UseViteDevelopmentServer(true);
}

app.Run();