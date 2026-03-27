using Microsoft.EntityFrameworkCore;
using Profily.Core.Entities;
using Profily.Core.Interfaces;
using Profily.Infrastructure.Data;

namespace Profily.Infrastructure.Services;

public sealed class SessionService : ISessionService
{
    private readonly ProfilyDbContext _dbContext;
    private static readonly TimeSpan _sessionDuration = TimeSpan.FromDays(7);

    public SessionService(ProfilyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Session> CreateAsync(Guid userId, CancellationToken ct = default)
    {
        var session = new Session
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(_sessionDuration),
            LastAccessedAt = DateTime.UtcNow
        };

        _dbContext.Sessions.Add(session);
        await _dbContext.SaveChangesAsync(ct);

        return session;
    }

    public async Task<Session?> GetValidAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId && s.ExpiresAt > DateTime.UtcNow, ct);

        return session;
    }

    public async Task ExtendAsync(Guid sessionId, CancellationToken ct = default)
    {
        var newExpiry = DateTime.UtcNow.Add(_sessionDuration);
        var now = DateTime.UtcNow;

        await _dbContext.Sessions
            .Where(s => s.Id == sessionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ExpiresAt, newExpiry)
                .SetProperty(x => x.LastAccessedAt, now), ct);
    }
    public async Task DeleteAsync(Guid sessionId, CancellationToken ct = default)
    {
        await _dbContext.Sessions
            .Where(s => s.Id == sessionId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task DeleteByUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _dbContext.Sessions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(ct);
    }
    public async Task DeleteExpiredAsync(CancellationToken ct = default)
    {
        await _dbContext.Sessions
            .Where(s => s.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync(ct);
    }
}