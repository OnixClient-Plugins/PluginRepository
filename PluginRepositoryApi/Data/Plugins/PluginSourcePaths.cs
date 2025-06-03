using System.Text.Json;

namespace PluginRepositoryApi.Data.Plugins;

public class PluginSourcePaths {
    public string SolutionDirectory { get; set; } = string.Empty;
    public string ProjectDirectory { get; set; } = string.Empty;
    public PluginManifest Manifest { get; set; }
    public string ManifestDirectory => Path.Combine(ProjectDirectory, "manifest.json");
    public string AssetsDirectory => Path.Combine(ProjectDirectory, "Assets");

    public PluginSourcePaths(string solutionDirectory, string pluginDirectory, PluginManifest manifest) {
        SolutionDirectory = solutionDirectory;
        ProjectDirectory = pluginDirectory;
        Manifest = manifest;
    }
    
    public void RefreshManifest() {
        string manifestPath = Path.Combine(ProjectDirectory, "manifest.json");
        if (File.Exists(manifestPath)) {
            Manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath)) 
                       ?? throw new InvalidOperationException("Failed to deserialize manifest.json");
        } else {
            throw new FileNotFoundException("Manifest file not found", manifestPath);
        }
    }
    
    public static PluginSourcePaths? TryReadSourcePaths(string solutionDirectory, out string? error) {
        if (solutionDirectory.EndsWith(".git") || solutionDirectory.EndsWith(".vs") || solutionDirectory.EndsWith(".idea")) {
            error = "Just a git, vs or idea directory, not a solution directory.";
            return null;
        }
        solutionDirectory = Path.GetFullPath(solutionDirectory);
        string? projectDir;
        foreach (var project in Directory.GetDirectories(solutionDirectory)) {
            try {
                string manifestPath = Path.Combine(project, "manifest.json");
                if (File.Exists(manifestPath)) {
                    projectDir = project;
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(File.ReadAllText(manifestPath));
                    if (manifest == null) {
                        error = "Failed to read manifest.json at " + manifestPath;
                        return null;
                    }

                    error = null;
                    return new PluginSourcePaths(solutionDirectory, projectDir, manifest);
                }
            } catch (Exception e) {
                error = "Failed to read project at " + project + ": " + e.Message;
            }
        }

        error = "No valid project found inside the solution.";
        return null;
    }
}