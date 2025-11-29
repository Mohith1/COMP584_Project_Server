namespace FleetManagement.Services.Abstractions;

public interface ISeedService
{
    Task<int> SeedCitiesAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

