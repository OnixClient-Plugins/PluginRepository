using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("trusted_plugins")]
public class TrustedPluginsController : ControllerBase {

    private readonly IPluginTrustService _pluginTrustService;
    public TrustedPluginsController(IPluginTrustService pluginTrustService) {
        _pluginTrustService = pluginTrustService;
    }

    [HttpGet]
    [ApiKeyAuthorize]
    public IActionResult Index() {
        return Ok(_pluginTrustService.TrustedPluginUuids);
    }
    
    [HttpGet("{hash}/{uuid}")]
    public async Task<IActionResult> IsTrusted(string hash, string uuid) {
        if (await _pluginTrustService.IsPluginTrusted(hash, uuid))
            return Ok();
        return NotFound();
    }
    
    [HttpPut("{uuid}")]
    [ApiKeyAuthorize]
    public async Task<IActionResult> AddTrustedPlugin(string uuid) {
        try {
            if (await _pluginTrustService.AddTrustedPlugin(uuid))
                return Ok();
        } catch (Exception e) {
            return BadRequest(e.Message);
        }

        return NotFound();
    }
    
    [HttpDelete("{uuid}")]
    [ApiKeyAuthorize]
    public async Task<IActionResult> RemoveTrustedPlugin(string uuid) {
        try {
            if (await _pluginTrustService.RemoveTrustedPlugin(uuid))
                return Ok();
            return NotFound();
        } catch (Exception e) {
            return BadRequest(e.Message);
        }
    }
    
    [HttpGet("/reset_cache")]
    [ApiKeyAuthorize]
    public IActionResult ResetCache() {
        try {
            _pluginTrustService.ResetCache();
            return Ok();
        } catch (Exception e) {
            return BadRequest(e.Message);
        }
    }
    
}