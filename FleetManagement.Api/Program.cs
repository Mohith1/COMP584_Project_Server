using System.Text;
using FleetManagement.Data;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Cities;
using FleetManagement.Services.Fleets;
using FleetManagement.Services.Owners;
using FleetManagement.Services.Vehicles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===========================================
// 0. CONFIGURE PORT FOR RAILWAY
// ===========================================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

Console.WriteLine($"[STARTUP] Configuring server to listen on port {port}");

// ===========================================
// 1. ADD CONTROLLERS
// ===========================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ===========================================
// 2. SWAGGER WITH JWT SUPPORT
// ===========================================
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fleet Management API",
        Version = "v1",
        Description = "API for managing fleets, vehicles, and telemetry"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ===========================================
// 3. DATABASE CONFIGURATION
// ===========================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

Console.WriteLine($"[STARTUP] Database connection string found: {!string.IsNullOrEmpty(connectionString)}");

if (!string.IsNullOrEmpty(connectionString))
{
    // Handle Railway/Supabase PostgreSQL connection strings
    if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
    {
        try
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var database = uri.AbsolutePath.TrimStart('/');
            connectionString = $"Host={uri.Host};Port={uri.Port};Database={database};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
            Console.WriteLine($"[STARTUP] Converted PostgreSQL URI to connection string. Host: {uri.Host}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[STARTUP] Error parsing DATABASE_URL: {ex.Message}");
        }
    }

    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    Console.WriteLine("[STARTUP] No DATABASE_URL found, using in-memory database");
    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseInMemoryDatabase("FleetManagementDb"));
}

// ===========================================
// 4. JWT AUTHENTICATION
// ===========================================
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? Environment.GetEnvironmentVariable("JWT_SECRET") 
    ?? "FleetManagement_SuperSecretKey_ChangeInProduction_AtLeast32Chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] 
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? "FleetManagementAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] 
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? "FleetManagementClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    options.TokenValidationParameters.ValidIssuers = new[]
    {
        jwtIssuer,
        builder.Configuration["Auth0:Domain"] ?? "",
        builder.Configuration["Okta:Domain"] ?? ""
    }.Where(s => !string.IsNullOrEmpty(s)).ToArray();
});

// ===========================================
// 5. AUTHORIZATION POLICIES
// ===========================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAuthenticatedUser", policy =>
        policy.RequireAuthenticatedUser());
    
    options.AddPolicy("RequireOwner", policy =>
        policy.RequireRole("Owner", "Admin"));
    
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));
});

// ===========================================
// 6. REGISTER SERVICES (Dependency Injection)
// ===========================================
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ICountryService, CountryService>();

// ===========================================
// 7. CORS CONFIGURATION
// ===========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            
            if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                return true;
            
            if (origin.EndsWith(".vercel.app"))
                return true;
            
            if (origin.EndsWith(".railway.app"))
                return true;

            if (origin.EndsWith(".netlify.app"))
                return true;

            return false;
        })
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

Console.WriteLine("[STARTUP] Application built successfully");

// ===========================================
// 8. MIDDLEWARE PIPELINE
// ===========================================

app.UseCors("AllowFrontend");

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Management API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// ===========================================
// 9. HEALTH CHECK ENDPOINT
// ===========================================
app.MapGet("/health", () => Results.Ok(new 
{ 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithOpenApi();

app.MapGet("/", () => Results.Ok(new
{
    name = "Fleet Management API",
    version = "1.0.0",
    documentation = "/swagger",
    health = "/health"
}))
.WithName("Root")
.WithOpenApi();

// ===========================================
// 10. MAP CONTROLLERS
// ===========================================
app.MapControllers();

// ===========================================
// 11. DATABASE MIGRATION (non-blocking)
// ===========================================
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    
    if (context.Database.IsNpgsql())
    {
        Console.WriteLine("[STARTUP] Running database migrations...");
        await context.Database.MigrateAsync();
        Console.WriteLine("[STARTUP] Database migrations completed");
    }
    else
    {
        Console.WriteLine("[STARTUP] Using in-memory database, ensuring created...");
        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("[STARTUP] In-memory database ready");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[STARTUP] Database setup warning: {ex.Message}");
    // Don't crash - the app can still serve health checks and some endpoints
}

Console.WriteLine($"[STARTUP] Server starting on http://0.0.0.0:{port}");

app.Run();
