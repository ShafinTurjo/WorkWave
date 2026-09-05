using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Backend.Options;
using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// EF Core + SQL Server (running via Docker locally).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Controllers (traditional [ApiController] style).
builder.Services.AddControllers();

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
// CORS: allow the Blazor WASM Frontend (its dev-server origins) to call this API.
const string FrontendCorsPolicy = "FrontendCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(
                "https://localhost:7174",
                "http://localhost:5189")
            .AllowAnyHeader()
            .AllowAnyMethod();
        // If cookies/auth headers with credentials are needed later, also add:
        // .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure the resume-uploads folder exists (wwwroot may not be created by default).
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "resumes"));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve uploaded resumes (and any other static assets) from wwwroot.
app.UseStaticFiles();

// Must come before endpoint mapping, after routing.
app.UseCors(FrontendCorsPolicy);

app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
