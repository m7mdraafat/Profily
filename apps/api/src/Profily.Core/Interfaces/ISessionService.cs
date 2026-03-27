using Profily.Core.Entities;

namespace Profily.Core.Interfaces;

public interface ISessionService
{
    Task<Session> CreateAsync(Guid userId, CancellationToken ct = default);
    Task<Session?> GetValidAsync(Guid sessionId, CancellationToken ct = default);
    Task ExtendAsync(Guid sessionId, CancellationToken ct = default);
    Task DeleteAsync(Guid sessionId, CancellationToken ct = default);
    Task DeleteByUserAsync(Guid userId, CancellationToken ct = default);
    Task DeleteExpiredAsync(CancellationToken ct = default);
}