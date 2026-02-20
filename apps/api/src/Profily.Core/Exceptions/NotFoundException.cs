namespace Profily.Core.Exceptions;

/// <summary>
/// Thrown when a requested entity does not exist.
/// Middleware maps this to HTTP 404.
/// </summary>
public sealed class NotFoundException : ProfilyException
{
    public string Entity { get; }
    public object EntityId { get; }

    public NotFoundException(string entity, object id)
        : base("NOT_FOUND", $"{entity} with id '{id}' not found")
    {
        Entity = entity;
        EntityId = id;
    }
}
