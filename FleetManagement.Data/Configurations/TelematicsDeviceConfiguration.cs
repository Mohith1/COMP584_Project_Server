using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class TelematicsDeviceConfiguration : IEntityTypeConfiguration<TelematicsDevice>
{
    public void Configure(EntityTypeBuilder<TelematicsDevice> builder)
    {
        builder.ToTable("TelematicsDevices");
        builder.HasIndex(device => device.SerialNumber).IsUnique();
        builder.Property(device => device.SerialNumber).HasColumnType("varchar(32)");
        builder.Property(device => device.Iccid).HasColumnType("varchar(32)");
        builder.Property(device => device.Imei).HasColumnType("varchar(32)");
        builder.Property(device => device.FirmwareVersion).HasColumnType("varchar(32)");

        builder.HasOne(device => device.Vehicle)
            .WithOne(vehicle => vehicle.TelematicsDevice)
            .HasForeignKey<TelematicsDevice>(device => device.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

