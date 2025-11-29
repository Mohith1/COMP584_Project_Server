using System.ComponentModel.DataAnnotations;

namespace FleetManagement.Data.Common;

public abstract class BaseEntity : ISoftDeletable
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    [Required]
    public bool IsDeleted { get; set; }
}

