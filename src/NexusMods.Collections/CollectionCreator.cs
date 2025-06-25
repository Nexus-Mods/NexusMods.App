using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.IO;
using CollectionMod = NexusMods.Abstractions.Collections.Json.Mod;
using ModSource = NexusMods.Abstractions.Collections.Json.ModSource;
using Size = NexusMods.Paths.Size;
using UpdatePolicy = NexusMods.Abstractions.Collections.Json.UpdatePolicy;

namespace NexusMods.Collections;

public static class CollectionCreator
{
    // TODO: remove for GA
    public static bool IsFeatureEnabled => ApplicationConstants.IsDebug;
    
    public static bool IsCollectionUploaded(IConnection connection, LoadoutItemGroupId groupId)
    {
        var group = CollectionGroup.Load(connection.Db, groupId);
        
        return group.IsValid() && group.TryGetAsManagedCollectionLoadoutGroup(out var managedCollectionLoadoutGroup);
    }
    
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

    public static async ValueTask<CollectionMetadata.ReadOnly> UploadDraftRevision(
        IServiceProvider serviceProvider,
        LoadoutItemGroupId groupId,
        CancellationToken cancellationToken)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        var jsonSerializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
        var mappingCache = serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();

        var userInfo = loginManager.UserInfo;
        if (userInfo is null) throw new NotSupportedException("User has to be logged in!");

        var users = User.FindByNexusId(connection.Db, userInfo.UserId.Value);
        if (!users.TryGetFirst(out var user)) throw new NotSupportedException("User has to be logged in!");

        var group = CollectionGroup.Load(connection.Db, groupId);
        Debug.Assert(group.IsValid());

        var collectionManifest = LoadoutItemGroupToCollectionManifest(
            group: group.AsLoadoutItemGroup(),
            mappingCache: mappingCache,
            author: user
        );

        await using var archiveFile = temporaryFileManager.CreateFile(ext: Extension.FromPath(".zip"));
        var streamFactory = await CreateArchive(archiveFile, jsonSerializerOptions, collectionManifest, cancellationToken);

        using var tx = connection.BeginTransaction();

        CollectionMetadata.ReadOnly collection;
        if (group.TryGetAsManagedCollectionLoadoutGroup(out var managedCollectionLoadoutGroup))
        {
            collection = managedCollectionLoadoutGroup.Collection;
            await nexusModsLibrary.UploadDraftRevision(collection, streamFactory, collectionManifest, cancellationToken);
        }
        else
        {
            collection = await nexusModsLibrary.CreateCollection(streamFactory, collectionManifest, cancellationToken);
            tx.Add(groupId, ManagedCollectionLoadoutGroup.Collection, collection);
        }

        await tx.Commit();
        return collection;
    }

    private static async ValueTask<IStreamFactory> CreateArchive(
        AbsolutePath archiveFilePath,
        JsonSerializerOptions jsonSerializerOptions,
        CollectionRoot collectionManifest,
        CancellationToken cancellationToken)
    {
        await using (var archiveStream = archiveFilePath.Open(mode: FileMode.Create, access: FileAccess.ReadWrite, share: FileShare.Read))
        using (var zipArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: Encoding.UTF8))
        {
            var entry = zipArchive.CreateEntry("collection.json");
            await using var entryStream = entry.Open();
            await JsonSerializer.SerializeAsync(entryStream, collectionManifest, jsonSerializerOptions, cancellationToken: cancellationToken);
        }

        return new NativeFileStreamFactory(archiveFilePath);
    }

    /// <summary>
    /// Creates a collection JSON manifest from a loadout item group.
    /// </summary>
    public static CollectionRoot LoadoutItemGroupToCollectionManifest(
        LoadoutItemGroup.ReadOnly group,
        IGameDomainToGameIdMappingCache mappingCache,
        User.ReadOnly author)
    {
        Debug.Assert(group.IsValid());

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
