namespace FleetManagement.Services.Abstractions;

public interface IOktaIntegrationService
{
    Task<string?> ProvisionUserAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);

    Task<string?> EnsureOwnerGroupAsync(string companyName, CancellationToken cancellationToken = default);
}

