using System.Text.Json.Serialization;

namespace PluginRepositoryApi.Data.Plugins;

public class OnixPlugin {
    [JsonPropertyName("manifest")]
    public required PluginManifest Manifest { get; set; }
    [JsonPropertyName("last_updated")]
    public required DateTime LastUpdated { get; set; }
    [JsonPropertyName("download_count")]
    public int DownloadCount { get; set; } = 0;
    
}