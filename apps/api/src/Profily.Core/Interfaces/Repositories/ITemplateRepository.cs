using Profily.Core.Entities;

namespace Profily.Core.Interfaces.Repositories;

public interface ITemplateRepository
{
    Task<List<Template>> GetAllActiveAsync(CancellationToken ct = default);
    Task<Template?> GetByIdAsync(string id, CancellationToken ct = default);
}
