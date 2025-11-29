using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class VehicleTelemetrySnapshotConfiguration : IEntityTypeConfiguration<VehicleTelemetrySnapshot>
{
    public void Configure(EntityTypeBuilder<VehicleTelemetrySnapshot> builder)
    {
        builder.ToTable("VehicleTelemetrySnapshots");
        builder.HasIndex(snapshot => new { snapshot.VehicleId, snapshot.CapturedAtUtc });

        builder.HasOne(snapshot => snapshot.Vehicle)
            .WithMany(vehicle => vehicle.TelemetrySnapshots)
            .HasForeignKey(snapshot => snapshot.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

