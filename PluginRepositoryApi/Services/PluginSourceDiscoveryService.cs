using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Data.Plugins;

namespace PluginRepositoryApi.Services;

public interface IPluginSourceDiscoveryService {
    void ReloadPluginSources();
    bool ReloadPluginSource(string uuid);
    event PluginSourceDiscoveryService.OnSourceRemovedDelegate? OnSourceRemoved;
    event PluginSourceDiscoveryService.OnSourceUpdatedDelegate? OnSourceUpdated;
    bool ReloadPluginSourceDirectory(string solutionDirectory);
    IReadOnlyList<PluginSourcePaths> PluginSources { get; }
}

public class PluginSourceDiscoveryService : IPluginSourceDiscoveryService {
    private readonly ILogger<PluginSourceDiscoveryService> _logger;
    private readonly ApplicationDataPaths _paths;
    
    public PluginSourceDiscoveryService(ILogger<PluginSourceDiscoveryService> logger, ApplicationDataPaths paths) {
        _logger = logger;
        _paths = paths;

        ReloadPluginSources();
    }
    
    private List<PluginSourcePaths> _pluginSources = new();
    private Dictionary<string, PluginSourcePaths> _uuidToSources = new();
    private ReaderWriterLockSlim _pluginSourcesLock = new ReaderWriterLockSlim();

    public void ReloadPluginSources() {
        List<PluginSourcePaths> newPluginSources = new();
        Dictionary<string, PluginSourcePaths> newUuidToSource = new();
        
        foreach (string sourceDir in Directory.EnumerateDirectories(_paths.PluginSources)) {
            if (sourceDir.EndsWith(".git") || sourceDir.EndsWith(".vs") || sourceDir.EndsWith(".idea")) continue;
            PluginSourcePaths? sourcePaths = PluginSourcePaths.TryReadSourcePaths(sourceDir, out string? pluginValidationError);
            if (sourcePaths == null) {
                _logger.LogError("Plugin source could not be read: " + pluginValidationError);
                continue;
            }
            newPluginSources.Add(sourcePaths);
            newUuidToSource[sourcePaths.Manifest.UUID] = sourcePaths;
        }
        
        
        _pluginSourcesLock.EnterReadLock();
        var deletedSources = _pluginSources.Where(x => !newPluginSources.Any(y => y.Manifest.UUID == x.Manifest.UUID)).ToList();
        _pluginSourcesLock.ExitReadLock();
        foreach (var deletedSource in deletedSources) {
            OnSourceRemoved?.Invoke(deletedSource);
        }
        foreach (var newSource in newPluginSources) {
            _pluginSourcesLock.EnterReadLock();
            PluginSourcePaths? oldSource = _pluginSources.Find(x => x.SolutionDirectory == newSource.SolutionDirectory);
            _pluginSourcesLock.ExitReadLock();
            if (oldSource == null || oldSource.Manifest.Version != newSource.Manifest.Version) {
                OnSourceUpdated?.Invoke(newSource);
            }
        }
        _pluginSourcesLock.EnterWriteLock();
        _pluginSources = newPluginSources;
        _uuidToSources = newUuidToSource;
        _pluginSourcesLock.ExitWriteLock();
    }

    public bool ReloadPluginSource(string uuid) {
        _pluginSourcesLock.EnterReadLock();
        PluginSourcePaths? sourcePaths = _uuidToSources.GetValueOrDefault(uuid); 
        _pluginSourcesLock.ExitReadLock();
        if (sourcePaths == null) {
            return false;
        }

        return ReloadPluginSourceDirectory(sourcePaths.SolutionDirectory);
    }
    
    public delegate void OnSourceRemovedDelegate(PluginSourcePaths source);
    public event OnSourceRemovedDelegate? OnSourceRemoved;
    public delegate void OnSourceUpdatedDelegate(PluginSourcePaths source);
    public event OnSourceUpdatedDelegate? OnSourceUpdated;

    public bool ReloadPluginSourceDirectory(string solutionDirectory) {
        if (solutionDirectory.EndsWith(".git") || solutionDirectory.EndsWith(".vs") || solutionDirectory.EndsWith(".idea")) return false;
        _pluginSourcesLock.EnterReadLock();
        PluginSourcePaths? sourcePaths = _pluginSources.Find(x => x.SolutionDirectory == solutionDirectory); 
        _pluginSourcesLock.ExitReadLock();
        if (sourcePaths != null) {
            _pluginSourcesLock.EnterWriteLock();
            _pluginSources.Remove(sourcePaths);
            _uuidToSources.Remove(sourcePaths.Manifest.UUID);
            _pluginSourcesLock.ExitWriteLock();
        }
        
        // no longer exists?
        if (!Directory.Exists(solutionDirectory)) {
            if (sourcePaths != null) {
                OnSourceRemoved?.Invoke(sourcePaths);
            }
            return true;
        }
        
        PluginSourcePaths? readSourcePaths = PluginSourcePaths.TryReadSourcePaths(solutionDirectory, out string? pluginValidationError);
        if (readSourcePaths == null) {
            _logger.LogError("Plugin source could not be read: " + pluginValidationError);
            return false;
        }
        
        OnSourceUpdated?.Invoke(readSourcePaths);
        if (sourcePaths is null || sourcePaths.Manifest.Version != readSourcePaths.Manifest.Version) {
            OnSourceUpdated?.Invoke(readSourcePaths);
        }
        
        return true;
    }
    
    public IReadOnlyList<PluginSourcePaths> PluginSources { 
        get {
            _pluginSourcesLock.EnterReadLock();
            List<PluginSourcePaths> result = _pluginSources;
            _pluginSourcesLock.ExitReadLock();
            return result.AsReadOnly();
        }
    }

}