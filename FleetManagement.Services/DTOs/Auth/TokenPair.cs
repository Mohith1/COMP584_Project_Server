namespace FleetManagement.Services.DTOs.Auth;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);

