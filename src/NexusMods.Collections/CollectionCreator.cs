using DynamicData.Kernel;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using CollectionMod = NexusMods.Abstractions.Collections.Json.Mod;

namespace NexusMods.Collections;

public static class CollectionCreator
{
    public static CollectionRoot CreateCollectionManifest(
        ITransaction tx,
        LoadoutItemGroup.ReadOnly group,
        IGameDomainToGameIdMappingCache mappingCache,
        User.ReadOnly creator,
        CollectionSlug collectionSlug)
    {
        var gameId = group.AsLoadoutItem().Loadout.Installation.GameId;
        var gameDomain = mappingCache.TryGetDomain(gameId, CancellationToken.None).Value;

        var collectionMetadata = new CollectionMetadata.New(tx)
        {
            Name = group.AsLoadoutItem().Name,
            GameId = gameId,
            Slug = collectionSlug,
            AuthorId = creator.UserId,
        };

        var collectionRevisionMetadata = new CollectionRevisionMetadata.New(tx)
        {
            CollectionId = collectionMetadata,
            RevisionId = RevisionId.From((ulong)Random.Shared.NextInt64(minValue: 0, maxValue: long.MaxValue)),
            RevisionNumber = RevisionNumber.From(1),
        };

        var collectionMods = new List<CollectionMod>();

        foreach (var libraryLinkedLoadoutItem in group.Children.OfTypeLoadoutItemGroup().OfTypeLibraryLinkedLoadoutItem())
        {
            var libraryItem = libraryLinkedLoadoutItem.LibraryItem;
            var libraryFile = Optional<LibraryFile.ReadOnly>.None;
            if (libraryItem.TryGetAsDownloadedFile(out var downloadedFile)) libraryFile = downloadedFile.AsLibraryFile();

            CollectionMod collectionMod;
            if (libraryItem.TryGetAsNexusModsLibraryItem(out var nexusModsLibraryItem))
            {
                collectionMod = ToCollectionMod(tx, collectionRevisionMetadata, nexusModsLibraryItem, libraryFile, index: collectionMods.Count);
            } else if (libraryItem.TryGetAsDownloadedFile(out downloadedFile))
            {
                collectionMod = ToCollectionMod(tx, collectionRevisionMetadata, gameDomain, downloadedFile, index: collectionMods.Count);
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
                Name = collectionMetadata.Name,
                DomainName = gameDomain,
                Author = creator.Name,
                Description = collectionMetadata.Summary ?? string.Empty,
            },
        };

        return collectionManifest;
    }

    private static CollectionMod ToCollectionMod(
        ITransaction tx,
        CollectionRevisionMetadata.New collectionRevisionMetadata,
        GameDomain gameDomain,
        DownloadedFile.ReadOnly downloadedFile,
        int index)
    {
        var libraryFile = downloadedFile.AsLibraryFile();

        _ = new CollectionDownloadExternal.New(tx, out var id)
        {
            Uri = downloadedFile.DownloadPageUri,
            Size = libraryFile.Size,
            Md5 = libraryFile.Md5.Value,
            CollectionDownload = new CollectionDownload.New(tx, id)
            {
                Name = libraryFile.FileName,
                CollectionRevisionId = collectionRevisionMetadata,
                IsOptional = false,
                ArrayIndex = index,
            },
        };

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
        ITransaction tx,
        CollectionRevisionMetadata.New collectionRevisionMetadata,
        NexusModsLibraryItem.ReadOnly nexusModsLibraryItem,
        Optional<LibraryFile.ReadOnly> libraryFile,
        int index)
    {
        var nexusModsFile = nexusModsLibraryItem.FileMetadata;
        var nexusModsModPage = nexusModsFile.ModPage;

        _ = new CollectionDownloadNexusMods.New(tx, out var id)
        {
            FileMetadataId = nexusModsFile,
            FileUid = nexusModsFile.Uid,
            ModUid = nexusModsModPage.Uid,
            CollectionDownload = new CollectionDownload.New(tx, id)
            {
                Name = nexusModsFile.Name,
                CollectionRevisionId = collectionRevisionMetadata,
                IsOptional = false,
                ArrayIndex = index,
            },
        };

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
                Md5 = libraryFile.Convert(x => x.Md5).ValueOr(() => Md5HashValue.From(0)),
            },
        };
    }
}
