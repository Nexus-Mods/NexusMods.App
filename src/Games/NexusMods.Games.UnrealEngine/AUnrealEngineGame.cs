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
    private readonly IServiceProvider _serviceProvider = provider;

    // The relative path to the folder containing the content and binaries directories.
    //  this is usually some sort of codename (e.g. Avowed => Alabama)
    public virtual RelativePath RelPathGameName => new("");

    // The default location for pak mods (not to be confused with blueprint mods) - usually the ~mods folder.
    public virtual RelativePath? RelPathPakMods => RelPathGameName.Join(new RelativePath("Content/Paks/~mods"));

    // The link which the user should follow in order to install UE4SS.
    public virtual NamedLink UE4SSLink => Helpers.UE4SSLink;

    // The game engine version - CUE4Parse will try to detect this automatically when the
    //  default version container is provided.
    public virtual VersionContainer? VersionContainer => VersionContainer.DEFAULT_VERSION_CONTAINER;

    // AES keys used to decrypt the game's pak files.
    public virtual IEnumerable<FAesKey>? AESKeys => null;

    // Certain games (e.g. Palworld) require different offsets for its variables.
    public virtual IStreamFactory? MemberVariableTemplate => null;

    // The architecture segment in the path to the game's binaries. Usually Win64 or WinGDK.
    public virtual Func<GameLocatorResult, RelativePath>? ArchitectureSegmentRetriever
    {
        get
        {
            return (installation) => installation.Store == GameStore.XboxGamePass
                ? new RelativePath("WinGDK")
                : new RelativePath("Win64");
        }
    }

    // Retrieve the standard locations for the game.
    //  We currently offer support for:
    //  - Pak mods
    //  - Blueprint/Logic mods
    //  - Lua mods
    //  - Config files
    //  - Save files
    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
        => Utils.StandardUnrealEngineLocations(fileSystem, installation, this);

    // By default, all UE games support UE4SS, its LUA scripting system and pak mods.
    public override ILibraryItemInstaller[] LibraryItemInstallers =>
    [
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
