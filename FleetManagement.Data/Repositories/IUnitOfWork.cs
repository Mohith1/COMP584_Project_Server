using FleetManagement.Data.Common;

namespace FleetManagement.Data.Repositories;

public interface IUnitOfWork
{
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

