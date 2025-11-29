using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AppUsers");
        builder.Property(user => user.OktaUserId).HasColumnType("varchar(64)");
        builder.HasIndex(user => user.OktaUserId).IsUnique().HasFilter("[OktaUserId] IS NOT NULL");

        builder.HasOne(user => user.OwnerProfile)
            .WithOne(owner => owner.IdentityUser)
            .HasForeignKey<Owner>(owner => owner.IdentityUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

