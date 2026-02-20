namespace Profily.Core.Exceptions;

/// <summary>
/// Base exception for all domain-specific errors in Profily.
/// Error handling middleware catches this and maps to HTTP responses.
/// 
/// Carries a machine-readable Code (for frontend switch/case)
/// and a human-readable Message (for UI display).
/// Does NOT carry HTTP status — that's the API layer's concern.
/// </summary>
public abstract class ProfilyException : Exception
{
    /// <summary>
    /// Machine-readable error code (e.g. "NOT_FOUND", "VALIDATION_ERROR").
    /// Frontend uses this to decide what to show — never the message text.
    /// </summary>
    public string Code { get; }

    protected ProfilyException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    protected ProfilyException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
