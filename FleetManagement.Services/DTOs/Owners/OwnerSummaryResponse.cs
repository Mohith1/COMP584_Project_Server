namespace FleetManagement.Services.DTOs.Owners;

public record OwnerSummaryResponse
{
    public required Guid Id { get; init; }

    public required string CompanyName { get; init; }

    public required string ContactEmail { get; init; }

    public string? ContactPhone { get; init; }

    public string? City { get; init; }

    public string? Country { get; init; }
}

