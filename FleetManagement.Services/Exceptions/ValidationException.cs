namespace FleetManagement.Services.Exceptions;

public class ValidationException(IDictionary<string, string[]> errors)
    : DomainException("One or more validation errors occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;

    public static ValidationException FromMessage(string field, string message) =>
        new(new Dictionary<string, string[]>
        {
            { field, new[] { message } }
        });
}

