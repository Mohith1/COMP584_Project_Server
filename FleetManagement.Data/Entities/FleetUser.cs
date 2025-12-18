namespace FleetManagement.Data.Entities;

public class FleetUser
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? OktaUserId { get; set; }
    public int Role { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? AssignedVehicleId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation properties
    public Owner Owner { get; set; } = null!;
    public Vehicle? AssignedVehicle { get; set; }
}













