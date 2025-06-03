using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Data.Plugins;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("compile")]
public class PluginRecompileController : ControllerBase {
    private readonly IPluginCompilerService _compilerService;
    private readonly IPluginPublisherService _publisherService;
    private readonly IPluginSourceDiscoveryService _sourcesService;
    
    public PluginRecompileController(IPluginPublisherService publisherService, IPluginSourceDiscoveryService sourcesService, IPluginCompilerService compilerService) {
        _publisherService = publisherService;
        _sourcesService = sourcesService;
        _compilerService = compilerService;
    }
    
    [ApiKeyAuthorize]
    [HttpGet]
    public async Task<IActionResult> RecompilePlugins() {
        IReadOnlyList<PluginSourcePaths> sources = _sourcesService.PluginSources;
        List<Task<PluginCompilationResult>> compilationTasks = new();
        foreach (var source in sources) {
            compilationTasks.Add(_compilerService.CompileSourceAsync(source));
        }
        await Task.WhenAll(compilationTasks);
        foreach (var task in compilationTasks) {
            PluginCompilationResult result = await task;
            if (result.Success) {
                await _publisherService.PublishPluginAsync(result);
            }
        }
        return Ok(compilationTasks.Select(task => task.Result));
    }
    
    [ApiKeyAuthorize]
    [HttpGet("{uuid}")]
    public async Task<IActionResult> RecompilePlugin(string uuid) {
        PluginSourcePaths? pluginPath = _sourcesService.PluginSources.FirstOrDefault(plugin => plugin.Manifest.UUID == uuid);
        if (pluginPath == null) {
            return NotFound();
        }
        
        PluginCompilationResult compiledPlugin = await _compilerService.CompileSourceAsync(pluginPath);
        if (compiledPlugin.Success) {
            await _publisherService.PublishPluginAsync(compiledPlugin);
        }

        return Ok(compiledPlugin);
    }
}
