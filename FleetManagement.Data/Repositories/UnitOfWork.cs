using FleetManagement.Data.Common;

namespace FleetManagement.Data.Repositories;

public class UnitOfWork(FleetDbContext context) : IUnitOfWork
{
    private readonly FleetDbContext _context = context;
    private readonly Dictionary<Type, object> _repositories = new();

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
    {
        var type = typeof(TEntity);
        if (_repositories.TryGetValue(type, out var repository))
        {
            return (IGenericRepository<TEntity>)repository;
        }

        var newRepository = new GenericRepository<TEntity>(_context);
        _repositories[type] = newRepository;
        return newRepository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

