using Frontend;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Point the shared HttpClient at the Backend API (not the Frontend's own host).
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5033/") });
builder.Services.AddScoped<Frontend.Services.ThemeService>();
builder.Services.AddScoped<Frontend.Services.AuthStateService>();
await builder.Build().RunAsync();
