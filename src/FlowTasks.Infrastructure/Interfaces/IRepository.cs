using System.Linq.Expressions;

namespace FlowTasks.Infrastructure.Interfaces;

public interface IRepository<T> where T : class
{
    // Query methods
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    // Query with includes
    IQueryable<T> Query();
    IQueryable<T> Query(Expression<Func<T, bool>> predicate);

    // Command methods
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
    Task DeleteAsync(object id, CancellationToken cancellationToken = default);
}

