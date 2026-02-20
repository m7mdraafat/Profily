namespace Profily.Core.Exceptions;

/// <summary>
/// Thrown when an operation conflicts with the current state.
/// Example: creating a portfolio when one already exists.
/// Middleware maps this to HTTP 409.
/// </summary>
public sealed class ConflictException : ProfilyException
{
    public ConflictException(string message)
        : base("CONFLICT", message) { }
}
