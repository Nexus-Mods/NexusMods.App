using System.Diagnostics;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.StardewValley.Models;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using SMAPIManifest = StardewModdingAPI.Toolkit.Serialization.Models.Manifest;

namespace NexusMods.Games.StardewValley.Installers;

public class SMAPIModInstaller : ALibraryArchiveInstaller, IModInstaller
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

    public async ValueTask<IEnumerable<ModInstallerResult>> GetModsAsync(
        ModInstallerInfo info,
        CancellationToken cancellationToken = default)
    {
        var manifestFiles = await GetManifestFiles(info.ArchiveFiles);
        if (manifestFiles.Count == 0)
            return [];

        var mods = manifestFiles
            .Select(found =>
            {
                var (manifestFile, manifest) = found;
                var parent = manifestFile.Parent();

                var modFiles = parent!.Item
                    .GetFiles<ModFileTree, RelativePath>()
                    .Select(kv =>
                        {
                            var storedFile = kv.ToStoredFile(
                                new GamePath(
                                    LocationId.Game,
                                    Constants.ModsFolder.Join(kv.Path().DropFirst(parent.Depth() - 1))
                                )
                            );

                            if (!kv.Equals(manifestFile)) return storedFile;
                            
                            storedFile.Add(SMAPIManifestMetadata.SMAPIManifest, Null.Instance);
                            return storedFile;
                        }
                    );

                return new ModInstallerResult
                {
                    Id = info.BaseModId,
                    Files = modFiles,
                    Name = manifest.Name,
                    Version = manifest.Version.ToString(),
                    Metadata = new TempEntity
                    {
                        SMAPIModMarker.IsSMAPIMod,
                    },
                };
            });

        return mods;
    }

    public override async ValueTask<InstallerResult> ExecuteAsync(
        LibraryArchive.ReadOnly libraryArchive,
        LoadoutItemGroup.New loadoutGroup,
        ITransaction transaction,
        Loadout.ReadOnly loadout,
        CancellationToken cancellationToken)
    {
        var manifestFileTuples = await GetManifestsAsync(libraryArchive, cancellationToken);
        if (manifestFileTuples.Count == 0) return new NotSupported();

        foreach (var tuple in manifestFileTuples)
        {
            var (manifestFileEntry, manifest) = tuple;
            var parent = manifestFileEntry.Path.Parent;

            var smapiModGroup = new LoadoutItemGroup.New(transaction, out var smapiModEntityId)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(transaction, smapiModEntityId)
                {
                    Name = manifest.Name,
                    LoadoutId = loadout,
                    ParentId = loadoutGroup,
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
                        IsManifestFile = true,
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

        return new Success();
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
