using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Collections;

internal class FallbackCollectionDownloadInstaller : ALibraryArchiveInstaller
{
    private readonly GamePath _defaultPath;

    private FallbackCollectionDownloadInstaller(IServiceProvider serviceProvider, GamePath defaultPath)
        : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<FallbackCollectionDownloadInstaller>>())
    {
        _defaultPath = defaultPath;
    }

    public static ILibraryItemInstaller? Create(IServiceProvider serviceProvider, IGame game)
    {
        var defaultPath = game.GetFallbackCollectionInstallDirectory();
        if (!defaultPath.HasValue) return null;
        return new FallbackCollectionDownloadInstaller(serviceProvider, defaultPath.Value);
    }

    public override ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation("Using fallback collection installer for `{Name}`", libraryArchive.AsLibraryFile().AsLibraryItem().Name);

        foreach (var fileEntry in libraryArchive.Children)
        {
            var to = new GamePath(_defaultPath.LocationId, _defaultPath.Path.Join(fileEntry.Path));

            _ = new LoadoutFile.New(transaction, out var entityId)
            {
                Hash = fileEntry.AsLibraryFile().Hash,
                Size = fileEntry.AsLibraryFile().Size,
                LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                {
                    TargetPath = to.ToGamePathParentTuple(loadout.Id),
                    LoadoutItem = new LoadoutItem.New(transaction, entityId)
                    {
                        Name = fileEntry.AsLibraryFile().FileName,
                        LoadoutId = loadout,
                        ParentId = loadoutGroup,
                    },
                },
            };
        }

        return ValueTask.FromResult<InstallerResult>(new Success());
    }
}
