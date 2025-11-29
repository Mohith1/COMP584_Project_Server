using FleetManagement.Data.Common;

namespace FleetManagement.Data.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public required string Token { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTimeOffset.UtcNow <= ExpiresAtUtc;
}

