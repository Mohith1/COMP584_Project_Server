namespace FleetManagement.Services.DTOs.Fleets;

public class FleetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public int VehicleCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
}

public class CreateFleetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
}

public class UpdateFleetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}











