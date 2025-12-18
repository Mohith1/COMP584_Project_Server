using Microsoft.AspNetCore.Identity;

namespace FleetManagement.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? OktaUserId { get; set; }
    public DateTimeOffset? LastLoginUtc { get; set; }
}













