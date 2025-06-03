using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Data.Plugins;

namespace PluginRepositoryApi.Services;

public interface IPluginDownloadCountTrackerService {
    Dictionary<string, int> DownloadCounts { get; }
    Task? ExecuteTask { get; }
    int GetDownloadCount(string pluginUuid);
    int IncrementDownloadCount(string pluginUuid);
    void LoadDownloadCounts();
    Task SaveDownloadCountsAsync();
    void Dispose();
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class PluginDownloadCountTrackerService : BackgroundService, IPluginDownloadCountTrackerService {
    public Dictionary<string, int> DownloadCounts { get; private set; } = new();
    private readonly ILogger<PluginDownloadCountTrackerService> _logger;
    private readonly ApplicationDataPaths _paths;
    private readonly ReaderWriterLockSlim _downloadCountsLock = new();
    private bool _isDirty = false;
    private readonly PeriodicTimer _saveTimer;
    
    public PluginDownloadCountTrackerService(ILogger<PluginDownloadCountTrackerService> logger, ApplicationDataPaths paths) {
        _logger = logger;
        _paths = paths;
        _saveTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        //_saveTimer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        LoadDownloadCounts();
    }
    
    public int GetDownloadCount(string pluginUuid) {
        _downloadCountsLock.EnterReadLock();
        try {
            return DownloadCounts.GetValueOrDefault(pluginUuid, 0);
        } finally {
            _downloadCountsLock.ExitReadLock();
        }
    }
    
    public int IncrementDownloadCount(string pluginUuid) {
        _downloadCountsLock.EnterWriteLock();
        try {
            DownloadCounts.TryAdd(pluginUuid, 0);
            int newCount = DownloadCounts[pluginUuid]++;
            _isDirty = true;
            return newCount + 1;
        } finally {
            _downloadCountsLock.ExitWriteLock();
        }
    }
    
    public void LoadDownloadCounts() {
        _downloadCountsLock.EnterWriteLock();
        try {
            DownloadCounts.Clear();
            string filePath = Path.Combine(_paths.Data, "download_counts.json");
            if (File.Exists(filePath)) {
                string json = File.ReadAllText(filePath);
                DownloadCounts = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            }
            _isDirty = false;
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to load download counts");
        } finally {
            _downloadCountsLock.ExitWriteLock();
        }
    }

    public async Task SaveDownloadCountsAsync() {
        if (!_isDirty) return;
        _downloadCountsLock.EnterReadLock();
        string json = System.Text.Json.JsonSerializer.Serialize(DownloadCounts);
        _downloadCountsLock.ExitReadLock();
        try {
            string filePath = Path.Combine(_paths.Data, "download_counts.json");
            await File.WriteAllTextAsync(filePath, json);
            _logger.LogInformation("Download counts saved successfully");
        } catch (Exception ex) {
            _logger.LogError(ex, "Failed to save download counts");
        } finally {
        }
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogInformation("PluginDownloadCountTrackerService started");
        
        while (!stoppingToken.IsCancellationRequested) {
            try {
                if (!await _saveTimer.WaitForNextTickAsync(stoppingToken).AsTask()) {
                    break; // Timer cancelled
                }
            } catch (Exception) {}
            if (_isDirty) {
                await SaveDownloadCountsAsync();
                _isDirty = false;
            }
        }
    }
    
}