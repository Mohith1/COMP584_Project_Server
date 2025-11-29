using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("Owners");
        builder.HasIndex(owner => owner.ContactEmail).IsUnique();
        builder.Property(owner => owner.CompanyName).HasColumnType("varchar(128)");
        builder.Property(owner => owner.ContactEmail).HasColumnType("varchar(256)");
        builder.Property(owner => owner.ContactPhone).HasColumnType("varchar(32)");
        builder.Property(owner => owner.PrimaryContactName).HasColumnType("varchar(128)");
        builder.Property(owner => owner.OktaGroupId).HasColumnType("varchar(64)");
        builder.Property(owner => owner.TimeZone).HasColumnType("varchar(64)");
        builder.Property(owner => owner.IdentityUserId);

        builder.HasOne(owner => owner.City)
            .WithMany()
            .HasForeignKey(owner => owner.CityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

