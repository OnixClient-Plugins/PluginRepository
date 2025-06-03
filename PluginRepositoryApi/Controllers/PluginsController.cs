using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Data.Plugins;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("plugins")]
public class PluginsController : ControllerBase {
    private readonly IPublishedPluginsService _publishedPlugins;
    private readonly ApplicationDataPaths _paths;
    private readonly IPluginDownloadCountTrackerService _downloadCountTracker;
    
    public PluginsController(IPublishedPluginsService publishedPlugins, ApplicationDataPaths paths, IPluginDownloadCountTrackerService downloadCountTracker) {
        _publishedPlugins = publishedPlugins;
        _paths = paths;
        _downloadCountTracker = downloadCountTracker;
    }
    
    [HttpGet]
    public IActionResult GetPlugins() {
        return Ok(_publishedPlugins.Plugins);
    }

    
    [HttpGet("{uuid}")]
    public IActionResult GetPlugin(string uuid) {
        OnixPlugin? plugin = _publishedPlugins.GetPlugin(uuid);
        if (plugin == null) {
            return NotFound();
        }

        return Ok(plugin);
    }
    
    [HttpGet("{uuid}/icon")]
    public async Task<IActionResult> GetPluginIcon(string uuid) {
        OnixPlugin? plugin = _publishedPlugins.GetPlugin(uuid);
        if (plugin == null) {
            return NotFound();
        }

        string iconPath = Path.Combine(_paths.Plugins, plugin.Manifest.UUID, "icon.png");
        if (!System.IO.File.Exists(iconPath)) {
            return NotFound();
        }
        byte[] iconData = await System.IO.File.ReadAllBytesAsync(iconPath);
        return File(iconData, "image/png");
    }
    [HttpGet("{uuid}/banner")]
    public async Task<IActionResult> GetPluginBanner(string uuid) {
        OnixPlugin? plugin = _publishedPlugins.GetPlugin(uuid);
        if (plugin == null) {
            return NotFound();
        }

        string iconPath = Path.Combine(_paths.Plugins, plugin.Manifest.UUID, "banner.png");
        if (!System.IO.File.Exists(iconPath)) {
            return NotFound();
        }
        byte[] iconData = await System.IO.File.ReadAllBytesAsync(iconPath);
        return File(iconData, "image/png");
    }
    [HttpGet("{uuid}/asset/{assetName}")]
    public async Task<IActionResult> GetPluginBanner(string uuid, string assetName) {
        OnixPlugin? plugin = _publishedPlugins.GetPlugin(uuid);
        if (plugin == null) {
            return NotFound();
        }

        string iconPath = Path.Combine(_paths.Plugins, plugin.Manifest.UUID, "assets", assetName);
        if (!System.IO.File.Exists(iconPath)) {
            return NotFound();
        }
        byte[] iconData = await System.IO.File.ReadAllBytesAsync(iconPath);
        return File(iconData, "application/octet-stream");
    }
    
    [HttpGet("{uuid}/download")]
    public async Task<IActionResult> GetPluginDownload(string uuid) {
        OnixPlugin? plugin = _publishedPlugins.GetPlugin(uuid);
        if (plugin == null) {
            return NotFound();
        }

        plugin.DownloadCount = _downloadCountTracker.IncrementDownloadCount(uuid);
        

        string downloadPath = Path.Combine(_paths.Plugins, plugin.Manifest.UUID, "download.zip");
        if (!System.IO.File.Exists(downloadPath)) {
            return NotFound();
        }
        byte[] iconData = await System.IO.File.ReadAllBytesAsync(downloadPath);
        return File(iconData, "application/zip");
    }
}