namespace PluginRepositoryApi.Data;

public class ApplicationDataPaths {
    public string Data { get; }
    public string Plugins { get; }
    public string Runtimes { get; }
    public string PluginSources { get; }

    public ApplicationDataPaths(string dataFolder) {
        Data = Path.GetFullPath(dataFolder);
        Plugins = Path.Combine(Data, "plugins");
        Runtimes = Path.Combine(Data, "runtimes");
        PluginSources = Path.Combine(Data, "plugin_sources");
        if (!Directory.Exists(Plugins)) {
            Directory.CreateDirectory(Plugins);
        }
        if (!Directory.Exists(Runtimes)) {
            Directory.CreateDirectory(Runtimes);
        }
        if (!Directory.Exists(PluginSources)) {
            Directory.CreateDirectory(PluginSources);
        }
    }
    
}