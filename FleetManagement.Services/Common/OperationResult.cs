namespace FleetManagement.Services.Common;

public class OperationResult
{
    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public static OperationResult Success() => new() { Succeeded = true };

    public static OperationResult Failure(string error) => new() { Succeeded = false, Error = error };
}

