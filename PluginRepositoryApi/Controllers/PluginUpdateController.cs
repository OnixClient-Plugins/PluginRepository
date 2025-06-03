using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("update")]
public class PluginUpdateController : ControllerBase {
     private readonly IPluginUpdaterService _pluginUpdater;
     
     public PluginUpdateController(IPluginUpdaterService pluginUpdater) {
          _pluginUpdater = pluginUpdater;
     }
     
     [ApiKeyAuthorize]
     [HttpPost]
     public async Task<IActionResult> UpdatePlugins() {
          try {
               return Ok(await _pluginUpdater.UpdatePluginsNow());
          } catch (Exception e) {
               return StatusCode(500, e.Message);
          }
     }
}