using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FleetManagement.Data.Entities;

namespace FleetManagement.Data;

public class FleetDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public FleetDbContext(DbContextOptions<FleetDbContext> options)
        : base(options)
    {
    }

    // Identity tables (inherited from IdentityDbContext)
    // - Users (ApplicationUser)
    // - Roles (ApplicationRole)
    // - UserClaims, UserLogins, UserRoles, UserTokens, RoleClaims

    // Custom entities
    public DbSet<Country> Countries { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Owner> Owners { get; set; }
    public DbSet<Fleet> Fleets { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<FleetUser> FleetUsers { get; set; }
    public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; }
    public DbSet<TelematicsDevice> TelematicsDevices { get; set; }
    public DbSet<VehicleTelemetrySnapshot> VehicleTelemetrySnapshots { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names for Identity
        modelBuilder.Entity<ApplicationUser>().ToTable("AppUsers");
        modelBuilder.Entity<ApplicationRole>().ToTable("AppRoles");

        // Apply entity configurations from Configurations folder if they exist
        // This will be handled by applying configurations automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);
    }
}










