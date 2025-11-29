using Microsoft.AspNetCore.Identity;

namespace FleetManagement.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Owner? OwnerProfile { get; set; }

    public string? OktaUserId { get; set; }

    public DateTimeOffset? LastLoginUtc { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

