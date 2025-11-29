using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("Countries");
        builder.HasIndex(country => country.IsoCode).IsUnique();
        builder.Property(country => country.Name)
            .HasColumnType("varchar(128)");
        builder.Property(country => country.IsoCode)
            .HasColumnType("char(3)")
            .IsFixedLength();
        builder.Property(country => country.Continent)
            .HasColumnType("varchar(32)");
    }
}

