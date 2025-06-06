using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using System.Diagnostics;
using PluginRepositoryApi.Data.Plugins;

namespace PluginRepositoryApi.Services;

public interface IPluginUpdaterService {
    bool IsUpdating { get; }
    Task<List<PluginCompilationResult>> UpdatePluginsNow(CancellationToken cts = default);
    Task RunGitCommandAsync(string commandWithoutGit, string workingDirectory, CancellationToken cts = default);
}

public class PluginUpdaterService : BackgroundService, IPluginUpdaterService {
    private readonly ILogger<PluginUpdaterService> _logger;
    private readonly ApplicationDataPaths _paths;
    private readonly IPluginSourceDiscoveryService _pluginSourceDiscoveryService;
    private readonly IPluginCompilerService _pluginCompilerService;
    private readonly IPluginPublisherService _pluginPublisherService;
    private readonly PeriodicTimer _timer;
    public bool IsUpdating { get; private set; }
    
    private readonly object _pluginsListLock = new();
    private List<PluginSourcePaths> _newPlugins = new();
    private List<PluginSourcePaths> _removedPlugins = new();

    public PluginUpdaterService(ILogger<PluginUpdaterService> logger, ApplicationDataPaths paths, IPluginSourceDiscoveryService pluginSourceDiscoveryService, IPluginCompilerService pluginCompilerService, IPluginPublisherService pluginPublisherService) {
        _logger = logger;
        _paths = paths;
        _pluginSourceDiscoveryService = pluginSourceDiscoveryService;
        _pluginCompilerService = pluginCompilerService;
        _pluginPublisherService = pluginPublisherService;
        _timer = new PeriodicTimer(TimeSpan.FromHours(1));
        
        _pluginSourceDiscoveryService.OnSourceUpdated += OnPluginSourceUpdated;
        _pluginSourceDiscoveryService.OnSourceRemoved += OnPluginSourceRemoved;
        InitializeGitRepository();
    }
    
    private void InitializeGitRepository() {
        if (!Directory.Exists(_paths.PluginSources)) {
            Directory.CreateDirectory(_paths.PluginSources);
        }
        string gitPath = Path.Combine(_paths.PluginSources, ".git");
        if (!Directory.Exists(gitPath)) {
            try {
                Directory.Delete(_paths.PluginSources, true);
                RunGitCommandAsync($"clone --recursive https://github.com/OnixClient-Plugins/Plugins.git \"{_paths.PluginSources}\"", Path.GetDirectoryName(_paths.PluginSources)!, CancellationToken.None).Wait();
            } catch (Exception e) {
                _logger.LogError(e, "Failed to initialize git repository for plugins.");
            }
        }
    }

    private void OnPluginSourceRemoved(PluginSourcePaths source) {
        lock (_pluginsListLock) {
            _removedPlugins.Add(source);
        }
    }

    private void OnPluginSourceUpdated(PluginSourcePaths source) {
        lock (_pluginsListLock) {
            _newPlugins.Add(source);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (await _timer.WaitForNextTickAsync(stoppingToken)) {
            await UpdatePluginsNow(stoppingToken);
        }
    }
    

    public async Task<List<PluginCompilationResult>> UpdatePluginsNow(CancellationToken cts = default) {
        IsUpdating = true;
        await PullPluginsAsync(cts);
        IsUpdating = false;
        _pluginSourceDiscoveryService.ReloadPluginSources();

        List<PluginCompilationResult> results = new();
        object resultsLock = new();
        List<Task> tasks = new();
        foreach (var newPlugin in _newPlugins) {
            tasks.Add(CompileSinglePlugin(cts, newPlugin, resultsLock, results));

        }
        foreach (var removedPlugin in _removedPlugins) {
            tasks.Add(_pluginPublisherService.UnpublishPluginAsync(removedPlugin.Manifest.UUID, cts));
        }
        await Task.WhenAll(tasks);
        return results;
    }

    private async Task CompileSinglePlugin(CancellationToken cts, PluginSourcePaths newPlugin, object resultsLock, List<PluginCompilationResult> results) {
        var compiledSource = await _pluginCompilerService.CompileSourceAsync(newPlugin.SolutionDirectory, newPlugin.ProjectDirectory, newPlugin.Manifest, cts);
        lock (resultsLock) {
            results.Add(compiledSource);
        }
        if (!cts.IsCancellationRequested && compiledSource.Success) {
            await _pluginPublisherService.PublishPluginAsync(compiledSource);
        }
    }

    public async Task RunGitCommandAsync(string commandWithoutGit, string workingDirectory, CancellationToken cts = default) {
        ProcessStartInfo pis = new ProcessStartInfo {
            FileName = "git",
            Arguments = commandWithoutGit,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };
        Process proc = new Process { StartInfo = pis };
        proc.Start();
        await proc.WaitForExitAsync(cts);
    }
    private async Task PullPluginsAsync(CancellationToken cts = default) {
        string workingDirectory = _paths.PluginSources;

        try {
            await RunGitCommandAsync("fetch origin", workingDirectory, cts);
            await RunGitCommandAsync("reset --hard origin/HEAD", workingDirectory, cts);
            await RunGitCommandAsync("submodule update --init --force --recursive", workingDirectory, cts);
        } catch (Exception e) {
            _logger.LogError(e, "Failed to pull plugins from git.");
        }
    }
    
}