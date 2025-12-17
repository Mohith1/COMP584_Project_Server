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

    // Add JWT Authentication to Swagger
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

if (!string.IsNullOrEmpty(connectionString))
{
    // Handle Railway/Supabase PostgreSQL connection strings
    if (connectionString.StartsWith("postgresql://") || connectionString.StartsWith("postgres://"))
    {
        // Convert URI format to Npgsql format
        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }

    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback to SQLite for local development
    builder.Services.AddDbContext<FleetDbContext>(options =>
        options.UseSqlite("Data Source=FleetManagement.db"));
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

    // For Auth0/Okta tokens, also accept tokens from those issuers
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
            // Allow localhost for development
            if (origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
                return true;
            
            // Allow all Vercel deployments
            if (origin.EndsWith(".vercel.app"))
                return true;
            
            // Allow all Railway deployments
            if (origin.EndsWith(".railway.app"))
                return true;

            // Allow all Netlify deployments
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

// ===========================================
// 8. MIDDLEWARE PIPELINE
// ===========================================

// Enable CORS (must be before other middleware)
app.UseCors("AllowFrontend");

// Enable Swagger (all environments for API documentation)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Fleet Management API v1");
    options.RoutePrefix = "swagger";
});

// HTTPS redirection (skip in development if needed)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Authentication & Authorization
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

// Root endpoint
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
// 11. AUTO-MIGRATE DATABASE (optional)
// ===========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<FleetDbContext>();
        // Only migrate if using a real database
        if (!context.Database.IsInMemory())
        {
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration skipped or failed. This may be expected in some environments.");
    }
}

app.Run();
