using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Data.Plugins;

namespace PluginRepositoryApi.Services;

public interface IPublishedPluginsService {
    OnixPlugin? GetPlugin(string uuid);
    IReadOnlyList<OnixPlugin> Plugins { get; }
    Task<byte[]> GetPluginDownloadAsync(string uuid);
    void OnPluginUnpublished(string uuid);
    void OnPluginPublished(PluginManifest plugin);
    Task ReloadPluginsAsync(CancellationToken cts = default);
}

public class PublishedPluginsService  : BackgroundService, IPublishedPluginsService {
    ILogger<PublishedPluginsService> _logger;
    ApplicationDataPaths _paths;
    PeriodicTimer _timer;    
    IPluginDownloadCountTrackerService _downloadCounts;
    public PublishedPluginsService(ILogger<PublishedPluginsService> logger, ApplicationDataPaths paths, IPluginDownloadCountTrackerService downloadCounts) {
        _logger = logger;
        _paths = paths;
        _downloadCounts = downloadCounts;
        _timer = new PeriodicTimer(TimeSpan.FromHours(1));
    }
    
    private List<OnixPlugin> _publishedPlugins = new();
    private readonly ReaderWriterLockSlim _publishedPluginsLock = new ReaderWriterLockSlim();

    public OnixPlugin? GetPlugin(string uuid) {
        _publishedPluginsLock.EnterReadLock();
        OnixPlugin? plugin = _publishedPlugins.Find(x => x.Manifest.UUID == uuid);
        _publishedPluginsLock.ExitReadLock();
        return plugin;
    }
    public IReadOnlyList<OnixPlugin> Plugins {
        get {
            _publishedPluginsLock.EnterReadLock();
            IReadOnlyList<OnixPlugin> plugins = _publishedPlugins.AsReadOnly();
            _publishedPluginsLock.ExitReadLock();
            return plugins;
        }
    }
    public async Task<byte[]> GetPluginDownloadAsync(string uuid) {
        OnixPlugin? plugin = GetPlugin(uuid);
        if (plugin == null) {
            return Array.Empty<byte>();
        }
        string pluginPath = Path.Combine(_paths.Plugins, plugin.Manifest.UUID, "download.zip");
        if (!File.Exists(pluginPath)) {
            return Array.Empty<byte>();
        }
        return await File.ReadAllBytesAsync(pluginPath);
    }
    
    DateTime GetPluginLastModified(string uuid) {
        string pluginPath = Path.Combine(_paths.Plugins, uuid, "download.zip");
        if (!File.Exists(pluginPath)) {
            return new DateTime(1984, 9, 21, 12, 0, 54);
        }
        return File.GetLastWriteTimeUtc(pluginPath);
    }
    
    private OnixPlugin PluginFromManifest(PluginManifest manifest) {
        var plugin = new OnixPlugin() {
            Manifest = manifest,
            LastUpdated = GetPluginLastModified(manifest.UUID),
            DownloadCount = _downloadCounts.GetDownloadCount(manifest.UUID)
        };
        return plugin;
    }

    public void OnPluginUnpublished(string uuid) {
        _publishedPluginsLock.EnterWriteLock();
        _publishedPlugins.RemoveAll(x => x.Manifest.UUID == uuid);
        _publishedPluginsLock.ExitWriteLock();
    }

    public void OnPluginPublished(PluginManifest plugin) {
        _publishedPluginsLock.EnterWriteLock();
        _publishedPlugins.RemoveAll(x => x.Manifest.UUID == plugin.UUID);
        _publishedPlugins.Add(PluginFromManifest(plugin));
        _publishedPluginsLock.ExitWriteLock();
    }
    
    private async Task<OnixPlugin?> LoadPluginAsync(string pluginPath, CancellationToken cts = default) {
        try {
            PluginManifest? manifest = JsonSerializer.Deserialize<PluginManifest>(await File.ReadAllTextAsync(Path.Combine(pluginPath, "manifest.json")));
            if (!File.Exists(Path.Combine(pluginPath, "download.zip"))) {
                return null;
            }    
            
            if (manifest != null) {
                return PluginFromManifest(manifest);
            }
        } catch (Exception e) {
            _logger.LogError(e, "Could not load plugin  {pluginPath}", Path.GetFileNameWithoutExtension(pluginPath));
        }
        return null;
    }

    public async Task ReloadPluginsAsync(CancellationToken cts = default) {
        List<Task<OnixPlugin?>> tasks = new();
        List<OnixPlugin> newPlugins = new();
        object pluginsLock = new();
        foreach (string pluginPath in Directory.EnumerateDirectories(_paths.Plugins)) {
            tasks.Add(LoadPluginAsync(pluginPath, cts));
        }
        await Task.WhenAll(tasks);
        foreach (var task in tasks) {
            if (task.Result != null) {
                newPlugins.Add(task.Result);
            }
        }
        
        _publishedPluginsLock.EnterWriteLock();
        _publishedPlugins = newPlugins;
        _publishedPluginsLock.ExitWriteLock();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (await _timer.WaitForNextTickAsync(stoppingToken)) {
            await ReloadPluginsAsync(stoppingToken);
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken) {
        return ReloadPluginsAsync(cancellationToken);
    }
}