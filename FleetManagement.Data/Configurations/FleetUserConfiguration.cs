using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class FleetUserConfiguration : IEntityTypeConfiguration<FleetUser>
{
    public void Configure(EntityTypeBuilder<FleetUser> builder)
    {
        builder.ToTable("FleetUsers");
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.FirstName).HasColumnType("varchar(64)");
        builder.Property(user => user.LastName).HasColumnType("varchar(64)");
        builder.Property(user => user.Email).HasColumnType("varchar(256)");
        builder.Property(user => user.PhoneNumber).HasColumnType("varchar(32)");
        builder.Property(user => user.OktaUserId).HasColumnType("varchar(64)");
        builder.Property(user => user.Role).HasConversion<int>();

        builder.HasOne(user => user.Owner)
            .WithMany(owner => owner.FleetUsers)
            .HasForeignKey(user => user.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(user => user.AssignedVehicle)
            .WithMany(vehicle => vehicle.AssignedDrivers)
            .HasForeignKey(user => user.AssignedVehicleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

