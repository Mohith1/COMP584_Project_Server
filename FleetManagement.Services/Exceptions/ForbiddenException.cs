namespace FleetManagement.Services.Exceptions;

public class ForbiddenException(string message) : DomainException(message);

