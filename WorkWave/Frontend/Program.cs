using Frontend;
using Frontend.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base URL comes from wwwroot/appsettings.json (Development) or
// wwwroot/appsettings.Production.json (Release build) — never hardcoded,
// so the same build works against whichever backend URL you deploy to.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7186/";

builder.Services.AddScoped<AuthHeaderHandler>();
builder.Services.AddHttpClient("WorkWaveApi", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthHeaderHandler>();

// Anything that injects a plain HttpClient gets the named, token-attaching client above.
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("WorkWaveApi"));

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<LocalizationService>();
await builder.Build().RunAsync();
