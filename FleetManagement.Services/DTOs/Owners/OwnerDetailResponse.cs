namespace FleetManagement.Services.DTOs.Owners;

public sealed record OwnerDetailResponse : OwnerSummaryResponse
{
    public required string? TimeZone { get; init; }

    public required int FleetCount { get; init; }

    public string? OktaGroupId { get; init; }
}

