using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;

namespace PluginRepositoryApi.Services;

public interface ITrustedDeveloperService {
    IReadOnlyList<string> TrustedDevelopers { get; }
    Task AddDeveloper(string id);
    Task<bool> RemoveDeveloper(string id);
}

public class TrustedDeveloperService : ITrustedDeveloperService {
    private ILogger<TrustedDeveloperService> _logger;
    private ApplicationDataPaths _paths;
    private List<string> _trustedDevelopers = new();
    private ReaderWriterLockSlim _trustedDevelopersLock = new();

    public TrustedDeveloperService(ILogger<TrustedDeveloperService> logger, ApplicationDataPaths paths) {
        _logger = logger;
        _paths = paths;

        _trustedDevelopers = LoadDevelopers().Result.ToList();
    }

    public IReadOnlyList<string> TrustedDevelopers {
        get {
            _trustedDevelopersLock.EnterReadLock();
            var result = _trustedDevelopers;
            _trustedDevelopersLock.ExitReadLock();
            return result;
        }
    }
    
    public async Task AddDeveloper(string id) {
        if (id.Length > 24) throw new InvalidDataException("Developer ID is too long");
        _trustedDevelopersLock.EnterReadLock();
        var newList = _trustedDevelopers.ToList();
        _trustedDevelopersLock.ExitReadLock();
        if (newList.Contains(id)) return;
        newList.Add(id);
        
        _trustedDevelopersLock.EnterWriteLock();
        _trustedDevelopers = newList;
        _trustedDevelopersLock.ExitWriteLock();
        await SaveDevelopers();
    }


    public async Task<bool> RemoveDeveloper(string id) {
        _trustedDevelopersLock.EnterReadLock();
        var newList = _trustedDevelopers.ToList();
        _trustedDevelopersLock.ExitReadLock();
        bool removed = newList.Remove(id);
        
        _trustedDevelopersLock.EnterWriteLock();
        _trustedDevelopers = newList;
        _trustedDevelopersLock.ExitWriteLock();
        if (removed) {
            await SaveDevelopers();
            return true;
        }

        return false;
    }

    private async Task SaveDevelopers() {
        try {
            _trustedDevelopersLock.EnterReadLock();
            var developers = _trustedDevelopers;
            _trustedDevelopersLock.ExitReadLock();
            await File.WriteAllLinesAsync(Path.Combine(_paths.Data, "trusted_developers.txt"), developers);
        }
        catch (Exception e) {
            _logger.LogError(e, "Failed to save trusted developers");
        }
    }
    
    private async Task<string[]> LoadDevelopers() {
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