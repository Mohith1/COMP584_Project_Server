using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");
        builder.HasIndex(vehicle => vehicle.Vin).IsUnique();
        builder.Property(vehicle => vehicle.Vin).HasColumnType("char(17)").IsFixedLength();
        builder.Property(vehicle => vehicle.PlateNumber).HasColumnType("varchar(16)");
        builder.Property(vehicle => vehicle.Make).HasColumnType("varchar(64)");
        builder.Property(vehicle => vehicle.Model).HasColumnType("varchar(64)");
        builder.Property(vehicle => vehicle.Status).HasConversion<int>();

        builder.HasOne(vehicle => vehicle.Fleet)
            .WithMany(fleet => fleet.Vehicles)
            .HasForeignKey(vehicle => vehicle.FleetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

