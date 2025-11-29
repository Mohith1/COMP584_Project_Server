using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FleetManagement.Data.Common;
using FleetManagement.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Data;

public class FleetDbContext(DbContextOptions<FleetDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Country> Countries => Set<Country>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Owner> Owners => Set<Owner>();

    public DbSet<Fleet> Fleets => Set<Fleet>();

    public DbSet<FleetUser> FleetUsers => Set<FleetUser>();

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    public DbSet<TelematicsDevice> TelematicsDevices => Set<TelematicsDevice>();

    public DbSet<VehicleTelemetrySnapshot> VehicleTelemetrySnapshots => Set<VehicleTelemetrySnapshot>();

    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FleetDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampEntities();
        return base.SaveChanges();
    }

    private void StampEntities()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = DateTimeOffset.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = DateTimeOffset.UtcNow;
            }
        }
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(et => typeof(ISoftDeletable).IsAssignableFrom(et.ClrType)))
        {
            var method = typeof(FleetDbContext).GetMethod(nameof(GetIsDeletedFilter),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            var filter = method.Invoke(null, [])!;
            entityType.SetQueryFilter((LambdaExpression)filter);
        }
    }

    private static LambdaExpression GetIsDeletedFilter<TEntity>() where TEntity : class, ISoftDeletable
    {
        Expression<Func<TEntity, bool>> filter = entity => !entity.IsDeleted;
        return filter;
    }
}

