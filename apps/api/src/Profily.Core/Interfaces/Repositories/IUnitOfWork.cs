namespace Profily.Core.Interfaces.Repositories;

/// <summary>
/// Groups multiple repository operations into a single database transaction.
/// Call SaveChangesAsync once after all repo operations to commit atomically.
/// 
/// Without this, each repo.Add/Update calls SaveChanges independently —
/// if the second fails, the first is already committed (data inconsistency).
/// 
/// Example:
///   await userRepo.UpdateAsync(user);         // stages change
///   await projectRepo.UpsertManyAsync(projects); // stages change
///   await unitOfWork.SaveChangesAsync();       // commits both atomically
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct = default);
}
