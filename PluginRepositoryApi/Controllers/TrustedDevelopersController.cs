using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("trusted_developers")]
public class TrustedDevelopersController : ControllerBase {
    ITrustedDeveloperService _trustedDeveloperService;
    public TrustedDevelopersController(ITrustedDeveloperService trustedDeveloperService) {
        _trustedDeveloperService = trustedDeveloperService;
    }
    
    [ApiKeyAuthorize]
    [HttpGet("")]
    public IActionResult GetTrustedList() {
        return Ok(_trustedDeveloperService.TrustedDevelopers);
    }
    
    [HttpGet("{xuid}")]
    public IActionResult IsTrusted(string xuid) {
        bool isTrusted = _trustedDeveloperService.TrustedDevelopers.Contains(xuid);
        if (isTrusted) {
            return Ok();
        }
        return NotFound();
    }
    
    [ApiKeyAuthorize]
    [HttpPut("{xuid}")]
    public async Task<IActionResult> AddTrustedDeveloper(string xuid) {
        await _trustedDeveloperService.AddDeveloper(xuid);
        return Ok();
    }
    
    [ApiKeyAuthorize]
    [HttpDelete("{xuid}")]
    public async Task<IActionResult> RemoveTrustedDeveloper(string xuid) {
        bool removed = await _trustedDeveloperService.RemoveDeveloper(xuid);
        if (!removed) {
            return NotFound();
        }
        return Ok();
    }
}