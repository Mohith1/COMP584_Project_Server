namespace FleetManagement.Services.Options;

public sealed record JwtSettings
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string SigningKey { get; init; }

    public int AccessTokenMinutes { get; init; } = 30;

    public int RefreshTokenDays { get; init; } = 14;
}

