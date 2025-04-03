using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Games.UnrealEngine.Emitters;
using NexusMods.Games.UnrealEngine.Installers;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public abstract class AUnrealEngineGame(IServiceProvider provider) : AGame(provider), IUnrealEngineGameAddon
{
    protected readonly IServiceProvider _serviceProvider = provider;
    protected readonly IFileSystem _fs = provider.GetRequiredService<IFileSystem>();
    
    public virtual RelativePath RelPathGameName => new("");
    public virtual RelativePath? RelPathPakMods => RelPathGameName.Join(new RelativePath("Content/Paks/~mods"));
    public virtual NamedLink UE4SSLink => Helpers.UE4SSLink;
    public virtual VersionContainer? VersionContainer => VersionContainer.DEFAULT_VERSION_CONTAINER;
    public virtual IEnumerable<FAesKey>? AESKeys => null;
    public virtual IStreamFactory? MemberVariableTemplate => null;
    
    public virtual Func<GameLocatorResult, RelativePath>? ArchitectureSegmentRetriever
    {
        get
        {
            return (installation) => installation.Store == GameStore.XboxGamePass
                ? new RelativePath("WinGDK")
                : new RelativePath("Win64");
        }
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
        => Utils.StandardUnrealEngineLocations(fileSystem, installation, this);

    public override ILibraryItemInstaller[] LibraryItemInstallers => [
        _serviceProvider.GetRequiredService<ScriptingSystemInstaller>(),
        _serviceProvider.GetRequiredService<ScriptingSystemLuaInstaller>(),
        _serviceProvider.GetRequiredService<UnrealEnginePakModInstaller>(),
    ];

    public override List<IModInstallDestination> GetInstallDestinations(IReadOnlyDictionary<LocationId, AbsolutePath> locations)
        => ModInstallDestinationHelpers.GetCommonLocations(locations);
    
    public override IDiagnosticEmitter[] DiagnosticEmitters =>
    [
        _serviceProvider.GetRequiredService<AssetConflictDiagnosticEmitter>(),
        _serviceProvider.GetRequiredService<ModOverwritesGameFilesEmitter>(),
        _serviceProvider.GetRequiredService<MissingScriptingSystemEmitter>(),
    ];
}
