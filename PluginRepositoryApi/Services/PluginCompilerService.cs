using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PluginRepositoryApi.Data;
using PluginRepositoryApi.Data.Plugins;

namespace PluginRepositoryApi.Services;

public interface IPluginCompilerService {
    public Task<PluginCompilationResult> CompileSourceAsync(string solutionDir, string projectDir, PluginManifest manifest, CancellationToken cts = default);
    public Task<PluginCompilationResult> CompileSourceAsync(PluginSourcePaths sourcePaths, CancellationToken cts = default);
}

public class PluginCompilerService : IPluginCompilerService {
    private readonly ILogger<PluginCompilerService> _logger;
    private readonly IOnixRuntimesService _onixRuntimesService;
    private readonly IPluginTrustService _pluginTrustService;

    public PluginCompilerService(ILogger<PluginCompilerService> logger, IOnixRuntimesService onixRuntimesService, IPluginTrustService pluginTrustService) {
        _logger = logger;
        _onixRuntimesService = onixRuntimesService;
        _pluginTrustService = pluginTrustService;
    }

    private string GetBuildOutputDir(string projectDir) {
        return Path.Combine(projectDir, "bin", "x64", "Release", "net8.0");
    }
    
    private async Task<Tuple<bool, string>> RunCompilationAsync(string solutionDir, int runtimeVersion, CancellationToken cts = default) {
        try {
            _logger.LogInformation("Compiling solution at {solutionDir}", solutionDir);
            string successFlagFilePath = Path.Combine(solutionDir, "BuildSuccessful.txt");
            if (File.Exists(successFlagFilePath)) {
                File.Delete(successFlagFilePath);
            }

            ProcessStartInfo startInfoClean = new() {
                FileName = "dotnet",
                Arguments = "clean",
                WorkingDirectory = solutionDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                EnvironmentVariables = {
                    // Sets the build flag as a server build (mainly stops copying the files to the game's folder).
                    { "ServerPluginBuild", "TRUE" },
                    // Adds it to the assembly search path.
                    { "ServerPluginBuildRuntime", _onixRuntimesService.GetRuntimeFolder(runtimeVersion) },
                }
            };
            Process processClean = new() {
                StartInfo = startInfoClean
            };
            processClean.Start();
            await processClean.WaitForExitAsync(cts);
            
            
            ProcessStartInfo startInfo = new() {
                FileName = "dotnet",
                Arguments = "build -c Release",
                WorkingDirectory = solutionDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                EnvironmentVariables = {
                    // Sets the build flag as a server build (mainly stops copying the files to the game's folder).
                    { "ServerPluginBuild", "TRUE" },
                    // Adds it to the assembly search path.
                    { "ServerPluginBuildRuntime", _onixRuntimesService.GetRuntimeFolder(runtimeVersion) },
                }
            };
            Process process = new() {
                StartInfo = startInfo
            };
            StringBuilder buildLogBuilder = new();
            process.OutputDataReceived += (_, args) => {
                if (args.Data != null) {
                    buildLogBuilder.AppendLine(args.Data);
                }
            };
            process.ErrorDataReceived += (_, args) => {
                if (args.Data != null) {
                    buildLogBuilder.AppendLine(args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try {
                await process.WaitForExitAsync(cts);
            }
            catch (OperationCanceledException) {
                process.Kill();
                return new Tuple<bool, string>(false, "Compilation timed out");
            }

            return new Tuple<bool, string>(File.Exists(successFlagFilePath), buildLogBuilder.ToString());
        } catch (Exception e) {
            _logger.LogError(e, "Failed to compile solution at {solutionDir}", solutionDir);
            return new Tuple<bool, string>(false, "Failed to compile solution: " + e.Message);
        }
    }

    async Task<byte[]> ZipBuiltPlugin(string projectDir, string pluginUuid) {
        MemoryStream pluginZipStream = new();
        ZipArchive pluginZip = new(pluginZipStream, ZipArchiveMode.Create, true);
        string outputDir = GetBuildOutputDir(projectDir);
        
        //remove assets from output as we will grab it from projectDir
        if (Directory.Exists(Path.Combine(outputDir, "assets")))
            Directory.Delete(Path.Combine(outputDir, "assets"), true);
        if (Directory.Exists(Path.Combine(outputDir, "Assets")))
            Directory.Delete(Path.Combine(outputDir, "Assets"), true);

        List<Task> tasks = new();
        object lockObj = new();
        try {
            foreach (string file in Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories)) {
                string relativePath = Path.GetRelativePath(outputDir, file);
                ZipArchiveEntry entry = pluginZip.CreateEntry(relativePath);
                await using Stream entryStream = entry.Open();
                await using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read);
                await fileStream.CopyToAsync(entryStream);
            }

            string assetsFolder = Path.Combine(projectDir, "Assets");
            pluginZip.CreateEntry("Assets/");
            foreach (string file in Directory.GetFiles(assetsFolder, "*", SearchOption.AllDirectories)) {
                string relativePath = Path.GetRelativePath(assetsFolder, file);
                ZipArchiveEntry entry = pluginZip.CreateEntry(Path.Combine("Assets", relativePath));
                await using Stream entryStream = entry.Open();
                await using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read);
                await fileStream.CopyToAsync(entryStream);
            }
            
            var manifestEntry = pluginZip.CreateEntry("manifest.json");
            await using Stream manifestStream = manifestEntry.Open();
            await using FileStream manifestFileStream = new(Path.Combine(projectDir, "manifest.json"), FileMode.Open, FileAccess.Read);
            await manifestFileStream.CopyToAsync(manifestStream);
        } catch (Exception e) {
            _logger.LogError(e, "Failed to zip plugin");
            return Array.Empty<byte>();
        }
        await Task.WhenAll(tasks);
        pluginZip.Dispose();
        return pluginZipStream.ToArray();
    }

    
    public async Task<PluginCompilationResult> CompileSourceAsync(string solutionDir, string projectDir, PluginManifest manifest, CancellationToken cts = default) {
        try {
            if (!File.Exists(Path.Combine(projectDir, "manifest.json"))) {
                _logger.LogError("(1) Failed to compile plugin {pluginName} in {solutionDir}, manifest not found", manifest.Name, solutionDir);
                return new PluginCompilationResult(false, "Manifest not found") {Manifest = manifest};
            }
            
            int runtimeVersion = _onixRuntimesService.LatestRuntimeId;
            (bool compiledSuccessfully, string buildLogs) = await RunCompilationAsync(solutionDir, runtimeVersion, cts);
            if (!compiledSuccessfully) {
              _logger.LogError("(2) Failed to compile plugin {pluginName} in {solutionDir}", manifest.Name, solutionDir);
              return new PluginCompilationResult(false, buildLogs) {Manifest = manifest};
            }

            string mainAssemblyPath = Path.Combine(GetBuildOutputDir(projectDir), manifest.TargetAssembly);
            if (!File.Exists(mainAssemblyPath)) {
                _logger.LogError("(3) Failed to compile plugin {pluginName} in {solutionDir}, target assembly ({targetAssembly}) not found", manifest.Name, solutionDir, manifest.TargetAssembly);
                return new PluginCompilationResult(false, $"{buildLogs}\n\nTarget assembly not found: {manifest.TargetAssembly}") {Manifest = manifest};
            }
            
            byte[] mainAssemblyBytes = await File.ReadAllBytesAsync(mainAssemblyPath, cts);
            string mainAssemblyHash = _pluginTrustService.ComputeHash(mainAssemblyBytes);

            byte[] pluginZip = await ZipBuiltPlugin(projectDir, manifest.UUID);
            if (pluginZip.Length == 0) {
                _logger.LogError("(4) Failed to compile plugin {pluginName} in {solutionDir}, failed to zip plugin", manifest.Name, solutionDir);
                return new PluginCompilationResult(false, $"{buildLogs}\n\nFailed to zip plugin") {Manifest = manifest};
            }

            var compilationResult = new PluginCompilationResult(compiledSuccessfully, buildLogs, mainAssemblyHash, manifest.Copy(runtimeVersion));
            // update manifest in project dir since we built it successfully for the new runtime
            await File.WriteAllTextAsync(Path.Combine(projectDir, "manifest.json"), JsonSerializer.Serialize(compilationResult.Manifest), cts);
            
            compilationResult.ZippedPlugin = pluginZip;
            compilationResult.IconPath = Path.Combine(projectDir, "Assets", "PluginIcon.png");
            compilationResult.BannerPath = Path.Combine(projectDir, "Assets", "PluginBanner.png");
            compilationResult.AssetsPath = Path.Combine(projectDir, "Assets");
            compilationResult.MainAssemblyHash = mainAssemblyHash;
            return compilationResult;
        } catch (Exception e) {
            _logger.LogError(e, "(5) Failed to compile plugin {pluginName} in {solutionDir}", manifest.Name, solutionDir);
            return new PluginCompilationResult(false, "Failed to compile plugin: " + e.Message) {Manifest = manifest};
        }
    }

    public Task<PluginCompilationResult> CompileSourceAsync(PluginSourcePaths sourcePaths, CancellationToken cts = default) {
        sourcePaths.RefreshManifest();
        return CompileSourceAsync(sourcePaths.SolutionDirectory, sourcePaths.ProjectDirectory, sourcePaths.Manifest, cts);
    }
}