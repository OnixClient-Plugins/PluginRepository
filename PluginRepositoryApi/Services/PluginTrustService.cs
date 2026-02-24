using PluginRepositoryApi.Data;

namespace PluginRepositoryApi.Services;

public interface IPluginTrustService {
    Task<bool> IsPluginTrusted(string hash, string uuid);
    bool IsPluginTrusted(string uuid);
    void CacheTrustedPlugin(string hash);
    string ComputeHash(byte[] data);
    List<string> TrustedPluginUuids { get; }
    Task<bool> AddTrustedPlugin(string uuid);
    Task<bool> AddTrustedPlugin(string uuid, string hash);
    Task<bool> RemoveTrustedPlugin(string uuid);
    void ResetCache();
}

public class PluginTrustService : IPluginTrustService {
    private readonly HashSet<string> _trustedPluginHashes = new();
    private readonly HashSet<string> _untrustedPluginHashes = new();
    private readonly HashSet<string> _trustedPluginUuids;
    private readonly ReaderWriterLockSlim _trustedPluginHashesLock = new();
    private readonly ApplicationDataPaths _paths;
    private readonly SemaphoreSlim _fileAccessLock = new(1, 1);
    
    public PluginTrustService(ApplicationDataPaths paths) {
        _paths = paths;
        _trustedPluginUuids = LoadTrustedPluginUuids().Result;
    }

    public bool IsPluginTrusted(string uuid) {
        _trustedPluginHashesLock.EnterReadLock();
        if (_trustedPluginUuids.Contains(uuid)) {
            _trustedPluginHashesLock.ExitReadLock();
            return true;
        }
        _trustedPluginHashesLock.ExitReadLock();
        return false;
    }

    public async Task<bool> IsPluginTrusted(string hash, string uuid) {
        _trustedPluginHashesLock.EnterReadLock();
        if (!_trustedPluginUuids.Contains(uuid)) {
            _trustedPluginHashesLock.ExitReadLock();   
            return false;
        }
        
        if (_untrustedPluginHashes.Contains(hash)) {
            _trustedPluginHashesLock.ExitReadLock();   
            return false;
        }
        
        if (_trustedPluginHashes.Contains(hash)) {
            _trustedPluginHashesLock.ExitReadLock();   
            return true;
        }
        _trustedPluginHashesLock.ExitReadLock();
        var hashPath = Path.Combine(_paths.Plugins, uuid, "hash.txt");
        if (!File.Exists(hashPath)) {
            _trustedPluginHashesLock.EnterWriteLock();
            _untrustedPluginHashes.Add(hash);
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        var fileHash = (await File.ReadAllTextAsync(hashPath)).Trim();
        if (fileHash != hash) {
            _trustedPluginHashesLock.EnterWriteLock();
            _untrustedPluginHashes.Add(hash);
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        _trustedPluginHashesLock.EnterWriteLock();
        _trustedPluginHashes.Add(hash);
        _trustedPluginHashesLock.ExitWriteLock();
        return true;
    }
    
    public void CacheTrustedPlugin(string hash) {
        _trustedPluginHashesLock.EnterWriteLock();
        _trustedPluginHashes.Add(hash);
        _trustedPluginHashesLock.ExitWriteLock();
    }
    
    public string ComputeHash(byte[] data) {
        using var sha512 = System.Security.Cryptography.SHA512.Create();
        var hash = sha512.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    public List<string> TrustedPluginUuids {
        get {
            _trustedPluginHashesLock.EnterReadLock();
            var result = _trustedPluginUuids.ToList();
            _trustedPluginHashesLock.ExitReadLock();
            return result;
        }
    }

    public async Task<bool> AddTrustedPlugin(string uuid) {
        _trustedPluginHashesLock.EnterWriteLock();
        if (!_trustedPluginUuids.Add(uuid)) {
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        _trustedPluginHashesLock.ExitWriteLock();

        await _fileAccessLock.WaitAsync();
        try {
            await File.AppendAllTextAsync(Path.Combine(_paths.Data, "trusted_plugins_uuids.txt"), uuid + Environment.NewLine);
        }
        catch (Exception) {
            _trustedPluginHashesLock.EnterWriteLock();
            _trustedPluginUuids.Remove(uuid);
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        finally {
            _fileAccessLock.Release();
        }
        return true;
    }
    public async Task<bool> AddTrustedPlugin(string uuid, string hash) {
        _trustedPluginHashesLock.EnterWriteLock();
        _trustedPluginHashes.Add(hash);
        if (!_trustedPluginUuids.Add(uuid)) {
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        _trustedPluginHashesLock.ExitWriteLock();

        await _fileAccessLock.WaitAsync();
        try {
            await File.AppendAllTextAsync(Path.Combine(_paths.Data, "trusted_plugins_uuids.txt"), uuid + Environment.NewLine);
        }
        catch (Exception) {
            _trustedPluginHashesLock.EnterWriteLock();
            _trustedPluginUuids.Remove(uuid);
            _trustedPluginHashes.Remove(hash);
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        finally {
            _fileAccessLock.Release();
        }
        return true;
    }
    
    public async Task<bool> RemoveTrustedPlugin(string uuid) {
        _trustedPluginHashesLock.EnterWriteLock();
        if (!_trustedPluginUuids.Remove(uuid)) {
            _trustedPluginHashesLock.ExitWriteLock();
            return false;
        }
        var snapshot = _trustedPluginUuids.ToList();
        _trustedPluginHashesLock.ExitWriteLock();

        try {
            if (await SaveTrustedPluginUuids(snapshot)) {
                _trustedPluginHashesLock.EnterWriteLock();
                _trustedPluginHashes.Clear();
                _trustedPluginHashesLock.ExitWriteLock();
                return true;
            }
        }
        catch (Exception) { }

        _trustedPluginHashesLock.EnterWriteLock();
        _trustedPluginUuids.Add(uuid);
        _trustedPluginHashesLock.ExitWriteLock();
        return false;
    }

    public void ResetCache() {
        _trustedPluginHashesLock.EnterWriteLock();
        _trustedPluginUuids.Clear();
        _trustedPluginHashes.Clear();
        _trustedPluginHashesLock.ExitWriteLock();
    }

    private async Task<HashSet<string>> LoadTrustedPluginUuids() {
        string path = Path.Combine(_paths.Data, "trusted_plugins_uuids.txt");
        if (!File.Exists(path)) {
            return new HashSet<string>();
        }

        await _fileAccessLock.WaitAsync();
        try {
            var lines = await File.ReadAllLinesAsync(path);
            return new HashSet<string>(lines.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        catch (Exception) { }
        finally {
            _fileAccessLock.Release();
        }
        return new HashSet<string>();
    }
    
    private async Task<bool> SaveTrustedPluginUuids(List<string> snapshot) {
        await _fileAccessLock.WaitAsync();
        try {
            await File.WriteAllLinesAsync(Path.Combine(_paths.Data, "trusted_plugins_uuids.txt"), snapshot);
        }
        catch (Exception) {
            return false;
        }
        finally {
            _fileAccessLock.Release();
        }

        return true;
    }
}