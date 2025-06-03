using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace PluginRepositoryApi.Controllers;

[ApiController]
[Route("docs")]
public class RuntimeDocumentationController : ControllerBase {
    private readonly ILogger<RuntimeDocumentationController> _logger;
    private readonly ApplicationDataPaths _paths;
    private readonly IOnixRuntimesService _runtimes;
    public RuntimeDocumentationController(ApplicationDataPaths paths, ILogger<RuntimeDocumentationController> logger, IOnixRuntimesService runtimes) {
        _paths = paths;
        _logger = logger;
        _runtimes = runtimes;
    }
    [SwaggerIgnore]
    private string GetMimeType(string filePath) {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch {
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".ico" => "image/x-icon",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
    [SwaggerIgnore]
    public bool IsTextFile(string filePath) {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch {
            ".html" => true,
            ".css" => true,
            ".js" => true,
            ".txt" => true,
            ".json" => true,
            _ => false
        };
    }
    
    
    [HttpGet()]
    public IActionResult GetDocumentation() {
        return Redirect("/docs/latest/");
    }
    [HttpGet("{runtimeVersion}")]
    public async Task<IActionResult> GetDocumentation(string runtimeVersion) {
        if (runtimeVersion == "latest") {
            runtimeVersion = _runtimes.LatestRuntimeId.ToString();
        }
        string docPath = Path.Combine(_paths.Data, "websites", "docs", runtimeVersion, "index.html");
        if (!System.IO.File.Exists(docPath)) {
            return NotFound("The file you requested could not be found at that URL.");
        }
        
        string docBytes = await System.IO.File.ReadAllTextAsync(docPath);
        return Content(docBytes,"text/html", new System.Text.UTF8Encoding(false));
    }
    
    [HttpGet("{runtimeVersion}/{*filePath}")]
    public async Task<IActionResult> GetFile(string runtimeVersion, string filePath) {
        if (runtimeVersion == "latest") {
            runtimeVersion = _runtimes.LatestRuntimeId.ToString();
        }
        string fullPath = Path.Combine(_paths.Data, "websites", "docs", runtimeVersion, filePath);
        if (!System.IO.File.Exists(fullPath)) {
            return NotFound("The file you requested could not be found at that URL.");
        }
        
        if (IsTextFile(filePath)) {
            string fileBytes = await System.IO.File.ReadAllTextAsync(fullPath);
            string mimeType = GetMimeType(fullPath);
            return Content(fileBytes, mimeType, new System.Text.UTF8Encoding(false));
        } else {
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            string mimeType = GetMimeType(fullPath);
            return File(fileBytes, mimeType);
        }
    }
}