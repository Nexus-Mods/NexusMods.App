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
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.UnrealEngine.Emitters;
using NexusMods.Games.UnrealEngine.Installers;
using NexusMods.Games.UnrealEngine.Interfaces;
using NexusMods.Paths;

namespace NexusMods.Games.UnrealEngine;

public abstract class AUnrealEngineGame : AGame, IUnrealEngineGameAddon
{
    public abstract string GameFolderName { get; }
    
    public virtual NamedLink UE4SSLink => Helpers.UE4SSLink;
    public virtual IEnumerable<FAesKey>? AESKeys => null;
    public virtual VersionContainer? VersionContainer => VersionContainer.DEFAULT_VERSION_CONTAINER;
    public virtual IStreamFactory? MemberVariableTemplate => null;

    protected readonly IServiceProvider _serviceProvider;
    protected readonly IFileSystem _fs;
    // protected readonly IOSInformation _osInformation;

    protected AUnrealEngineGame(IServiceProvider provider) : base(provider)
    {
        _serviceProvider = provider;
        _fs = provider.GetRequiredService<IFileSystem>();
        // _osInformation = provider.GetRequiredService<IOSInformation>();
    }

    protected override IReadOnlyDictionary<LocationId, AbsolutePath> GetLocations(IFileSystem fileSystem, GameLocatorResult installation)
    {
        return Utils.StandardUnrealEngineLocations(fileSystem, installation, GameFolderName);
    }

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
