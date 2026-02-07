// src/Profily.Infrastructure/Services/FrameworkMappings.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using Profily.Core.Models.TechStack;

namespace Profily.Infrastructure.Services;

/// <summary>
/// Loaded once at startup from framework-mappings.json.
/// Provides lookup tables for each dependency parser.
/// </summary>
public sealed class FrameworkMappings
{
    public Dictionary<string, TechMapping> PackageJson { get; init; } = new();
    public Dictionary<string, TechMapping> Csproj { get; init; } = new();
    public Dictionary<string, TechMapping> Requirements { get; init; } = new();
    public Dictionary<string, TechMapping> GoMod { get; init; } = new();
    public Dictionary<string, TechMapping> CargoToml { get; init; } = new();
    public Dictionary<string, TechMapping> PomXml { get; init; } = new();
    public Dictionary<string, TechMapping> FilePresence { get; init; } = new();
    public Dictionary<string, TechMapping> TopicMappings { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All known technology names (lowercase) for README badge matching.
    /// </summary>
    public HashSet<string> AllKnownNames { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps lowercase variants to canonical name (e.g., "reactjs" → "React").
    /// </summary>
    public Dictionary<string, string> NameNormalization { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Reverse lookup: canonical tech name → (Category, Icon).
    /// Used by README badge detection to resolve the correct category instead of guessing.
    /// </summary>
    public Dictionary<string, TechMapping> TechInfoByName { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

    public void BuildLookups()
    {
        var allMappings = PackageJson.Values
            .Concat(Csproj.Values)
            .Concat(Requirements.Values)
            .Concat(GoMod.Values)
            .Concat(CargoToml.Values)
            .Concat(PomXml.Values)
            .Concat(FilePresence.Values)
            .Concat(TopicMappings.Values);

        foreach (var mapping in allMappings)
        {
            var lower = mapping.Name.ToLowerInvariant();
            AllKnownNames.Add(lower);
            NameNormalization.TryAdd(lower, mapping.Name);
            TechInfoByName.TryAdd(mapping.Name, mapping);
        }
    }

    public static FrameworkMappings LoadFromEmbeddedResource()
    {
        var assembly = typeof(FrameworkMappings).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("framework-mappings.json"));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("framework-mappings.json embedded resource not found");

        var raw = JsonSerializer.Deserialize<FrameworkMappingsRaw>(stream)
            ?? throw new InvalidOperationException("Failed to deserialize framework-mappings.json");

        var mappings = new FrameworkMappings
        {
            PackageJson = raw.PackageJson ?? new(),
            Csproj = raw.Csproj ?? new(),
            Requirements = raw.Requirements ?? new(),
            GoMod = raw.GoMod ?? new(),
            CargoToml = raw.CargoToml ?? new(),
            PomXml = raw.PomXml ?? new(),
            FilePresence = raw.FilePresence ?? new(),
            TopicMappings = raw.TopicMappings ?? new(StringComparer.OrdinalIgnoreCase)
        };

        mappings.BuildLookups();
        return mappings;
    }

    /// <summary>
    /// Raw deserialization shape matching the JSON file.
    /// </summary>
    private sealed class FrameworkMappingsRaw
    {
        [JsonPropertyName("packageJson")]
        public Dictionary<string, TechMapping>? PackageJson { get; set; }
        [JsonPropertyName("csproj")]
        public Dictionary<string, TechMapping>? Csproj { get; set; }
        [JsonPropertyName("requirements")]
        public Dictionary<string, TechMapping>? Requirements { get; set; }
        [JsonPropertyName("goMod")]
        public Dictionary<string, TechMapping>? GoMod { get; set; }
        [JsonPropertyName("cargoToml")]
        public Dictionary<string, TechMapping>? CargoToml { get; set; }
        [JsonPropertyName("pomXml")]
        public Dictionary<string, TechMapping>? PomXml { get; set; }
        [JsonPropertyName("filePresence")]
        public Dictionary<string, TechMapping>? FilePresence { get; set; }
        [JsonPropertyName("topicMappings")]
        public Dictionary<string, TechMapping>? TopicMappings { get; set; }
    }
}

public sealed class TechMapping
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("category")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required TechCategory Category { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}