using FleetManagement.Data.Entities;
using FleetManagement.Services.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace FleetManagement.Api.Extensions;

public static class IdentitySeeder
{
    public static async Task SeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var roles = new[]
        {
            SystemRoles.Owner,
            SystemRoles.FleetManager,
            SystemRoles.Driver,
            SystemRoles.Administrator
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = role,
                    NormalizedName = role.ToUpperInvariant()
                });
            }
        }

        var adminEmail = configuration["AdminUser:Email"];
        var adminPassword = configuration["AdminUser:Password"];

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, SystemRoles.Administrator);
                }
            }
        }
    }
}

