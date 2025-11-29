using FleetManagement.Data.Common;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace FleetManagement.Data.Repositories;

public class GenericRepository<TEntity>(FleetDbContext context) : IGenericRepository<TEntity> where TEntity : BaseEntity
{
    private readonly FleetDbContext _context = context;

    public IQueryable<TEntity> Queryable => _context.Set<TEntity>().AsQueryable();

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<TEntity>> GetAsync(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Set<TEntity>().Where(predicate).ToListAsync(cancellationToken);
        return entities;
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        _context.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => _context.Set<TEntity>().Update(entity);

    public void Remove(TEntity entity) => _context.Set<TEntity>().Remove(entity);
}

