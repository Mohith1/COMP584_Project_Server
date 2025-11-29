namespace FleetManagement.Services.Options;

public sealed record OktaSettings
{
    public required string Domain { get; init; }

    public required string ApiToken { get; init; }

    public required string AuthorizationServerId { get; init; }

    public required string Audience { get; init; }

    public string DefaultOwnerGroupId { get; init; } = string.Empty;
}

