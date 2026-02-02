namespace Profily.Core.Models.Auth;

/// <summary>
/// Response returned when getting current user info.
/// </summary>
public sealed record UserInfoResponse
{
    public required string Id { get; init; }
    public required string GitHubUsername { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// Response returned after successful authentication.
/// </summary>
public sealed record AuthResponse
{
    public required bool IsAuthenticated { get; init; }
    public UserInfoResponse? User { get; init; }
}

/// <summary>
/// Standard error response for API errors.
/// </summary>
public sealed record ErrorResponse
{
    public required string Error { get; init; }
    public string? Message { get; init; }
    public string? TraceId { get; init; }
}