namespace Profily.Core.Entities;

public sealed class FeatureFlag
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
}
