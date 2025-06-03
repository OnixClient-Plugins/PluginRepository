using System.Text.Json.Serialization;

namespace PluginRepositoryApi.Data.Plugins;

public class PluginManifest {
    [JsonPropertyName("uuid")]
    // ReSharper disable once InconsistentNaming
    public required string UUID { get; init; }
    [JsonPropertyName("plugin_name")]
    public required string Name { get; init; }
    [JsonPropertyName("plugin_author")]
    public required string Author { get; init; }
    [JsonPropertyName("plugin_description")]
    public required string Description { get; init; }
    [JsonPropertyName("plugin_version")]
    public required string Version { get; init; }
    [JsonPropertyName("game_version")]
    public required string GameVersion { get; init; }
    [JsonPropertyName("runtime_version")]
    public required int RuntimeVersion { get; init; }
    [JsonPropertyName("target_assembly")]
    public required string TargetAssembly { get; init; }
    [JsonPropertyName("repository_url")]
    public string? RepositoryLink { get; init; }

    [JsonPropertyName("categories")] 
    public string[] Categories { get; init; } = Array.Empty<string>();
    [JsonPropertyName("supported_game_version_ranges")]
    public string[] SupportedGameVersionRanges { get; init; } = Array.Empty<string>();
    
    
    public PluginManifest() {}

    public PluginManifest Copy() => Copy(RuntimeVersion);
    public PluginManifest Copy(int runtimeVersion) {
        return new PluginManifest() {
            UUID = UUID,
            Name = Name,
            Author = Author,
            Description = Description,
            Version = Version,
            GameVersion = GameVersion,
            RuntimeVersion = runtimeVersion,
            TargetAssembly = TargetAssembly,
            RepositoryLink = RepositoryLink,
            Categories = Categories.ToArray(),
            SupportedGameVersionRanges = SupportedGameVersionRanges.ToArray(),
            
        };
    }
}