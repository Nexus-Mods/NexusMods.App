using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGameInstaller : ALibraryArchiveInstaller
{
    private readonly RelativePath _preferencesPrefix = "preferences".ToRelativePath();
    private readonly RelativePath _savesPrefix = "saves".ToRelativePath();
    public StubbedGameInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<StubbedGameInstaller>>())
    {
    }
    
    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction tx,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var modFiles = LibraryArchiveTreeExtensions.GetTree(libraryArchive).GetFiles()
            .Select(kv =>
            {
                var path = kv.Item.Path;
                if (path.Path.StartsWith(_preferencesPrefix))
                    return kv.ToLoadoutFile(loadout, loadoutGroup, tx, new GamePath(LocationId.Preferences, path));

                if (path.Path.StartsWith(_savesPrefix))
                    return kv.ToLoadoutFile(loadout, loadoutGroup, tx, new GamePath(LocationId.Saves, path));

                return kv.ToLoadoutFile(loadout, loadoutGroup, tx, new GamePath(LocationId.Game, path));
            })
            .ToArray();

        return modFiles.Length == 0
            ? ValueTask.FromResult<InstallerResult>(new NotSupported(Reason: "Archive contains no files"))
            : ValueTask.FromResult<InstallerResult>(new Success());
    }
}
