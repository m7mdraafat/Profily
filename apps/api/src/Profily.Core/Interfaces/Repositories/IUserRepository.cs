using Profily.Core.Entities;

namespace Profily.Core.Interfaces.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByGitHubIdAsync(long githubId, CancellationToken ct = default);
}
