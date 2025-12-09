using Microsoft.EntityFrameworkCore;
using FleetManagement.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database (optional - won't crash if connection string is missing)
// Note: Add Npgsql.EntityFrameworkCore.PostgreSQL package for PostgreSQL support
var postgresConnection = builder.Configuration.GetConnectionString("PostgreSQL");
if (!string.IsNullOrEmpty(postgresConnection))
{
    // TODO: Uncomment when Npgsql package is added
    // builder.Services.AddDbContext<FleetDbContext>(options =>
    //     options.UseNpgsql(postgresConnection));
    
    // Temporary: Use in-memory until PostgreSQL package is added
    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseInMemoryDatabase("FleetManagementDb"));
}
else
{
    // Fallback: Use in-memory database for health checks if no connection string
    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseInMemoryDatabase("FleetManagementDb"));
}

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

// Configure the HTTP request pipeline.
// Enable CORS (must be before other middleware)
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

// Health check endpoint (for Railway)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

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
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
