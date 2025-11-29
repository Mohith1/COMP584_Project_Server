using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.HasIndex(city => new { city.CountryId, city.Name }).IsUnique();
        builder.Property(city => city.Name)
            .HasColumnType("varchar(128)");
        builder.Property(city => city.PostalCode)
            .HasColumnType("varchar(8)");
        builder.Property(city => city.PopulationMillions)
            .HasPrecision(11, 2);

        builder.HasOne(city => city.Country)
            .WithMany(country => country.Cities)
            .HasForeignKey(city => city.CountryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

