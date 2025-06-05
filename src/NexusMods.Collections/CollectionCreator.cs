using DynamicData.Kernel;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Hashes;
using CollectionMod = NexusMods.Abstractions.Collections.Json.Mod;

namespace NexusMods.Collections;

public static class CollectionCreator
{
    private static string GenerateNewCollectionName(string[] allNames)
    {
        const string defaultValue = "My new collection";
        const string template = "({0}) My new collection";
        var count = 1;
        var current = allNames.Any(x => x.SequenceEqual(defaultValue)) ? TemplatedName() : defaultValue;

        foreach (var existingName in allNames.Order(StringComparer.OrdinalIgnoreCase))
        {
            if (existingName.SequenceEqual(current)) current = TemplatedName();
        }

        return current;
        string TemplatedName() => string.Format(template, ++count);
    }

    /// <summary>
    /// Creates a new collection group in the loadout.
    /// </summary>
    public static async ValueTask<CollectionGroup.ReadOnly> CreateNewCollectionGroup(IConnection connection, LoadoutId loadoutId)
    {
        var names = (await Loadout.Load(connection.Db, loadoutId).MutableCollections()).Select(x => x.AsLoadoutItemGroup().AsLoadoutItem().Name).ToArray();
        var newName = GenerateNewCollectionName(names);

        using var tx = connection.BeginTransaction();

        var group = new CollectionGroup.New(tx, out var id)
        {
            IsReadOnly = false,
            LoadoutItemGroup = new LoadoutItemGroup.New(tx, id)
            {
                IsGroup = true,
                LoadoutItem = new LoadoutItem.New(tx, id)
                {
                    Name = newName,
                    LoadoutId = loadoutId,
                },
            },
        };

        var result = await tx.Commit();
        return result.Remap(group);
    }

    /// <summary>
    /// Creates a collection JSON manifest from a loadout item group.
    /// </summary>
    public static CollectionRoot LoadoutItemGroupToCollectionManifest(
        LoadoutItemGroup.ReadOnly group,
        IGameDomainToGameIdMappingCache mappingCache,
        User.ReadOnly author)
    {
        var gameId = group.AsLoadoutItem().Loadout.Installation.GameId;
        var gameDomain = mappingCache.TryGetDomain(gameId, CancellationToken.None).Value;

        var collectionMods = new List<CollectionMod>();

        foreach (var libraryLinkedLoadoutItem in group.Children.OfTypeLoadoutItemGroup().OfTypeLibraryLinkedLoadoutItem())
        {
            var libraryItem = libraryLinkedLoadoutItem.LibraryItem;
            var libraryFile = Optional<LibraryFile.ReadOnly>.None;
            if (libraryItem.TryGetAsDownloadedFile(out var downloadedFile)) libraryFile = downloadedFile.AsLibraryFile();

            CollectionMod collectionMod;
            if (libraryItem.TryGetAsNexusModsLibraryItem(out var nexusModsLibraryItem))
            {
                collectionMod = ToCollectionMod(nexusModsLibraryItem, libraryFile);
            } else if (libraryItem.TryGetAsDownloadedFile(out downloadedFile))
            {
                collectionMod = ToCollectionMod(gameDomain, downloadedFile);
            }
            else
            {
                continue;
            }

            collectionMods.Add(collectionMod);
        }

        var collectionManifest = new CollectionRoot
        {
            Mods = collectionMods.ToArray(),
            Info = new CollectionInfo
            {
                Name = group.AsLoadoutItem().Name,
                DomainName = gameDomain,
                Author = author.Name,
                Description = string.Empty,
            },
        };

        return collectionManifest;
    }

    private static CollectionMod ToCollectionMod(
        GameDomain gameDomain,
        DownloadedFile.ReadOnly downloadedFile)
    {
        var libraryFile = downloadedFile.AsLibraryFile();

        return new CollectionMod
        {
            Name = downloadedFile.AsLibraryFile().FileName,
            DomainName = gameDomain,
            Source = new ModSource
            {
                Type = ModSourceType.Browse,
                Md5 = libraryFile.Md5.Value,
                FileSize = libraryFile.Size,
                Url = downloadedFile.DownloadPageUri,
            },
        };
    }

    private static CollectionMod ToCollectionMod(
        NexusModsLibraryItem.ReadOnly nexusModsLibraryItem,
        Optional<LibraryFile.ReadOnly> libraryFile)
    {
        var nexusModsFile = nexusModsLibraryItem.FileMetadata;
        var nexusModsModPage = nexusModsFile.ModPage;

        return new CollectionMod
        {
            Name = nexusModsFile.Name,
            Version = nexusModsFile.Version,
            DomainName = nexusModsModPage.GameDomain,
            Source = new ModSource
            {
                Type = ModSourceType.NexusMods,
                ModId = nexusModsModPage.Uid.ModId,
                FileId = nexusModsFile.Uid.FileId,
                UpdatePolicy = UpdatePolicy.ExactVersionOnly,
                FileSize = libraryFile.Convert(x => x.Size).ValueOr(() => Size.Zero),
                Md5 = libraryFile.Convert(x => x.Md5).ValueOr(() => Md5Value.From(0)),
            },
        };
    }
}
