namespace Profily.Core.Interfaces.Repositories;

/// <summary>
/// Generic repository for common CRUD operations.
/// Specific repositories extend this with custom queries.
/// No IQueryable exposed — keeps Core persistence-ignorant.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
}
