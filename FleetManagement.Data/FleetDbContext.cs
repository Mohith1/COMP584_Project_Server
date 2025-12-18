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

        // =====================================================
        // IMPORTANT: PostgreSQL is case-sensitive!
        // Table names must match exactly what's in Supabase
        // Using quoted PascalCase names to match Supabase schema
        // =====================================================

        // Identity tables
        modelBuilder.Entity<ApplicationUser>().ToTable("AppUsers");
        modelBuilder.Entity<ApplicationRole>().ToTable("AppRoles");

        // Custom entities - explicit table mapping for PostgreSQL
        modelBuilder.Entity<Country>().ToTable("Countries");
        modelBuilder.Entity<City>().ToTable("Cities");
        modelBuilder.Entity<Owner>().ToTable("Owners");
        modelBuilder.Entity<Fleet>().ToTable("Fleets");
        modelBuilder.Entity<Vehicle>().ToTable("Vehicles");
        modelBuilder.Entity<FleetUser>().ToTable("FleetUsers");
        modelBuilder.Entity<MaintenanceTicket>().ToTable("MaintenanceTickets");
        modelBuilder.Entity<TelematicsDevice>().ToTable("TelematicsDevices");
        modelBuilder.Entity<VehicleTelemetrySnapshot>().ToTable("VehicleTelemetrySnapshots");
        modelBuilder.Entity<RefreshToken>().ToTable("RefreshTokens");

        // Configure Owner entity
        modelBuilder.Entity<Owner>(entity =>
        {
            entity.Property(e => e.CityId).IsRequired(false);  // CityId is nullable
            entity.Property(e => e.Auth0UserId).HasMaxLength(255);
            
            entity.HasOne(e => e.City)
                .WithMany()
                .HasForeignKey(e => e.CityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure City -> Country relationship
        modelBuilder.Entity<City>(entity =>
        {
            entity.HasOne(e => e.Country)
                .WithMany(c => c.Cities)
                .HasForeignKey(e => e.CountryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Fleet -> Owner relationship
        modelBuilder.Entity<Fleet>(entity =>
        {
            entity.HasOne(e => e.Owner)
                .WithMany(o => o.Fleets)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Vehicle -> Fleet relationship
        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasOne(e => e.Fleet)
                .WithMany(f => f.Vehicles)
                .HasForeignKey(e => e.FleetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Apply any additional configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);
    }
}














