const urlParams = new URLSearchParams(window.location.search);

async function updateGameVersionPlaceholder() {
    const gameVersionInput = document.getElementById('gameVersion');
    if (!gameVersionInput) return;

    gameVersionInput.placeholder = 'Loading...';

    const cachedVersion = localStorage.getItem('latestGameVersion');
    if (cachedVersion) {
        gameVersionInput.placeholder = cachedVersion;
    }

    try {
        const response = await fetch('https://raw.githubusercontent.com/OnixClient/onix_compatible_appx/refs/heads/main/versions.json');

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('Fetched version data:', data);

        if (data && Array.isArray(data) && data.length > 0) {
            const latestVersion = data[0].version;
            gameVersionInput.placeholder = latestVersion;
            console.log('Updated placeholder to:', latestVersion);

            localStorage.setItem('latestGameVersion', latestVersion);
        } else {
            throw new Error('Invalid version data format');
        }
    } catch (error) {
        console.error('Failed to fetch version:', error);
        if (!cachedVersion) {
            gameVersionInput.placeholder = 'Version unavailable';
        }
    }
}

async function updateRuntimeVersionPlaceholder() {
    const runtimeVersionInput = document.getElementById('runtimeVersion');
    if (!runtimeVersionInput) return;

    runtimeVersionInput.placeholder = 'Loading...';

    const cachedVersion = localStorage.getItem('latestRuntimeVersion');
    if (cachedVersion) {
        runtimeVersionInput.placeholder = cachedVersion;
    }

    try {
        const response = await fetch('https://plugin.onixclient.com/runtimes/latest-version');

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const version = await response.text();
        const trimmedVersion = version.trim();
        runtimeVersionInput.placeholder = trimmedVersion;

        localStorage.setItem('latestRuntimeVersion', trimmedVersion);
    } catch (error) {
        console.error('Failed to fetch runtime version:', error);
        if (!cachedVersion) {
            runtimeVersionInput.placeholder = 'Version unavailable';
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);

    scheduleVersionUpdates();

    if (document.getElementById('pluginName'))
        document.getElementById('pluginName').value = urlParams.get('plugin_name') || '';

    if (document.getElementById('description'))
        document.getElementById('description').value = urlParams.get('plugin_description') || 'My amazing plugin description that I forgot to change for some reason.';

    if (document.getElementById('pluginAuthor'))
        document.getElementById('pluginAuthor').value = urlParams.get('plugin_author') || '';

    if (document.getElementById('repositoryUrl'))
        document.getElementById('repositoryUrl').value = urlParams.get('repository_url') || '';

    if (document.getElementById('folderName'))
        document.getElementById('folderName').value = urlParams.get('folder_name') || (sanitizePluginName(urlParams.get('plugin_name')))

    if (document.getElementById('pluginVersion'))
        document.getElementById('pluginVersion').value = urlParams.get('plugin_version') || '1.0.0';

    if (document.getElementById('gameVersion'))
        document.getElementById('gameVersion').value = urlParams.get('game_version') || '';

    if (document.getElementById('runtimeVersion'))
        document.getElementById('runtimeVersion').value = urlParams.get('runtime_version') || '';

    const events = urlParams.get('events');
    if (events) {
        const selectedEvents = events.split(',').map(event => event.trim());

        const checkboxes = document.querySelectorAll('#eventsContent input[type="checkbox"]');

        checkboxes.forEach(checkbox => {
            if (selectedEvents.includes(checkbox.value)) {
                checkbox.checked = true;
            }
        });

        const selectedCount = document.querySelectorAll('#eventsContent input:checked').length;
        const buttonText = selectedCount > 0 ? `Selected Events (${selectedCount})` : 'Select Events';
        const dropdownButton = document.getElementById('eventsDropdown');
        if (dropdownButton && dropdownButton.firstChild) {
            dropdownButton.firstChild.textContent = buttonText;
        }
    } const iconUrl = urlParams.get('plugin_icon');
    if (iconUrl) {
        const preview = document.getElementById('iconPreview');
        const placeholder = document.getElementById('iconPlaceholder');
        if (preview && placeholder) {
            preview.src = iconUrl;
            preview.style.display = 'block';
            placeholder.style.display = 'none';
        }

        window.pluginIconUrl = iconUrl;
    }

    const pluginBannerUrl = urlParams.get('plugin_banner');
    if (pluginBannerUrl) {
        const bannerPreview = document.getElementById('bannerPreview');
        const bannerPlaceholder = document.getElementById('bannerPlaceholder');
        if (bannerPreview && bannerPlaceholder) {
            bannerPreview.src = pluginBannerUrl;
            bannerPreview.style.display = 'block';
            bannerPlaceholder.style.display = 'none';
        }

        window.pluginBannerUrl = pluginBannerUrl;
    }

    const bannerUrl = urlParams.get('plugin_banner');
    if (bannerUrl) {
        const preview = document.getElementById('bannerPreview');
        const placeholder = document.getElementById('bannerPlaceholder');
        if (preview && placeholder) {
            preview.src = bannerUrl;
            preview.style.display = 'block';
            placeholder.style.display = 'none';
        }

        window.pluginBannerUrl = bannerUrl;
    }

    if (urlParams.get('immediately_generate') === 'true') {
        generateProject();
    }

    const textarea = document.getElementById('description');
    if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = (textarea.scrollHeight) + 'px';
    }

    const eventsDropdown = document.getElementById('eventsDropdown');
    if (eventsDropdown) {
        document.getElementById('eventsDropdown').addEventListener('click', function (e) {
            e.stopPropagation();

            if (isAnimating) return;

            const dropdownContent = document.getElementById('eventsContent');
            const button = this;

            isDropdownOpen = !isDropdownOpen;

            if (isDropdownOpen) {
                openDropdown(dropdownContent, button);
            } else {
                closeDropdown(dropdownContent, button);
            }
        });
    } updateGameVersionPlaceholder();
    updateRuntimeVersionPlaceholder();
    scheduleVersionUpdates();
});

