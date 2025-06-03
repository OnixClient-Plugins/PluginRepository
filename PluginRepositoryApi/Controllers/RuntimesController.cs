using Microsoft.AspNetCore.Mvc;
using PluginRepositoryApi.Services;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("runtimes")]
public class RuntimesController  : ControllerBase {
    private readonly IOnixRuntimesService _runtimeService;
    
    public RuntimesController(IOnixRuntimesService runtimeService) {
        _runtimeService = runtimeService;
    }
    
    [HttpGet]
    public IActionResult GetRuntimes() {
        return Ok(_runtimeService.AvailableRuntimes);
    }
    [HttpGet("latest-version")]
    public IActionResult LatestVersion() {
        return Ok(_runtimeService.LatestRuntimeId);
    }
    [HttpGet("latest-loader-dll")]
    public IActionResult LatestLoaderDll() {
        return File(_runtimeService.LatestRuntimeLoaderBytes, "application/octet-stream", "OnixRuntimeLoader.dll");
    }
    
    [HttpGet("{version}")]
    public async Task<IActionResult> GetRuntime(int version) {
        string? runtimePath = await _runtimeService.GetOrGenerateRuntimeZipPath(version);
        if (runtimePath == null) {
            return NotFound();
        }
        byte[] runtimeFile = await System.IO.File.ReadAllBytesAsync(runtimePath);
        return File(runtimeFile, "application/zip");
    }
}