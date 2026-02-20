using Profily.Core.Entities;

namespace Profily.Core.Interfaces.Repositories;

public interface IProjectRepository : IRepository<Project>
{
    Task<List<Project>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task UpsertManyAsync(Guid userId, List<Project> projects, CancellationToken ct = default);
}
