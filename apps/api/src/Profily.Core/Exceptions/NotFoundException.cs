namespace Profily.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object id)
        : base($"{entity} with id '{id}' was not found") { }
}
