using CUE4Parse.Encryption.Aes;
using CUE4Parse.UE4.Versions;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Diagnostics.Values;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.Abstractions.Games;
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
    public abstract NamedLink UE4SSLink { get; }
    public abstract FAesKey? AESKey { get; }
    public abstract VersionContainer? VersionContainer { get; }

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
    
    public override Version GetLocalVersion(GameInstallMetadata.ReadOnly installation)
    {
        try
        {
            var executableGamePath = GetPrimaryFile(installation.Store);
            var fvi = executableGamePath
                .Combine(_fs.FromUnsanitizedFullPath(installation.Path)).FileInfo
                .GetFileVersionInfo();
            return fvi.ProductVersion;
        }
        catch (Exception)
        {
            return new Version(0, 0, 0, 0);
        }
    }
}
