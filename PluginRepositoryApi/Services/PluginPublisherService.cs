using System.Text.Json;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Data.Plugins;

namespace PluginRepositoryApi.Services;

public interface IPluginPublisherService {
    Task PublishPluginAsync(PluginCompilationResult compilationReult);
    Task PublishPluginAsync(byte[] pluginZip, PluginManifest manifest, string mainAssemblyHash, string? iconPath, string? bannerPath, string? assetsPath);
    Task UnpublishPluginAsync(string uuid, CancellationToken cts = default);
}

public class PluginPublisherService : IPluginPublisherService {
    private readonly ApplicationDataPaths _paths;
    private readonly IPublishedPluginsService _publishedPluginsService;
    private readonly IPluginTrustService _pluginTrustService;
    private readonly ILogger<PluginPublisherService> _logger;
    
    public PluginPublisherService(ApplicationDataPaths paths, ILogger<PluginPublisherService> logger, IPublishedPluginsService publishedPluginsService, IPluginTrustService pluginTrustService) {
        _paths = paths;
        _logger = logger;
        _publishedPluginsService = publishedPluginsService;
        _pluginTrustService = pluginTrustService;
    }

    public async Task PublishPluginAsync(PluginCompilationResult compilationReult) {
        if (!compilationReult.Success || compilationReult.Manifest is null) {
            _logger.LogError("Failed to publish plugin due to compilation failure");
            return;
        }
        await PublishPluginAsync(compilationReult.ZippedPlugin, compilationReult.Manifest, compilationReult.MainAssemblyHash, compilationReult.IconPath, compilationReult.BannerPath, compilationReult.AssetsPath);
    }
    
    public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive) {
        var dir = new DirectoryInfo(sourceDir);

        if (!dir.Exists) return;

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles()) {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: true);
        }

        if (recursive) {
            foreach (DirectoryInfo subDir in dir.GetDirectories()) {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, recursive);
            }
        }
    }

    public async Task PublishPluginAsync(byte[] pluginZip, PluginManifest manifest, string mainAssemblyHash, string? iconPath, string? bannerPath, string? assetsPath) {
        try {
            string publishedPluginPath = Path.Combine(_paths.Plugins, manifest.UUID);
            if (!Directory.Exists(publishedPluginPath)) {
                Directory.CreateDirectory(publishedPluginPath);
            }

            await File.WriteAllBytesAsync(Path.Combine(publishedPluginPath, $"download.zip"), pluginZip);
            await File.WriteAllTextAsync(Path.Combine(publishedPluginPath, "manifest.json"), JsonSerializer.Serialize(manifest));

            string publishedIconPath = Path.Combine(publishedPluginPath, "icon.png");
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath)) {
                File.Copy(iconPath, publishedIconPath, true);
            }
            else if (File.Exists(publishedIconPath)) {
                File.Delete(publishedIconPath);
            }
            string publishedBannerPath = Path.Combine(publishedPluginPath, "banner.png");
            if (!string.IsNullOrEmpty(bannerPath) && File.Exists(bannerPath)) {
                File.Copy(bannerPath, publishedBannerPath, true);
            }
            else if (File.Exists(publishedBannerPath)) {
                File.Delete(publishedBannerPath);
            }
            string publishedAssetsPath = Path.Combine(publishedPluginPath, "assets");
            if (!string.IsNullOrEmpty(assetsPath) && Directory.Exists(assetsPath)) {
                if (Directory.Exists(publishedAssetsPath)) {
                    Directory.Delete(publishedAssetsPath, true);
                }

                CopyDirectory(assetsPath, publishedAssetsPath, true);
            }
            else if (Directory.Exists(publishedAssetsPath)) {
                Directory.Delete(publishedAssetsPath, true);
            }

            try {
                string hashFilePath = Path.Combine(publishedPluginPath, "hash.txt");
                await File.WriteAllTextAsync(hashFilePath, mainAssemblyHash);
            } catch (Exception e) {
                _logger.LogError(e, "Failed to save hash.txt for plugin {uuid}", manifest.UUID);
            }

            if (_pluginTrustService.IsPluginTrusted(manifest.UUID))
                _pluginTrustService.CacheTrustedPlugin(mainAssemblyHash);
            _publishedPluginsService.OnPluginPublished(manifest);
        } catch (Exception e) {
            _logger.LogError(e, "Failed to publish plugin");
        }
    }
    
    
    public async Task UnpublishPluginAsync(string uuid, CancellationToken cts = default) {
        // 25 retries
        for (int i = 0; i < 25; i++) {
            try {
                if (Directory.Exists(Path.Combine(_paths.Plugins, uuid))) {
                    Directory.Delete(uuid, true);
                    _publishedPluginsService.OnPluginUnpublished(uuid);
                    return;
                }
                _publishedPluginsService.OnPluginUnpublished(uuid);
                return;
            }
            catch (Exception e) {
                _logger.LogError(e, "Failed to unpublish plugin {uuid}", uuid);
            }

            try {
                await Task.Delay(500, cts);
            } catch (TaskCanceledException) {
                return;
            }
        }
    }
}