let isDropdownOpen = false;
let isAnimating = false;

function openDropdown(dropdownContent, button) {
    if (isAnimating) return;
    isAnimating = true;

    button.classList.add('active');

    const buttonRect = button.getBoundingClientRect();
    const spaceBelow = window.innerHeight - buttonRect.bottom;
    const dropdownHeight = 300;

    dropdownContent.style.display = 'block';
    const naturalHeight = Math.min(dropdownContent.scrollHeight, dropdownHeight);
    dropdownContent.style.display = '';

    if (spaceBelow < naturalHeight) {
        dropdownContent.classList.add('dropdown-up');
    } else {
        dropdownContent.classList.remove('dropdown-up');
    }

    dropdownContent.classList.add('show', 'animate-in');

    dropdownContent.addEventListener('animationend', function handler() {
        isAnimating = false;
        dropdownContent.removeEventListener('animationend', handler);
    });
}

function closeDropdown(dropdownContent, button) {
    if (isAnimating) return;
    isAnimating = true;

    button.classList.remove('active');
    dropdownContent.classList.add('animate-out');

    dropdownContent.addEventListener('animationend', function handler() {
        dropdownContent.classList.remove('show', 'animate-out');
        isAnimating = false;
        dropdownContent.removeEventListener('animationend', handler);
    });
}


function generateUUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

function sanitizePluginName(name) {
    if (name === '' || name === null) return '';
    const capitalized = name.replace(/(?:^|\s)([a-z])/g, function (match, letter) {
        return letter.toUpperCase();
    });

    return capitalized.replace(/[^a-zA-Z0-9\s]/g, '')
        .replace(/\s+/g, ' ')
        .replace(/\s/g, '');
}

function showError(message) {
    const errorDiv = document.getElementById('error-message');
    if (window.errorTimeout) {
        clearTimeout(window.errorTimeout);
    }
    errorDiv.textContent = message;
    void errorDiv.offsetWidth;
    errorDiv.classList.add('show');
    window.errorTimeout = setTimeout(() => {
        errorDiv.classList.remove('show');
    }, 4000);
}

function addShake(element) {
    element.classList.add('shake');
    element.addEventListener('animationend', () => {
        element.classList.remove('shake');
    }, { once: true });
}

function generateSolutionFile(identifiers) {
    return `Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.11.35312.102
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "${identifiers.name}", "${identifiers.name}/${identifiers.name}.csproj", "{${identifiers.projectGuid}}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|x64 = Debug|x64
        Release|x64 = Release|x64
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {${identifiers.projectGuid}}.Debug|x64.ActiveCfg = Debug|x64
        {${identifiers.projectGuid}}.Debug|x64.Build.0 = Debug|x64
        {${identifiers.projectGuid}}.Release|x64.ActiveCfg = Release|x64
        {${identifiers.projectGuid}}.Release|x64.Build.0 = Release|x64
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
    GlobalSection(ExtensibilityGlobals) = postSolution
        SolutionGuid = {${identifiers.solutionGuid}}
    EndGlobalSection
EndGlobal`;
}

