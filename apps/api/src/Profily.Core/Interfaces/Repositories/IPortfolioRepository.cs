using Profily.Core.Entities;

namespace Profily.Core.Interfaces.Repositories;

public interface IPortfolioRepository : IRepository<Portfolio>
{
    Task<Portfolio?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
