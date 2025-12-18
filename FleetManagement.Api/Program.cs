using System.Text;
using FleetManagement.Api.Hubs;
using FleetManagement.Data;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Cities;
using FleetManagement.Services.Fleets;
using FleetManagement.Services.Owners;
using FleetManagement.Services.Vehicles;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
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

// Auth0 Configuration
var auth0Domain = builder.Configuration["Auth0:Domain"] 
    ?? Environment.GetEnvironmentVariable("AUTH0_DOMAIN") 
    ?? "";
var auth0Audience = builder.Configuration["Auth0:Audience"] 
    ?? Environment.GetEnvironmentVariable("AUTH0_AUDIENCE") 
    ?? "https://fleetmanagement-api-production.up.railway.app";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure for both custom JWT and Auth0 tokens
    var validIssuers = new List<string> { jwtIssuer };
    var validAudiences = new List<string> { jwtAudience };
    
    // Add Auth0 issuer if configured
    if (!string.IsNullOrEmpty(auth0Domain))
    {
        // Auth0 issuer format: https://{domain}/
        var auth0Issuer = auth0Domain.StartsWith("http") ? auth0Domain : $"https://{auth0Domain}/";
        validIssuers.Add(auth0Issuer);
        validAudiences.Add(auth0Audience);
    }
    
    // Add Okta issuer if configured
    var oktaDomain = builder.Configuration["Okta:Domain"] ?? "";
    if (!string.IsNullOrEmpty(oktaDomain))
    {
        validIssuers.Add(oktaDomain);
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuers = validIssuers.ToArray(),
        ValidAudiences = validAudiences.ToArray(),
        // For custom JWT tokens (owner authentication)
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    // Configure Auth0 token validation (if Auth0 is configured)
    if (!string.IsNullOrEmpty(auth0Domain))
    {
        // Auth0 uses JWKS (JSON Web Key Set) for token validation
        // The issuer is https://{domain}/
        var auth0Issuer = auth0Domain.StartsWith("http") ? auth0Domain : $"https://{auth0Domain}/";
        
        // Set Authority for Auth0 JWKS validation
        // This allows the middleware to fetch signing keys from Auth0's JWKS endpoint
        options.Authority = auth0Issuer;
        options.Audience = auth0Audience;
        
        // Note: When Authority is set, Auth0 tokens are validated using JWKS automatically
        // Custom JWT tokens (with our symmetric key) are still validated via TokenValidationParameters
        // The middleware will try both validation methods based on the token's issuer
    }

    // Configure JWT for SignalR WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // SignalR sends token as query string parameter "access_token"
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && 
                (path.StartsWithSegments("/hub/fleets") || 
                 path.StartsWithSegments("/hub/vehicles") || 
                 path.StartsWithSegments("/hub/telemetry")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
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
// 6. SIGNALR CONFIGURATION
// ===========================================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true; // Enable in development for debugging
});

// ===========================================
// 7. REGISTER SERVICES (Dependency Injection)
// ===========================================
builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<ICountryService, CountryService>();

// ===========================================
// 8. CORS CONFIGURATION
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
// 9. MIDDLEWARE PIPELINE
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
// 10. HEALTH CHECK ENDPOINT
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
// 11. MAP SIGNALR HUBS
// ===========================================
app.MapHub<FleetHub>("/hub/fleets");
app.MapHub<VehicleHub>("/hub/vehicles");
app.MapHub<TelemetryHub>("/hub/telemetry");

Console.WriteLine("[STARTUP] SignalR hubs mapped:");
Console.WriteLine("  - /hub/fleets");
Console.WriteLine("  - /hub/vehicles");
Console.WriteLine("  - /hub/telemetry");

// ===========================================
// 12. MAP CONTROLLERS
// ===========================================
app.MapControllers();

// ===========================================
// 13. DATABASE MIGRATION (non-blocking)
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
