using System.IO.Compression;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;

namespace PluginRepositoryApi.Services;


public interface IOnixRuntimesService {
    int LatestRuntimeId { get; }
    string GetRuntimeFolder(int runtimeId);
    List<int> AvailableRuntimes { get; }
    Task<string?> GetOrGenerateRuntimeZipPath(int runtimeId);
    public byte[] LatestRuntimeLoaderBytes { get; }
    void ReloadAvailableRuntimes();
}

public class OnixRuntimesService : IOnixRuntimesService {
    private readonly ILogger<OnixRuntimesService> _logger;
    private readonly ApplicationDataPaths _paths;
    private readonly FileSystemWatcher _runtimeFilesystemWatcher;
    
    private readonly Dictionary<string, Task<string>> _pendingRuntimeZipGenerations = new();
    private readonly object _pendingRuntimeZipGenerationsLock = new();

    private readonly ReaderWriterLockSlim _availableRuntimesLock = new ReaderWriterLockSlim();
    private Dictionary<int, bool> _availableRuntimes = new();
    private int _latestRuntimeId;
    
    private byte[] _latestRuntimeLoaderBytes = Array.Empty<byte>();

    public int LatestRuntimeId {
        get {
            _availableRuntimesLock.EnterReadLock();
            int result = _latestRuntimeId;
            _availableRuntimesLock.ExitReadLock();
            return result;
        }
    }

    public string GetRuntimeFolder(int runtimeId) {
        return Path.Combine(_paths.Runtimes, runtimeId.ToString());
    }

    public List<int> AvailableRuntimes {
        get {
            _availableRuntimesLock.EnterReadLock();
            List<int> result = _availableRuntimes.Keys.ToList();
            _availableRuntimesLock.ExitReadLock();
            return result;
        }
    }
    public byte[] LatestRuntimeLoaderBytes {
        get {
            _availableRuntimesLock.EnterReadLock();
            byte[] result = _latestRuntimeLoaderBytes;
            _availableRuntimesLock.ExitReadLock();
            return result;
        }
    }
    

    public OnixRuntimesService(ILogger<OnixRuntimesService> logger, ApplicationDataPaths paths) {
        _logger = logger;
        _paths = paths;

        _runtimeFilesystemWatcher = new FileSystemWatcher(_paths.Runtimes) {
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
            Filter = "*"
        };
        _runtimeFilesystemWatcher.Created += OnRuntimePathCreated;
        _runtimeFilesystemWatcher.Deleted += OnRuntimePathDeleted;

        ReloadAvailableRuntimes();
    }
    
    public async Task<string?> GetOrGenerateRuntimeZipPath(int runtimeId) {
        string runtimePath = GetRuntimeFolder(runtimeId);
        
        _availableRuntimesLock.EnterReadLock();
        if (_availableRuntimes.TryGetValue(runtimeId, out bool downloadReady)) {
            _availableRuntimesLock.ExitReadLock();
            if (downloadReady) {
                return Path.Combine(runtimePath, "download.zip");   
            }
        }
        else {
            _availableRuntimesLock.ExitReadLock();
            return null;
        }

        string runtimeZipPath = await GenerateRuntimeZip(runtimePath);
        if (string.IsNullOrEmpty(runtimeZipPath))
            return null;
        _availableRuntimesLock.EnterWriteLock();
        _availableRuntimes[runtimeId] = true;
        _availableRuntimesLock.ExitWriteLock();
        return runtimeZipPath;
    }

