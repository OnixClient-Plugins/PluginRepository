using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using PluginRepositoryApi.Data;

namespace PluginRepositoryApi.Services;

public interface IApiKeyValidator {
    bool IsValid(string apiKey);
}

public class ApiKeyValidatorService : IApiKeyValidator {
    private readonly ApplicationDataPaths _paths;
    private FileSystemWatcher _apiKeyFileWatcher;
    private string _apiKeysPath;
    private string[] _apiKeys;

    public ApiKeyValidatorService(ApplicationDataPaths paths) {
        _paths = paths;
        _apiKeysPath = System.IO.Path.Combine(Path.GetFullPath(_paths.Data), "api_keys.txt");
        _apiKeys = File.ReadAllLines(_apiKeysPath);
        _apiKeyFileWatcher = new FileSystemWatcher(_paths.Data) {
            IncludeSubdirectories = false,
            EnableRaisingEvents = true,
            Filter = "api_keys.txt",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };
        
        _apiKeyFileWatcher.Created += OnApiKeyFileChanged;
        _apiKeyFileWatcher.Changed += OnApiKeyFileChanged;
        
    }

    private void OnApiKeyFileChanged(object sender, FileSystemEventArgs e) {
        if (e.FullPath == _apiKeysPath) {
            _apiKeys = File.ReadAllLines(_apiKeysPath);
        }
    } 
    

    public bool IsValid(string apiKey) {
        return _apiKeys.Contains(apiKey);
    }
}

public class ApiKeyAuthorizeAttribute : TypeFilterAttribute {
    public ApiKeyAuthorizeAttribute() : base(typeof(ApiKeyAuthorizationFilter)) {}
}

public class ApiKeyAuthorizationFilter : IAuthorizationFilter {
    private readonly IApiKeyValidator _apiKeyValidator;

    public ApiKeyAuthorizationFilter(IApiKeyValidator apiKeyValidator) {
        _apiKeyValidator = apiKeyValidator;
    }
    
    public void OnAuthorization(AuthorizationFilterContext context) {
        if (!context.HttpContext.Request.Headers.TryGetValue("Balls-Api-Key", out var extractedApiKey)) {
            context.Result = new UnauthorizedResult();
            return;
        }
        if (extractedApiKey.Any(x => _apiKeyValidator.IsValid(x ?? string.Empty))) {
            return;
        }
        
        context.Result = new UnauthorizedResult();
    }
}