function generateProjectFile(name, uuid) {
    return `<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Platforms>x64</Platforms>
    <LangVersion>13</LangVersion>
  </PropertyGroup>

	<PropertyGroup>
		<OnixPluginUUID>${uuid}</OnixPluginUUID>
	</PropertyGroup> 

	<Target Name="PostBuild" AfterTargets="PostBuildEvent">
		<Exec Command="del &quot;$(OutDir)OnixRuntime.pdb&quot; &gt; NUL" IgnoreExitCode="true" />
		<Exec Command="del &quot;$(OutDir)OnixRuntime.dll&quot; &gt; NUL" IgnoreExitCode="true" />
		<Exec Command="rm &quot;$(OutDir)OnixRuntime.pdb&quot; &gt; NUL" IgnoreExitCode="true" />
		<Exec Command="rm &quot;$(OutDir)OnixRuntime.dll&quot; &gt; NUL" IgnoreExitCode="true" />
		<Exec Condition="'$(ServerPluginBuild)' != 'TRUE'" Command="rmdir /Q /S &quot;$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\plugins\\$(OnixPluginUUID)&quot;" IgnoreExitCode="true" />
		<Exec Condition="'$(ServerPluginBuild)' != 'TRUE'" Command="xcopy /S /C /I /Y &quot;$(OutDir)&quot; &quot;$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\plugins\\$(OnixPluginUUID)&quot;" />
		<Exec Condition="'$(ServerPluginBuild)' != 'TRUE'" Command="xcopy /E /C /I /Y &quot;$(ProjectDir)Assets&quot; &quot;$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\plugins\\$(OnixPluginUUID)\\assets&quot;" />
		<Exec Condition="'$(ServerPluginBuild)' != 'TRUE'" Command="xcopy /C /Y &quot;$(ProjectDir)manifest.json&quot; &quot;$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\plugins\\$(OnixPluginUUID)&quot;" />
		<Exec Condition="'$(ServerPluginBuild)' != 'TRUE'" Command="echo OnixClient > &quot;$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\plugins\\$(OnixPluginUUID)\\CompletePluginFlag.txt&quot;" />
		<Exec Condition="'$(ServerPluginBuild)' == 'TRUE'" Command="echo OnixClient > &quot;$(SolutionDir)BuildSuccessful.txt&quot;" />
	</Target>

    <ItemGroup>
        <Reference Condition="'$(ServerPluginBuild)' == 'TRUE'" Include="OnixRuntime"> </Reference>
        <Reference Condition="'$(ServerPluginBuild)' != 'TRUE'" Include="OnixRuntime">
        <HintPath>$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\runtime\\OnixRuntime.dll</HintPath>
        </Reference>

        <Analyzer Condition="'$(ServerPluginBuild)' == 'TRUE'" Include="$(ServerPluginBuildRuntime)/OnixRuntime.CodeGenerators.dll"/>
        <Analyzer Condition="'$(ServerPluginBuild)' != 'TRUE'" Include="$(LOCALAPPDATA)\\Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\OnixClient\\Plugins\\runtime\\OnixRuntime.CodeGenerators.dll"/>
    </ItemGroup>

    <PropertyGroup Condition="'$(ServerPluginBuild)' == 'TRUE'">
        <AssemblySearchPaths>$(ServerPluginBuildRuntime);$(AssemblySearchPaths);</AssemblySearchPaths>
    </PropertyGroup>

	<ItemGroup>
	  <Folder Include="Assets\\" />
	</ItemGroup>

</Project>`;
}

const eventHandlers = {
    OnTick: {
        parameters: '',
        body: '',
        register: `Onix.Events.Common.Tick += OnTick;`,
        unregister: `Onix.Events.Common.Tick -= OnTick;`,
        usings: []
    },
    OnHudRender: {
        parameters: 'RendererGame gfx, float delta',
        body: '',
        register: `Onix.Events.Common.HudRender += OnHudRender;`,
        unregister: `Onix.Events.Common.HudRender -= OnHudRender;`,
        usings: ['using OnixRuntime.Api.Rendering;']
    },
    OnWorldRender: {
        parameters: 'RendererWorld gfx, float delta',
        body: '',
        register: `Onix.Events.Common.WorldRender += OnWorldRender;`,
        unregister: `Onix.Events.Common.WorldRender -= OnWorldRender;`,
        usings: ['using OnixRuntime.Api.Rendering;']
    },
    OnHudRenderDirect2D: {
        parameters: 'RendererDirect2D gfx, float delta',
        body: '',
        register: `Onix.Events.Common.HudRenderDirect2D += OnHudRenderDirect2D;`,
        unregister: `Onix.Events.Common.HudRenderDirect2D -= OnHudRenderDirect2D;`,
        usings: ['using OnixRuntime.Api.Rendering;']
    },
    OnHudInput: {
        parameters: 'InputKey key, bool isDown',
        body: '',
        register: `Onix.Events.Common.HudInput += OnHudInput;`,
        unregister: `Onix.Events.Common.HudInput -= OnHudInput;`,
        usings: ['using OnixRuntime.Api.Inputs;']
    },
    OnChatMessage: {
        parameters: 'string message, string username, string xuid, ChatMessageType type',
        body: '',
        register: `Onix.Events.Common.ChatMessage += OnChatMessage;`,
        unregister: `Onix.Events.Common.ChatMessage -= OnChatMessage;`,
        usings: []
    }
};

