using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class FleetConfiguration : IEntityTypeConfiguration<Fleet>
{
    public void Configure(EntityTypeBuilder<Fleet> builder)
    {
        builder.ToTable("Fleets");
        builder.HasIndex(f => new { f.OwnerId, f.Name }).IsUnique();
        builder.Property(f => f.Name).HasColumnType("varchar(128)");
        builder.Property(f => f.Description).HasColumnType("varchar(512)");

        builder.HasOne(f => f.Owner)
            .WithMany(o => o.Fleets)
            .HasForeignKey(f => f.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

