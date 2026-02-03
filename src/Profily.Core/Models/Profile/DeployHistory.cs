using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Profily.Core.Models.Profile;

/// <summary>
/// Immutable record of a deploy operation.
/// One record created per deploy, never updated.
/// 
/// Design notes:
/// - CofigSnapshot enables rollback/comparison.
/// - Stores both success and failure for debugging
/// - Partition key is userId for efficient history queries.
/// </summary>
public sealed class DeployHistory : CosmosDocument
{
    [JsonIgnore]
    public const string DocumentType = "deployHistory";

    [JsonPropertyName("type")]
    public override string Type => DocumentType;

    /// <summary>
    /// Snapshot of profileConfig at deploy time.
    /// Enables exact recreation of deployed READEME.
    /// </summary>
    [JsonPropertyName("configSnapshot")]
    [Required]
    public required JsonDocument ConfigSnapshot { get; set; }

    /// <summary>
    /// Template used (if any). For analytics.
    /// </summary>
    [JsonPropertyName("templateId")]
    public string? TemplateId { get; set; }

    /// <summary>
    /// Outcome of the deploy operation.
    /// </summary>
    [JsonPropertyName("result")]
    [Required]
    public required DeployResult Result { get; set; }

    public static DeployHistory Create(string userId, ProfileConfig config, DeployResult result) => new()
    {
        Id = $"{DocumentType}-{userId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}",
        UserId = userId,
        ConfigSnapshot = JsonSerializer.SerializeToDocument(config),
        TemplateId = config.TemplateId,
        Result = result
    };
}

/// <summary>
/// Result of a deploy operation.
/// Immutable record - results doesn't change after deploy.
/// </summary>
public sealed record DeployResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    /// <summary>
    /// Error message if deploy failed. Null on success
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Git commit SHA if deploy succeeded.
    /// Enables direct link to commit.
    /// </summary>
    [JsonPropertyName("commitSha")]
    public string? CommitSha { get; init; }


    /// <summary>
    /// URL to deployed repository.
    /// </summary>
    [JsonPropertyName("repoUrl")]
    public string? RepoUrl { get; init; }

    /// <summary>
    /// List of files created/updated in this deploy.
    /// </summary>
    [JsonPropertyName("filesDeployed")]
    public List<string>? FilesDeployed { get; init; }

    public static DeployResult Successful(string commitSha, string repoUrl, List<string> filesDeployed) => new()
    {
        Success = true,
        CommitSha = commitSha,
        RepoUrl = repoUrl,
        FilesDeployed = filesDeployed
    };

    public static DeployResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}