using FleetManagement.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FleetManagement.Data.Configurations;

public class MaintenanceTicketConfiguration : IEntityTypeConfiguration<MaintenanceTicket>
{
    public void Configure(EntityTypeBuilder<MaintenanceTicket> builder)
    {
        builder.ToTable("MaintenanceTickets");
        builder.Property(ticket => ticket.Title).HasColumnType("varchar(128)");
        builder.Property(ticket => ticket.Description).HasColumnType("varchar(1024)");
        builder.Property(ticket => ticket.Status).HasConversion<int>();

        builder.HasOne(ticket => ticket.Vehicle)
            .WithMany(vehicle => vehicle.MaintenanceTickets)
            .HasForeignKey(ticket => ticket.VehicleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

