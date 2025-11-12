using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Library;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Installers;

/// <summary>
/// An installer for the 'Bannerlord Software Extender' (BLSE):<br/>
/// - https://www.nexusmods.com/mountandblade2bannerlord/mods/1?tab=description
/// <br/><br/>
/// BLSE ships in approximately this folder structure:
///
/// <code>
/// 📁 bin
///     📁 Gaming.Desktop.x64_Shipping_Client
///         📄 Bannerlord.BLSE.Launcher.exe (139.3 kB)
///         📄 Bannerlord.BLSE.LauncherEx.exe (375.8 kB)
///         📄 Bannerlord.BLSE.Shared.dll (2.7 MB)
///         📄 Bannerlord.BLSE.Standalone.exe (139.3 kB)
///     📁 Win64_Shipping_Client
///         📄 Bannerlord.BLSE.Launcher.exe (139.3 kB)
///         📄 Bannerlord.BLSE.Launcher.exe.config (156 B)
///         📄 Bannerlord.BLSE.LauncherEx.exe (375.8 kB)
///         📄 Bannerlord.BLSE.LauncherEx.exe.config (156 B)
///         📄 Bannerlord.BLSE.Shared.dll (2.7 MB)
///         📄 Bannerlord.BLSE.Standalone.exe (139.3 kB)
///         📄 Bannerlord.BLSE.Standalone.exe.config (156 B)
/// </code>
///
/// This installer will extract the files in the `bin` folder from either `Win64_Shipping_Client` or `Gaming.Desktop.x64_Shipping_Client`
/// based on the user's game store.
/// </summary>
// ReSharper disable once InconsistentNaming
public class BLSEInstaller : ALibraryArchiveInstaller
{
    /// <summary/>
    public BLSEInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<BLSEInstaller>>()) { }

    /// <inheritdoc/>
    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive, LoadoutItemGroup.New loadoutGroup, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        const string launcherFileName = "Bannerlord.BLSE.Launcher.exe";
        
        var store = loadout.InstallationInstance.Store;
        var installDir = store == GameStore.XboxGamePass ? (RelativePath)"bin/Gaming.Desktop.x64_Shipping_Client" : (RelativePath)"bin/Win64_Shipping_Client";

        // Check if we are BLSE, we'll do a simple file check to determine this.
        var hasBlseLauncher = libraryArchive.Children.Any(x => x.Path.StartsWith(installDir / launcherFileName));
        if (!hasBlseLauncher) return ValueTask.FromResult((InstallerResult)(new NotSupported(Reason: $"Archive doesn't contain a file named `{launcherFileName}`")));

        // This is the group which contains the files for BLSE.
        var modGroup = new LoadoutItemGroup.New(transaction, out var modGroupEntityId)
        {
            IsGroup = true,
            LoadoutItem = new LoadoutItem.New(transaction, modGroupEntityId)
            {
                Name = "Bannerlord Software Extender (BLSE)",
                LoadoutId = loadout,
                ParentId = loadoutGroup,
            },
        };
        
        // Get the files for the given game store.
        var folderEntries = libraryArchive.Children.Where(x => x.Path.StartsWith(installDir));

        // Assign each subfolder 
        foreach (var fileEntry in folderEntries)
        {
            _ = new LoadoutFile.New(transaction, out var entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    // Note(sewer): Path of file inside archive matches that on FileSystem.
                    //              So we don't need to compute it, as we've filtered it in
                    //              the 'where' clause above.
                    TargetPath = (loadout.Id, LocationId.Game, fileEntry.Path),
                    LoadoutItem = new LoadoutItem.New(transaction, entityId)
                    {
                        Name = fileEntry.AsLibraryFile().FileName,
                        LoadoutId = loadout,
                        ParentId = modGroup,
                    },
                },
            };
        }
        
        return ValueTask.FromResult((InstallerResult)(new Success()));
    }
}
