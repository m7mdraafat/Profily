namespace Profily.Core.Interfaces;

public interface IProjectSyncService
{
    Task<SyncResult> SyncAsync(Guid userId, string accessToken, CancellationToken ct = default);
}

public sealed class SyncResult
{
    public int NewRepos { get; set; }
    public int UpdatedRepos { get; set; }
    public int TotalRepos { get; set; }
}