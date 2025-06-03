using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("trusted_plugin_sources")]
public class TrustedPluginSourceController : ControllerBase {
    public readonly ITrustedPluginSourcesService _trustedPluginSourcesService;
    public TrustedPluginSourceController(ITrustedPluginSourcesService trustedPluginSourcesService) {
        _trustedPluginSourcesService = trustedPluginSourcesService;
    }
    
    [HttpGet]
    [ApiKeyAuthorize]
    public IActionResult GetTrustedSources() {
        return Ok(_trustedPluginSourcesService.TrustedPluginSources);
    }
    
    [HttpGet("{uuid}")]
    [ApiKeyAuthorize]
    public IActionResult IsTrusted(string uuid) {
        if (_trustedPluginSourcesService.IsPluginTrusted(uuid))
            return Ok();
        return NotFound();
    }
    
    [HttpPut("{uuid}")]
    [ApiKeyAuthorize]
    public async Task<IActionResult> AddTrustedSource(string uuid) {
        try {
            await _trustedPluginSourcesService.AddTrustedSource(uuid);
            return Ok();
        } catch (Exception e) {
            return BadRequest(e.Message);
        }
    }
    
    [HttpDelete("{uuid}")]
    [ApiKeyAuthorize]
    public async Task<IActionResult> RemoveTrustedSource(string uuid) {
        try {
            if (await _trustedPluginSourcesService.RemoveTrustedSource(uuid))
                return Ok();
            return NotFound();
        } catch (Exception e) {
            return BadRequest(e.Message);
        }
    }
    
}