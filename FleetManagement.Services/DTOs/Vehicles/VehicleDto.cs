namespace FleetManagement.Services.DTOs.Vehicles;

public class VehicleDto
{
    public Guid Id { get; set; }
    public string Vin { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int ModelYear { get; set; }
    public int Status { get; set; }
    public Guid FleetId { get; set; }
    public string? FleetName { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public class CreateVehicleDto
{
    public string Vin { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int ModelYear { get; set; }
    public int Status { get; set; }
    public Guid FleetId { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateVehicleDto
{
    public string Vin { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int ModelYear { get; set; }
    public int Status { get; set; }
}









