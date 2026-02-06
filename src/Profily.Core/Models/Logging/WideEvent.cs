using System.Diagnostics;

namespace Profily.Core.Models.Logging;

/// <summary>
/// A wide event / canonical log line - one context-rich event per request.
/// Accumulates fields throughout the request lifecycle and is emitted once at the end of the request, containing all relevant information for monitoring and debugging.
/// </summary>
public sealed class WideEvent
{
    private readonly Dictionary<string, object?> _data = new();
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// Gets the accumulated event data for structured logging emission.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Data => _data;

    // Indexer for ad-hoc fields
    public object? this[string key]
    {
        get => _data.TryGetValue(key, out var value) ? value : null;
        set => _data[key] = value;
    }

    // Request context

    public void SetRequestContext(
        string method,
        string path,
        string requestId,
        string? userAgent = null,
        string? clientIp = null)
    {
        _data["request.method"] = method;
        _data["request.path"] = path;
        _data["request.id"] = requestId;
        if (userAgent != null)
            _data["request.user_agent"] = userAgent;
        if (clientIp != null)
            _data["request.client_ip"] = clientIp;
    }

    // User context
    public void SetUserContext(
        string? userId,
        string? username = null,
        string? email = null)
    {
        if (userId is not null) _data["user.id"] = userId;
        if (username is not null) _data["user.username"] = username;
        if (email is not null) _data["user.email"] = email;
    }

    // Business context (flexible)
    public void Set(string key, object? value)
    {
        _data[key] = value;
    }

    public void SetBusinessContext(Dictionary<string, object?> context)
    {
        foreach (var (key, value) in context)
        {
            _data[key] = value;
        }
    }

    // Error context
    public void SetErrorContext(Exception ex)
    {
        _data["error.type"] = ex.GetType().FullName;
        _data["error.message"] = ex.Message;
        if (ex.InnerException is not null)
            _data["error.inner_exception"] = ex.InnerException.GetType().FullName + ": " + ex.InnerException.Message;
    }

    public void SetErrorContext(string errorType, string errorMessage)
    {
        _data["error.type"] = errorType;
        _data["error.message"] = errorMessage;
    }

    // Infrastructure / Environment context
    public void SetEnvironmentContext(string serviceName, string? version = null, string? environment = null)
    {
        _data["service.name"] = serviceName;
        if (version != null)
            _data["service.version"] = version;
        if (environment != null)
            _data["service.environment"] = environment;
    }

    // Completion
    public void Complete(int statusCode)
    {
        stopwatch.Stop();
        _data["response.status_code"] = statusCode;
        _data["duration_ms"] = stopwatch.Elapsed.TotalMilliseconds;
        _data["outcome"] = statusCode >= 200 && statusCode < 400 ? "success" : "failure";
        _data["timestamp"] = DateTime.UtcNow.ToString("O");
    }
}