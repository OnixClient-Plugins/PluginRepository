using System.Text.Json.Serialization;

namespace PluginRepositoryApi.Data.Plugins;

public class PluginCompilationResult {
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("build_log")]
    public string BuildLog { get; set; } = string.Empty;
    
    [JsonPropertyName("main_assembly_hash")]
    public string MainAssemblyHash { get; set; } = string.Empty;
    
    [JsonIgnore]
    public string IconPath { get; set; } = string.Empty;
    [JsonIgnore]
    public string BannerPath { get; set; } = string.Empty;
    [JsonIgnore]
    public string AssetsPath { get; set; } = string.Empty;
    
    [JsonIgnore]
    public byte[] ZippedPlugin { get; set; } = Array.Empty<byte>();
    
    [JsonPropertyName("manifest")]
    public PluginManifest? Manifest { get; set; }
    public PluginCompilationResult(bool success, string buildLog, string? mainAssemblyPath = null, PluginManifest? pluginManifest = null) {
        Success = success;
        BuildLog = buildLog;
        MainAssemblyHash = mainAssemblyPath ?? string.Empty;
        Manifest = pluginManifest;
    }
}