document.getElementById('eventsContent').addEventListener('click', function (e) {
    e.stopPropagation();
});


document.addEventListener('click', function (e) {
    if (isAnimating) return;

    const dropdownContent = document.getElementById('eventsContent');
    const dropdownButton = document.getElementById('eventsDropdown');

    if (!dropdownContent.contains(e.target) && !dropdownButton.contains(e.target) && isDropdownOpen) {
        isDropdownOpen = false;
        closeDropdown(dropdownContent, dropdownButton);
    }
});

document.querySelectorAll('#eventsContent input[type="checkbox"]').forEach(checkbox => {
    checkbox.addEventListener('click', function (e) {
        e.stopPropagation();
        updateDropdownText();
    });
});

function updateDropdownText() {
    const selectedCount = document.querySelectorAll('#eventsContent input:checked').length;
    const buttonText = selectedCount > 0 ? `Selected Events (${selectedCount})` : 'Select Events';
    const dropdownButton = document.getElementById('eventsDropdown');
    if (dropdownButton.firstChild) {
        dropdownButton.firstChild.textContent = buttonText;
    }
}

function generateEntryPointFile(name) {
    const selectedEvents = Array.from(document.querySelectorAll('#eventsContent input:checked'))
        .map(input => input.value);

    let methods = '';
    let registerEvents = '';
    let unregisterEvents = '';

    const usings = new Set(['using OnixRuntime.Api;', 'using OnixRuntime.Plugin;']);
    selectedEvents.forEach(event => {
        eventHandlers[event].usings.forEach(using => usings.add(using));
    });

    selectedEvents.forEach((event, index, array) => {
        methods += `        private void ${event}(${eventHandlers[event].parameters}) {
            
        }` + (index === array.length - 1 ? '' : '\n\n');

        registerEvents += `            ${eventHandlers[event].register}` + (index === array.length - 1 ? '' : '\n');
        unregisterEvents += `            ${eventHandlers[event].unregister}` + (index === array.length - 1 ? '' : '\n');
    });

    const registerSection = registerEvents ? `\n${registerEvents}` : '';
    const unregisterSection = unregisterEvents ? `\n${unregisterEvents}` : '';
    const methodsSection = methods ? `\n\n${methods}` : '';

    return `${Array.from(usings).join('\n')}

namespace ${name} {
    public class ${name} : OnixPluginBase {
        public static ${name} Instance { get; private set; } = null!;
        public static ${name}Config Config { get; private set; } = null!;

        public ${name}(OnixPluginInitInfo initInfo) : base(initInfo) {
            Instance = this;
            // If you can clean up what the plugin leaves behind manually, please do not unload the plugin when disabling.
            base.DisablingShouldUnloadPlugin = false;
#if DEBUG
           // base.WaitForDebuggerToBeAttached();
#endif
        }

        protected override void OnLoaded() {
            Console.WriteLine($"Plugin {CurrentPluginManifest.Name} loaded!");
            Config = new ${name}Config(PluginDisplayModule);${registerSection}
        }

        protected override void OnEnabled() {

        }

        protected override void OnDisabled() {

        }

        protected override void OnUnloaded() {
            // Ensure every task or thread is stopped when this function returns.
            // You can give them base.PluginEjectionCancellationToken which will be cancelled when this function returns. 
            Console.WriteLine($"Plugin {CurrentPluginManifest.Name} unloaded!");${unregisterSection}
        }${methodsSection}
    }
}`;
}

function generateConfigFile(name) {
    return `using OnixRuntime.Api.OnixClient;
namespace ${name} {
    public partial class ${name}Config : OnixModuleSettingRedirector {

    }
}`
}


