namespace FleetManagement.Services.Exceptions;

public class NotFoundException(string resource, string identifier)
    : DomainException($"{resource} with identifier '{identifier}' was not found.");

