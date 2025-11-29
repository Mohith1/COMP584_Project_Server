using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace FleetManagement.Data.Factories;

public class FleetDbContextFactory : IDesignTimeDbContextFactory<FleetDbContext>
{
    public FleetDbContext CreateDbContext(string[] args)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("SqlServer")
                               ?? "Server=(localdb)\\mssqllocaldb;Database=FleetManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<FleetDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure();
            sqlOptions.MigrationsAssembly(typeof(FleetDbContext).Assembly.FullName);
        });

        return new FleetDbContext(optionsBuilder.Options);
    }
}

