using System.Diagnostics;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Installers;

public class SMAPIModInstaller : ALibraryArchiveInstaller
{
    private readonly IFileStore _fileStore;

    public SMAPIModInstaller(IServiceProvider serviceProvider)
        : base(serviceProvider, serviceProvider.GetRequiredService<ILogger<SMAPIModInstaller>>())
    {
        _fileStore = serviceProvider.GetRequiredService<IFileStore>();
    }

    private async ValueTask<List<(KeyedBox<RelativePath, ModFileTree>, SMAPIManifest)>> GetManifestFiles(
        KeyedBox<RelativePath, ModFileTree> files)
    {
        var results = new List<(KeyedBox<RelativePath, ModFileTree>, SMAPIManifest)>();
        foreach (var kv in files.GetFiles())
        {
            if (!kv.FileName().Equals(Constants.ManifestFile)) continue;

            try
            {
                await using var stream = await kv.Item.OpenAsync();
                var manifest = await Interop.DeserializeManifest(stream);
                if (manifest is not null) results.Add((kv, manifest));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception trying to deserialize {File}", kv.Path());
            }
        }

        return results;
    }
    
    public override async ValueTask<LoadoutItem.New[]> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var manifestFileTuples = await GetManifestsAsync(libraryArchive, cancellationToken);
        if (manifestFileTuples.Count == 0) return [];

        var group = libraryArchive.ToGroup(loadout, transaction, out var groupLoadoutItem);

        foreach (var tuple in manifestFileTuples)
        {
            var (manifestFileEntry, manifest) = tuple;
            var parent = manifestFileEntry.Path.Parent;

            var smapiModGroup = new LoadoutItemGroup.New(transaction, out var smapiModEntityId)
            {
                IsIsLoadoutItemGroupMarker = true,
                LoadoutItem = new LoadoutItem.New(transaction, smapiModEntityId)
                {
                    Name = manifest.Name,
                    LoadoutId = loadout,
                    ParentId = group,
                },
            };

            var manifestLoadoutItemId = Optional<EntityId>.None;
            foreach (var fileEntry in libraryArchive.Children.Where(x => x.Path.InFolder(parent)))
            {
                var to = new GamePath(LocationId.Game, Constants.ModsFolder.Join(fileEntry.Path.DropFirst(parent.Depth - 1)));

                var loadoutFile = new LoadoutFile.New(transaction, out var entityId)
                {
                    Hash = fileEntry.AsLibraryFile().Hash,
                    Size = fileEntry.AsLibraryFile().Size,
                    LoadoutItemWithTargetPath = new LoadoutItemWithTargetPath.New(transaction, entityId)
                    {
                        TargetPath = to,
                        LoadoutItem = new LoadoutItem.New(transaction, entityId)
                        {
                            Name = fileEntry.AsLibraryFile().FileName,
                            LoadoutId = loadout,
                            ParentId = smapiModGroup,
                        },
                    },
                };

                if (fileEntry.Id == manifestFileEntry.Id)
                {
                    manifestLoadoutItemId = entityId;
                    _ = new SMAPIManifestLoadoutFile.New(transaction, entityId)
                    {
                        IsIsManifestMarker = true,
                        LoadoutFile = loadoutFile,
                    };
                }
            }

            Debug.Assert(manifestLoadoutItemId.HasValue);

            _ = new SMAPIModLoadoutItem.New(transaction, smapiModEntityId)
            {
                ManifestId = manifestLoadoutItemId.Value,
                LoadoutItemGroup = smapiModGroup,
            };
        }

        return [groupLoadoutItem];
    }

    private async ValueTask<List<ValueTuple<LibraryArchiveFileEntry.ReadOnly, SMAPIManifest>>> GetManifestsAsync(
        LibraryArchive.ReadOnly libraryArchive,
        CancellationToken cancellationToken)
    {
        var results = new List<(LibraryArchiveFileEntry.ReadOnly, SMAPIManifest)>();

        foreach (var fileEntry in libraryArchive.Children)
        {
            if (!fileEntry.Path.FileName.Equals(Constants.ManifestFile)) continue;

            try
            {
                await using var stream = await _fileStore.GetFileStream(fileEntry.AsLibraryFile().Hash, token: cancellationToken);
                var manifest = await Interop.DeserializeManifest(stream);
                if (manifest is null) continue;

                results.Add((fileEntry, manifest));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while deserializing {Path} from {Archive}", fileEntry.Path, fileEntry.Parent.AsLibraryFile().FileName);
            }
        }

        return results;
    }
}
