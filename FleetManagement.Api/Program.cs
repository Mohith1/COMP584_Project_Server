using Microsoft.EntityFrameworkCore;
using FleetManagement.Data;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Fleets;
using FleetManagement.Services.Vehicles;
using FleetManagement.Services.Owners;
using FleetManagement.Services.Cities;

var builder = WebApplication.CreateBuilder(args);

// Configure port for Railway - Railway sets PORT env var
// Must bind to 0.0.0.0 (all interfaces) not just localhost
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(postgresConnection))
{
    try
    {
        // Handle Railway's DATABASE_URL format if needed
        if (postgresConnection.StartsWith("postgresql://"))
        {
            // Convert postgresql:// to standard connection string format
            var uri = new Uri(postgresConnection);
            var userInfo = uri.UserInfo.Split(':');
            postgresConnection = $"Host={uri.Host};Port={uri.Port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;";
        }

        builder.Services.AddDbContext<FleetDbContext>(options =>
            options.UseNpgsql(postgresConnection));
        
        Console.WriteLine("[Database] PostgreSQL connection configured");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Error configuring PostgreSQL: {ex.Message}");
        Console.WriteLine("[Database] Falling back to in-memory database");
        builder.Services.AddDbContext<FleetDbContext>(options =>
            options.UseInMemoryDatabase("FleetManagementDb"));
    }
}
else
{
    // Fallback to in-memory database for development/testing
    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseInMemoryDatabase("FleetManagementDb"));
    
    Console.WriteLine("[Database] Using in-memory database (PostgreSQL not configured)");
}

// Register services
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ICountryService, CountryService>();

// Add CORS support for Vercel proxy and frontend applications
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndLocalhost", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                // Allow localhost for development
                if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                    return true;
                
                // Allow all Vercel deployments (both preview and production)
                if (origin.EndsWith(".vercel.app") || origin.EndsWith(".vercel.app/"))
                    return true;
                
                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Log configuration for debugging
var actualPort = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"[Railway] PORT env var: {actualPort}");
Console.WriteLine($"[Railway] Application binding to: http://0.0.0.0:{actualPort}");
Console.WriteLine($"[Railway] Application starting...");

// Configure the HTTP request pipeline.
// CRITICAL: Health check endpoint MUST be registered FIRST, before any middleware
// This ensures Railway can immediately check /health without CORS or other middleware blocking it
app.MapGet("/health", () => 
{
    return Results.Ok(new { 
        status = "healthy", 
        timestamp = DateTime.UtcNow,
        port = actualPort
    });
})
    .WithName("HealthCheck")
    .WithOpenApi();

// Also add root endpoint for Railway healthcheck fallback
app.MapGet("/", () => Results.Ok(new { 
    status = "ok", 
    message = "Fleet Management API is running",
    health = "/health",
    swagger = "/swagger"
}))
    .WithName("Root")
    .WithOpenApi();

// Enable CORS (must be before other middleware, but after health endpoint)
app.UseCors("AllowVercelAndLocalhost");

// Enable Swagger in all environments (can be restricted to Development if needed)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Management API v1");
    c.RoutePrefix = "swagger";
});

// HTTPS redirection - skip in containerized environments where reverse proxy handles HTTPS
// Uncomment the line below if you need HTTPS redirection in production
// app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();
