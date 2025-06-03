using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;

namespace PluginRepositoryApi.Services;

public interface ITrustedPluginSourcesService {
    IReadOnlyList<string> TrustedPluginSources { get; }
    Task AddTrustedSource(string pluginUuid);
    Task<bool> RemoveTrustedSource(string id);
    public bool IsPluginTrusted(string uuid);
}

public class TrustedPluginSourcesService : ITrustedPluginSourcesService {
    private readonly ILogger<TrustedPluginSourcesService> _logger;
    private readonly ApplicationDataPaths _paths;
    private List<string> _trustedPluginSources = new();
    private readonly ReaderWriterLockSlim _trustedPluginSourcesLock = new();

    public TrustedPluginSourcesService(ILogger<TrustedPluginSourcesService> logger, ApplicationDataPaths paths) {
        _logger = logger;
        _paths = paths;

        _trustedPluginSources = LoadPluginSources().Result.ToList();
    }

    public IReadOnlyList<string> TrustedPluginSources {
        get {
            _trustedPluginSourcesLock.EnterReadLock();
            var result = _trustedPluginSources;
            _trustedPluginSourcesLock.ExitReadLock();
            return result;
        }
    }

    public bool IsPluginTrusted(string uuid) {
        return TrustedPluginSources.Contains(uuid);
    }
    
    public async Task AddTrustedSource(string pluginUuid) {
        if (pluginUuid.Length > 24) throw new InvalidDataException("Plugin ID is too long");
        _trustedPluginSourcesLock.EnterReadLock();
        var newList = _trustedPluginSources.ToList();
        _trustedPluginSourcesLock.ExitReadLock();
        if (newList.Contains(pluginUuid)) return;
        newList.Add(pluginUuid);
        
        _trustedPluginSourcesLock.EnterWriteLock();
        _trustedPluginSources = newList;
        _trustedPluginSourcesLock.ExitWriteLock();
        await SaveTrustedSources();
    }


    public async Task<bool> RemoveTrustedSource(string id) {
        _trustedPluginSourcesLock.EnterReadLock();
        var newList = _trustedPluginSources.ToList();
        _trustedPluginSourcesLock.ExitReadLock();
        bool removed = newList.Remove(id);
        
        _trustedPluginSourcesLock.EnterWriteLock();
        _trustedPluginSources = newList;
        _trustedPluginSourcesLock.ExitWriteLock();
        if (removed) {
            await SaveTrustedSources();
            return true;
        }

        return false;
    }

    private async Task SaveTrustedSources() {
        try {
            _trustedPluginSourcesLock.EnterReadLock();
            var developers = _trustedPluginSources;
            _trustedPluginSourcesLock.ExitReadLock();
            await File.WriteAllLinesAsync(Path.Combine(_paths.Data, "trusted_developers.txt"), developers);
        }
        catch (Exception e) {
            _logger.LogError(e, "Failed to save trusted developers");
        }
    }
    
    private async Task<string[]> LoadPluginSources() {
        try {
            return await File.ReadAllLinesAsync(Path.Combine(_paths.Data, "trusted_developers.txt"));
        }
        catch (FileNotFoundException) {
            return await Task.FromResult(Array.Empty<string>());
        }
        catch (Exception e) {
            _logger.LogError(e, "Failed to load trusted developers");
            return await Task.FromResult(Array.Empty<string>());
        }
    }
}