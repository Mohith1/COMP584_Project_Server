using System;
using FleetManagement.Data.DependencyInjection;
using FleetManagement.Data.Entities;
using FleetManagement.Services.Abstractions;
using FleetManagement.Services.Auth;
using FleetManagement.Services.Fleets;
using FleetManagement.Services.Okta;
using FleetManagement.Services.Options;
using FleetManagement.Services.Owners;
using FleetManagement.Services.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FleetManagement.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFleetManagementCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDataLayer(configuration);

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<OktaSettings>(configuration.GetSection("Okta"));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<FleetManagement.Data.FleetDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IFleetService, FleetService>();
        services.AddScoped<ISeedService, SeedService>();
        services.AddHttpClient<OktaIntegrationService>();
        services.AddScoped<IOktaIntegrationService>(sp => sp.GetRequiredService<OktaIntegrationService>());

        return services;
    }
}

