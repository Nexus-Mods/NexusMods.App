using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Trees.Traits;

namespace NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

public class StubbedGameInstaller : ALibraryArchiveInstaller
{
    private readonly RelativePath _preferencesPrefix = "preferences".ToRelativePath();
    private readonly RelativePath _savesPrefix = "saves".ToRelativePath();
    public StubbedGameInstaller(IServiceProvider serviceProvider) : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<StubbedGameInstaller>>())
    {
    }
    
    public override ValueTask<LoadoutItem.New[]> ExecuteAsync(LibraryArchive.ReadOnly libraryArchive, ITransaction transaction, Loadout.ReadOnly loadout, CancellationToken cancellationToken)
    {
        var group = libraryArchive.ToGroup(loadout, transaction, out var loadoutItem);
        
        var modFiles = libraryArchive.GetTree().GetFiles()
            .Select(kv =>
            {
                var path = kv.Item.Path;
                if (path.Path.StartsWith(_preferencesPrefix))
                    return kv.ToLoadoutFile(loadout, group, transaction, new GamePath(LocationId.Preferences, path));

                if (path.Path.StartsWith(_savesPrefix))
                    return kv.ToLoadoutFile(loadout, group, transaction, new GamePath(LocationId.Saves, path));

                return kv.ToLoadoutFile(loadout, group, transaction, new GamePath(LocationId.Game, path));
            });

        return ValueTask.FromResult<LoadoutItem.New[]>([loadoutItem]);
    }
}