async function generateProject() {
    if (!validateAllFields()) {
        showError('Please fill in all required fields.');
        return;
    }

    const generateButton = document.getElementById('generateProject');
    generateButton.classList.add('loading');
    updateButtonText(generateButton, 'Preparing project...');
    generateButton.style.paddingLeft = '44px';

    const formData = {
        pluginName: document.getElementById('pluginName').value,
        folderName: document.getElementById('folderName').value || sanitizePluginName(document.getElementById('pluginName').value) || '',
        pluginAuthor: document.getElementById('pluginAuthor').value,
        pluginDescription: document.getElementById('description').value,
        pluginVersion: document.getElementById('pluginVersion').value,
        gameVersion: document.getElementById('gameVersion').value || document.getElementById('gameVersion').placeholder,
        runtimeVersion: document.getElementById('runtimeVersion').value,
        repositoryUrl: document.getElementById('repositoryUrl').value,
        uuid: generateUUID(),
        hasBanner: !!document.getElementById('bannerFile').files[0] || !!window.pluginBannerUrl
    };
    if (!formData.pluginAuthor || !formData.pluginVersion || !formData.runtimeVersion) {
        showError('Please fill in all required fields.');
        generateButton.classList.remove('loading');
        generateButton.textContent = 'Generate Project';
        return;
    }

    if (!formData.pluginName) {
        showError('Plugin has an invalid name.');
        generateButton.classList.remove('loading');
        generateButton.textContent = 'Generate Project';
        return;
    }

    const identifiers = {
        name: formData.folderName,
        projectGuid: generateUUID().toUpperCase(),
        solutionGuid: generateUUID().toUpperCase()
    };

    try {
        const zip = new JSZip(); const projectFolder = zip.folder(formData.folderName);
        const sourceFolder = projectFolder.folder(formData.folderName);
        const assetsFolder = sourceFolder.folder('Assets');

        updateButtonText(generateButton, 'Loading resources...');

        async function fetchResource(url, type = 'icon') {
            if (!url) return null;

            try {
                const response = await fetch(url, { mode: 'cors', credentials: 'omit' });
                if (response.ok) {
                    const arrayBuffer = await response.arrayBuffer();
                    return {
                        data: arrayBuffer,
                        contentType: response.headers.get('content-type') || 'image/png'
                    };
                }
            } catch (directError) {
                console.log(`Direct fetch failed for ${type}:`, directError);
            }
            try {
                const proxyUrl = `https://api.allorigins.win/raw?url=${encodeURIComponent(url)}`;
                const proxyResponse = await fetch(proxyUrl);
                if (proxyResponse.ok) {
                    const arrayBuffer = await proxyResponse.arrayBuffer();
                    return {
                        data: arrayBuffer,
                        contentType: proxyResponse.headers.get('content-type') || 'image/png'
                    };
                }
            } catch (proxyError) {
                console.log(`Proxy fetch failed for ${type}:`, proxyError);
            }
            try {
                const img = new Image();
                const imageLoadPromise = new Promise((resolve, reject) => {
                    img.onload = () => resolve(img);
                    img.onerror = () => reject(new Error(`Failed to load ${type} image`));
                    setTimeout(() => reject(new Error(`${type} image load timeout`)), 5000);
                });
                img.crossOrigin = 'anonymous';
                img.src = url; const loadedImg = await imageLoadPromise;

                const canvas = document.createElement('canvas');
                canvas.width = loadedImg.width;
                canvas.height = loadedImg.height;
                const ctx = canvas.getContext('2d');
                ctx.drawImage(loadedImg, 0, 0);

                const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
                const arrayBuffer = await blob.arrayBuffer();
                return {
                    data: arrayBuffer,
                    contentType: 'image/png'
                };
            } catch (imgError) {
                console.error(`All ${type} loading methods failed:`, imgError);
                return null;
            }
        }
        const iconPromise = async () => {
            const iconFile = document.getElementById('iconFile').files[0];
            if (iconFile) {
                const iconData = await iconFile.arrayBuffer();
                return {
                    data: iconData,
                    contentType: 'image/png',
                    source: 'file'
                };
            } else if (window.pluginIconUrl) {
                const result = await fetchResource(window.pluginIconUrl, 'icon');
                if (result) {
                    return {
                        ...result,
                        source: 'url'
                    };
                }
            }
            return null;
        };

        const bannerPromise = async () => {
            const bannerFile = document.getElementById('bannerFile').files[0];
            if (bannerFile) {
                const bannerData = await bannerFile.arrayBuffer();
                return {
                    data: bannerData,
                    contentType: bannerFile.type,
                    source: 'file'
                };
            } else if (window.pluginBannerUrl) {
                const result = await fetchResource(window.pluginBannerUrl, 'banner');
                if (result) {
                    return {
                        ...result,
                        source: 'url'
                    };
                }
            }
            return null;
        };
        const [iconResult, bannerResult] = await Promise.all([
            iconPromise(),
            bannerPromise()
        ]);

        if (iconResult) {
            assetsFolder.file('PluginIcon.png', iconResult.data, { binary: true });
        }
        if (bannerResult) {
            assetsFolder.file('PluginBanner.png', bannerResult.data, { binary: true });
        }
        updateButtonText(generateButton, 'Creating files...');

        sourceFolder.file('manifest.json', JSON.stringify({
            uuid: formData.uuid,
            plugin_name: formData.pluginName,
            plugin_author: formData.pluginAuthor,
            plugin_description: formData.pluginDescription,
            plugin_version: formData.pluginVersion,
            game_version: formData.gameVersion,
            supported_game_version_ranges: [ "0.0.0-9.99.9" ],
            runtime_version: parseInt(formData.runtimeVersion),
            target_assembly: `${formData.folderName}.dll`,
            repository_url: formData.repositoryUrl || undefined
        }, null, 2));

        sourceFolder.file(`${formData.folderName}.cs`, generateEntryPointFile(formData.folderName));
        sourceFolder.file(`${formData.folderName}.csproj`, generateProjectFile(formData.folderName, formData.uuid));
        sourceFolder.file(`${formData.folderName}Config.cs`, generateConfigFile(formData.folderName));
        projectFolder.file(`${formData.folderName}.sln`, generateSolutionFile(identifiers));

        updateButtonText(generateButton, 'Generating Project...');
        const content = await zip.generateAsync({
            type: 'blob',
            compression: 'DEFLATE',
            compressionOptions: { level: 1 }
        });

        const url = URL.createObjectURL(content);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${formData.folderName}.zip`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    } catch (error) {
        console.error('Error in project generation:', error);
        showError('Error generating project: ' + error.message);
    } finally {
        setTimeout(() => {
            generateButton.classList.remove('loading');
            generateButton.textContent = 'Generate Project';
            generateButton.style.paddingLeft = '';
        }, 1500);
    }
}

function updateButtonText(button, text) {
    button.classList.add('loading-text-fade');
    button.textContent = text;

    setTimeout(() => {
        button.classList.remove('loading-text-fade');
    }, 300);
}

const textarea = document.getElementById('description');
textarea.addEventListener('input', function () {
    this.style.height = 'auto';
    this.style.height = (this.scrollHeight) + 'px';
});

window.addEventListener('load', () => {
    textarea.style.height = 'auto';
    textarea.style.height = (textarea.scrollHeight) + 'px';
});

document.querySelectorAll('input[required]').forEach(input => {
    input.addEventListener('blur', function () {
        if (!this.value) {
            this.style.borderColor = '#ff3b30';
        } else {
            this.style.borderColor = 'rgba(255, 255, 255, 0.1)';
        }
    });
});

document.getElementById('iconFile').addEventListener('change', function (e) {
    const file = e.target.files[0];
    if (file) {
        if (file.type !== 'image/png') {
            showError('Icon must be a PNG file');
            this.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = function (event) {
            const preview = document.getElementById('iconPreview');
            const placeholder = document.getElementById('iconPlaceholder');
            if (preview && placeholder) {
                preview.src = event.target.result;
                preview.style.display = 'block';
                placeholder.style.display = 'none';
            }
        };
        reader.onerror = function (error) {
            console.error('Error reading file:', error);
        };
        reader.readAsDataURL(file);
    }
});

document.getElementById('bannerFile').addEventListener('change', function (e) {
    const file = e.target.files[0];
    if (file) {
        if (file.type !== 'image/png' && file.type !== 'image/jpeg') {
            showError('Banner must be a PNG or JPEG file');
            this.value = '';
            return;
        }

        const reader = new FileReader();
        reader.onload = function (event) {
            const preview = document.getElementById('bannerPreview');
            const placeholder = document.getElementById('bannerPlaceholder');
            if (preview && placeholder) {
                preview.src = event.target.result;
                preview.style.display = 'block';
                placeholder.style.display = 'none';
            }
        };
        reader.onerror = function (error) {
            console.error('Error reading file:', error);
        };
        reader.readAsDataURL(file);
    }
});

let folderNameManuallySet = false;

document.getElementById('pluginName').addEventListener('input', function () {
    const folderNameInput = document.getElementById('folderName');
    if (!folderNameManuallySet) {
        folderNameInput.value = sanitizePluginName(this.value);
    }
});

document.getElementById('folderName').addEventListener('input', function () {
    folderNameManuallySet = true;
});

document.getElementById('folderName').addEventListener('change', function () {
    if (!this.value) {
        folderNameManuallySet = false;
        if (document.getElementById('pluginName').value) {
            this.value = sanitizePluginName(document.getElementById('pluginName').value);
        }
    }
});

function validateField(input) {
    if (input.hasAttribute('required') && !input.value.trim()) {
        input.style.borderColor = '#ff3b30';
        return false;
    } else {
        input.style.borderColor = 'rgba(255, 255, 255, 0.1)';
        return true;
    }
}

function validateAllFields() {
    const requiredFields = document.querySelectorAll('.form-group input[required], .form-group textarea[required], input[required], textarea[required]');
    let isValid = true;
    let firstInvalid = null;

    requiredFields.forEach(field => {
        if (!field.value.trim()) {
            field.style.borderColor = '#ff3b30';
            isValid = false;
            if (!firstInvalid) firstInvalid = field;
        } else {
            field.style.borderColor = 'rgba(255, 255, 255, 0.1)';
        }
    });

    if (!isValid) {
        if (firstInvalid) firstInvalid.focus();
        showError('Please fill in all required fields.');
    }

    return isValid;
}

document.querySelectorAll('input[required], textarea[required]').forEach(input => {
    input.addEventListener('blur', function () {
        validateField(this);
    });
});

document.getElementById('generateProject').addEventListener('click', function (e) {
    e.preventDefault();
    if (!validateAllFields()) {
        return;
    }
    generateProject();
});

document.getElementById('generateProject').insertAdjacentHTML('afterend', `
    <div class="utility-controls">
        <div class="utility-toggle" id="instantGenerateToggle">
            <label class="toggle-label" for="immediateToggle">
                Instant Generate
            </label>
            <label class="toggle">
                <input type="checkbox" id="immediateToggle">
                <span class="toggle-slider"></span>
            </label>
            <div class="utility-tooltip">When enabled, the plugin will be generated immediately when you open the URL</div>
        </div>
        <button class="utility-button" id="copyParams">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
            </svg>
            <span>Copy URL</span>
        </button>
    </div>
`);

const toggleContainer = document.getElementById('instantGenerateToggle');
let tooltipTimeout;
let tooltipDelayTimeout;

toggleContainer.addEventListener('mouseenter', () => {
    clearTimeout(tooltipTimeout);
    clearTimeout(tooltipDelayTimeout);

    tooltipDelayTimeout = setTimeout(() => {
        const tooltip = toggleContainer.querySelector('.utility-tooltip');
        tooltip.classList.add('show');
    }, 500);
});

toggleContainer.addEventListener('mouseleave', () => {
    clearTimeout(tooltipDelayTimeout);

    const tooltip = toggleContainer.querySelector('.utility-tooltip');
    tooltip.classList.add('hiding');
    tooltipTimeout = setTimeout(() => {
        tooltip.classList.remove('show', 'hiding');
    }, 200);
});

let copyTimeout;
let immediateGenerateEnabled = urlParams.get('immediately_generate') === 'true';
const copyButton = document.getElementById('copyParams');
const originalHTML = copyButton.innerHTML;

document.getElementById('immediateToggle').checked = immediateGenerateEnabled;

document.getElementById('immediateToggle').addEventListener('change', function () {
    immediateGenerateEnabled = this.checked;
});

copyButton.addEventListener('click', async function () {
    if (copyTimeout) {
        clearTimeout(copyTimeout);
        copyButton.innerHTML = originalHTML;
    } const form = {
        plugin_name: document.getElementById('pluginName').value,
        plugin_author: document.getElementById('pluginAuthor').value,
        plugin_description: document.getElementById('description').value,
        repository_url: document.getElementById('repositoryUrl').value,
        folder_name: document.getElementById('folderName').value,
        plugin_version: document.getElementById('pluginVersion').value,
        game_version: document.getElementById('gameVersion').value,
        runtime_version: document.getElementById('runtimeVersion').value,
        plugin_icon: window.pluginIconUrl || '',
        plugin_banner: window.pluginBannerUrl || '',
        events: Array.from(document.querySelectorAll('#eventsContent input:checked'))
            .map(cb => cb.value)
            .join(','),
        immediately_generate: immediateGenerateEnabled
    };

    const params = new URLSearchParams();
    Object.entries(form).forEach(([key, value]) => {
        if (value) params.append(key, value);
    });

    const baseUrl = window.location.origin;
    const fullUrl = `${baseUrl}${window.location.pathname}?${params.toString()}`;

    const copyMethods = [
        async () => {
            if (!navigator.clipboard || !window.isSecureContext) throw new Error('Clipboard API not available');
            await navigator.clipboard.writeText(fullUrl);
        },
        async () => {
            const result = await navigator.permissions.query({ name: 'clipboard-write' });
            if (result.state === 'granted' || result.state === 'prompt') {
                await navigator.clipboard.writeText(fullUrl);
            } else {
                throw new Error('No clipboard permission');
            }
        },
        async () => {
            const textArea = document.createElement('textarea');
            textArea.value = fullUrl;
            textArea.style.position = 'absolute';
            textArea.style.opacity = '0';
            document.body.appendChild(textArea);
            textArea.select();

            let success = false;
            try {
                success = document.execCommand('copy');
            } finally {
                document.body.removeChild(textArea);
            }

            if (!success) throw new Error('execCommand failed');
        }
    ];

    let copied = false;
    let lastError;

    for (const method of copyMethods) {
        try {
            await method();
            copied = true;
            break;
        } catch (err) {
            console.log('Copy method failed:', err);
            lastError = err;
            continue;
        }
    }

    if (copied) {
        copyButton.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg><span>Copied!</span>';
        copyTimeout = setTimeout(() => {
            copyButton.innerHTML = originalHTML;
            copyTimeout = null;
        }, 2000);
    } else {
        console.error('All copy methods failed:', lastError);
        showError(`Failed to copy automatically. URL: ${fullUrl}`);
    }
});

document.getElementById('gameVersion').addEventListener('blur', function () {
    if (!this.value.trim()) {
        this.style.borderColor = 'rgba(255, 255, 255, 0.1)';
    }
});

function scheduleVersionUpdates() {
    updateGameVersionPlaceholder();
    updateRuntimeVersionPlaceholder();

    setInterval(() => {
        updateGameVersionPlaceholder();
        updateRuntimeVersionPlaceholder();
    }, 60 * 60 * 1000);
}

function populateGameVersionDropdown(versionData) {
    const dropdown = document.getElementById('gameVersionDropdown');

    dropdown.innerHTML = '';

    if (versionData && Array.isArray(versionData) && versionData.length > 0) {
        versionData.forEach(version => {
            addVersionOption(dropdown, version.version);
        });

        if (dropdown.options.length > 0) {
            dropdown.selectedIndex = 0;
        }

        const gameVersionInput = document.getElementById('gameVersion');
        if (gameVersionInput && dropdown.options.length > 0) {
            gameVersionInput.placeholder = dropdown.options[0].value;
        }
    } else {
        ensureGameVersionAvailable();
    }
}

function addVersionOption(selectElement, version) {
    for (let i = 0; i < selectElement.options.length; i++) {
        if (selectElement.options[i].value === version) {
            return false;
        }
    }

    const option = document.createElement('option');
    option.value = version;
    option.textContent = version;
    selectElement.appendChild(option);
    return true;
}

function ensureGameVersionAvailable() {
    const dropdown = document.getElementById('gameVersionDropdown');
    const gameVersionInput = document.getElementById('gameVersion');

    if (!dropdown || !gameVersionInput) return;

    if (dropdown.options.length === 1 && dropdown.options[0].value === "") {
        dropdown.innerHTML = '';
    }

    const cachedVersionList = localStorage.getItem('gameVersionList');
    if (cachedVersionList) {
        try {
            const versions = JSON.parse(cachedVersionList);
            if (versions && Array.isArray(versions) && versions.length > 0) {
                versions.forEach(version => {
                    addVersionOption(dropdown, version.version);
                });
                return;
            }
        } catch (error) {
            console.error('Failed to parse cached version list:', error);
        }
    }

    const cachedVersion = localStorage.getItem('latestGameVersion');
    if (cachedVersion) {
        addVersionOption(dropdown, cachedVersion);
    }

    const commonVersions = ['1.20.80', '1.20.70', '1.20.60', '1.20.50', '1.20.40', '1.20.30'];
    let addedAny = false;

    commonVersions.forEach(version => {
        if (addVersionOption(dropdown, version)) {
            addedAny = true;
        }
    });

    if (addedAny) {
        console.log('Used fallback common versions for the dropdown');
    }

    if (dropdown.options.length > 0) {
        gameVersionInput.placeholder = dropdown.options[0].value;
    }
}