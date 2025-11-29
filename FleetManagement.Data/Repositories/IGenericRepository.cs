using System.Linq.Expressions;
using FleetManagement.Data.Common;

namespace FleetManagement.Data.Repositories;

public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    IQueryable<TEntity> Queryable { get; }

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TEntity>> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}