    private void OnRuntimePathCreated(object sender, FileSystemEventArgs e) => _ = OnRuntimePathCreatedTask(sender, e);
    private async Task OnRuntimePathCreatedTask(object sender, FileSystemEventArgs e) {
        if (e.Name == null) return;
        if (Path.GetDirectoryName(e.FullPath) != _paths.Runtimes) return;
        if (!int.TryParse(e.Name, out int runtimeId)) return;
        await Task.Delay(7500);
        
        bool downloadReady = File.Exists(Path.Combine(e.FullPath, "download.zip"));
        _availableRuntimesLock.EnterWriteLock();
        _availableRuntimes[runtimeId] = downloadReady;
        if (runtimeId > _latestRuntimeId)
            _latestRuntimeId = runtimeId;
        _availableRuntimesLock.ExitWriteLock();
        
        _logger.LogInformation("Runtime {runtimeId} was created on disk and is now available.", runtimeId);
    }

    private void OnRuntimePathDeleted(object sender, FileSystemEventArgs e) {
        if (e.Name == null) return;
        if (Path.GetDirectoryName(e.FullPath) != _paths.Runtimes) return;
        if (!int.TryParse(e.Name, out int runtimeId)) return;
        _availableRuntimesLock.EnterWriteLock();
        _availableRuntimes.Remove(runtimeId);
        if (_latestRuntimeId == runtimeId) {
            _latestRuntimeId = _availableRuntimes.Keys.Max();
        }
        _availableRuntimesLock.ExitWriteLock();
        
        _logger.LogInformation("Runtime {runtimeId} was deleted from disk.", runtimeId);
    }

    public void ReloadAvailableRuntimes() {
        Dictionary<int, bool> availableRuntimes = new();
        int largest = 0;
        foreach (string runtimeDir in Directory.EnumerateDirectories(_paths.Runtimes)) {
            if (int.TryParse(Path.GetFileName(runtimeDir), out int runtimeId)) {
                availableRuntimes[runtimeId] = File.Exists(Path.Combine(runtimeDir, "download.zip"));
                largest = largest < runtimeId ? runtimeId : largest;
            }
        }

        _availableRuntimesLock.EnterWriteLock();
        _availableRuntimes = availableRuntimes;
        _latestRuntimeId = largest;

        try {
            string latestLoaderDll = Path.Combine(GetRuntimeFolder(_latestRuntimeId), "OnixRuntimeLoader.dll");
            if (File.Exists(latestLoaderDll)) {
                _latestRuntimeLoaderBytes = File.ReadAllBytes(latestLoaderDll);
            }
            else {
                _logger.LogWarning("Latest runtime loader DLL not found at {path}", latestLoaderDll);
            }
        }
        catch (Exception e) {
            _logger.LogError(e, "Failed to load latest runtime loader DLL");
        }
        finally {
            _availableRuntimesLock.ExitWriteLock();
        }
    }

    
    private async Task<string> GenerateRuntimeZip(string runtimePath) {
        string outputPath = Path.Combine(runtimePath, "download.zip");
        if (File.Exists(outputPath)) {
            return outputPath;
        }
        
        Task<string>? generationTask;
        lock (_pendingRuntimeZipGenerationsLock) {
            if (!_pendingRuntimeZipGenerations.TryGetValue(runtimePath, out generationTask)) {
                generationTask = Task.Run(async () => {
                    try {
                        using (FileStream fileStream = new(outputPath, FileMode.OpenOrCreate)) {
                            using (ZipArchive archive = new(fileStream, ZipArchiveMode.Create, true)) {
                                foreach (string file in Directory.EnumerateFiles(runtimePath, "*", SearchOption.AllDirectories)) {
                                    string relativePath = Path.GetRelativePath(runtimePath, file);
                                    if (relativePath.StartsWith("OnixRuntimeLoader.")) continue;
                                    if (relativePath.EndsWith(".pdb")) continue;
                                    if (relativePath == "download.zip") continue;
                                    await Task.Run(() => archive.CreateEntryFromFile(file, relativePath));
                                }
                            }
                        }
                    } catch (Exception e) {
                        _logger.LogError(e, "Failed to generate runtime zip");
                        return string.Empty;
                    }

                    return outputPath;
                });
                _pendingRuntimeZipGenerations[runtimePath] = generationTask;
            }
        }

        return await generationTask;
    }
}