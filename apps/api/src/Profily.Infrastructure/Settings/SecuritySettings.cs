namespace Profily.Infrastructure.Settings;

public sealed class SecuritySettings
{
    public const string SectionName = "Security";
    public string TokenEncryptionKey { get; set; } = string.Empty;
}
