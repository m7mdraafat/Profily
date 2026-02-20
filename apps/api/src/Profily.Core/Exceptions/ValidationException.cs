namespace Profily.Core.Exceptions;

/// <summary>
/// Thrown when request data fails validation.
/// Middleware maps this to HTTP 400.
/// FieldErrors provides per-field error messages for the frontend.
/// </summary>
public sealed class ValidationException : ProfilyException
{
    public IReadOnlyDictionary<string, string> FieldErrors { get; }

    public ValidationException(string message)
        : base("VALIDATION_ERROR", message)
    {
        FieldErrors = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    public ValidationException(Dictionary<string, string> fieldErrors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred")
    {
        FieldErrors = fieldErrors;
    }
}
