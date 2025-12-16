using Microsoft.AspNetCore.Identity;

namespace FleetManagement.Data.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}










