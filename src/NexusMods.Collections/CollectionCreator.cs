using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Errors;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.IO;
using NexusMods.Telemetry;
using CollectionMod = NexusMods.Abstractions.Collections.Json.Mod;
using ModSource = NexusMods.Abstractions.Collections.Json.ModSource;
using Size = NexusMods.Paths.Size;
using UpdatePolicy = NexusMods.Abstractions.Collections.Json.UpdatePolicy;

namespace NexusMods.Collections;

public static class CollectionCreator
{
    public static bool IsCollectionUploaded(IConnection connection, CollectionGroupId groupId, out CollectionMetadata.ReadOnly collection)
    {
        var group = ManagedCollectionLoadoutGroup.Load(connection.Db, groupId);
        if (!group.IsValid())
        {
            collection = default(CollectionMetadata.ReadOnly);
            return false;
        }

        collection = group.Collection;
        return true;
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

    public static async Task<EntityId> MakeEditableLocalCollection(
        ILoadoutManager loadoutManager,
        IConnection conn,
        CollectionGroupId collectionGroupId,
        string newName)
    {
        var cloneId = await loadoutManager.CloneCollection(collectionGroupId);
        var cloneEnt = NexusCollectionLoadoutGroup.Load(conn.Db, cloneId);

        using var tx = conn.BeginTransaction();
        // Remap the name
        tx.Add(cloneId, LoadoutItem.Name, newName);
        // Make it editable
        tx.Add(cloneId, CollectionGroup.IsReadOnly, false);
        // Retract the Nexus references as this is no longer associated with the official collection
        tx.Retract(cloneId, NexusCollectionLoadoutGroup.RevisionId, NexusCollectionLoadoutGroup.RevisionId.Get(cloneEnt));
        tx.Retract(cloneId, NexusCollectionLoadoutGroup.CollectionId, NexusCollectionLoadoutGroup.CollectionId.Get(cloneEnt));
        tx.Retract(cloneId, NexusCollectionLoadoutGroup.LibraryFileId, NexusCollectionLoadoutGroup.LibraryFileId.Get(cloneEnt));

        // Retract the Nexus references in items so that this is no longer associated with the official collection
        var db = conn.Db;
        foreach (var item in conn.Query<EntityId>($"SELECT Id FROM mdb_NexusCollectionItemLoadoutGroup(Db=>{db}) WHERE Parent = {cloneId.Id}"))
        {
            var ent = NexusCollectionItemLoadoutGroup.Load(db, item);
            tx.Retract(item, NexusCollectionItemLoadoutGroup.Download, NexusCollectionItemLoadoutGroup.Download.Get(ent));
            tx.Retract(item, NexusCollectionItemLoadoutGroup.IsRequired, NexusCollectionItemLoadoutGroup.IsRequired.Get(ent));

            if (NexusCollectionReplicatedLoadoutGroup.Replicated.TryGetValue(ent, out var replicated))
                tx.Retract(item, NexusCollectionReplicatedLoadoutGroup.Replicated, replicated);

            if (NexusCollectionBundledLoadoutGroup.CollectionLibraryFileId.TryGetValue(ent, out var bundleLibraryFileId))
                tx.Retract(item, NexusCollectionBundledLoadoutGroup.CollectionLibraryFileId, bundleLibraryFileId);

            if (NexusCollectionBundledLoadoutGroup.BundleDownload.TryGetValue(ent, out var bundleDownload))
            {
                tx.Retract(item, NexusCollectionBundledLoadoutGroup.BundleDownload, bundleDownload);

                // We've now orphaned the bundled files, so we'll now create a download archive that contains the files this loadout group needs.

                var fileName = "Bundled Files - " + ent.AsLoadoutItemGroup().AsLoadoutItem().Name;

                // Create a new library item, 
                var libraryFile = new ManuallyCreatedArchive.New(tx, out var libraryFileId)
                {
                    Source = ManuallyCreatedArchive.CreationSource.CollectionBundled,
                    LibraryArchive = new LibraryArchive.New(tx, libraryFileId)
                    {
                        IsArchive = true,
                        LibraryFile = new LibraryFile.New(tx, libraryFileId)
                        {
                            Hash = Hash.Zero,
                            // We'll update the size later with the total size of all the files
                            Size = Size.Zero,
                            FileName = fileName,
                            LibraryItem = new LibraryItem.New(tx, libraryFileId)
                            {
                                Name = fileName,
                            },
                        },

                    },
                };

                // Link the mod group to the archive
                tx.Add(item, LibraryLinkedLoadoutItem.LibraryItemId, libraryFileId);

                var added = new HashSet<Hash>();

                var sum = Size.Zero;
                // Now link up all the required items
                foreach (var child in ent.AsLoadoutItemGroup().Children.OfTypeLoadoutItemWithTargetPath().OfTypeLoadoutFile())
                {
                    // Don't add duplicates
                    if (added.Contains(child.Hash))
                        continue;

                    var name = child.AsLoadoutItemWithTargetPath().AsLoadoutItem().Name;
                    _ = new LibraryArchiveFileEntry.New(tx, out var fileId)
                    {
                        ParentId = libraryFileId,
                        Path = child.AsLoadoutItemWithTargetPath().TargetPath.Item3,
                        LibraryFile = new LibraryFile.New(tx, fileId)
                        {
                            Hash = child.Hash,
                            Size = child.Size,
                            FileName = name,
                            LibraryItem = new LibraryItem.New(tx, fileId)
                            {
                                Name = name,
                            },
                        },
                    };
                    sum += child.Size;
                    added.Add(child.Hash);
                }

                tx.Add(libraryFileId, NexusMods.Abstractions.Library.Models.LibraryFile.Size, sum);
            }

        }

        await tx.Commit();
        return cloneId;
    }

    /// <summary>
    /// Creates a new collection group in the loadout.
    /// </summary>
    public static async ValueTask<CollectionGroup.ReadOnly> CreateNewCollectionGroup(IConnection connection, LoadoutId loadoutId, string newName)
    {
        var loadout = Loadout.Load(connection.Db, loadoutId);

        if (string.IsNullOrWhiteSpace(newName))
        {
            var names = loadout.MutableCollections().Select(x => x.Name).ToArray();
            newName = GenerateNewCollectionName(names);
        }

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

        Tracking.AddEvent(Events.Collections.CreateLocalCollection, new EventMetadata(name: $"{loadout.LocatableGame.DisplayName} - {newName}"));
        return result.Remap(group);
    }

    private static async ValueTask<(IStreamFactory, TemporaryPath, CollectionRoot)> PrepareForUpload(
        IServiceProvider serviceProvider,
        CollectionGroupId groupId,
        CancellationToken cancellationToken)
    {
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var loginManager = serviceProvider.GetRequiredService<ILoginManager>();
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var temporaryFileManager = serviceProvider.GetRequiredService<TemporaryFileManager>();
        var jsonSerializerOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
        var mappingCache = serviceProvider.GetRequiredService<IGameDomainToGameIdMappingCache>();
        var fileHashesService = serviceProvider.GetRequiredService<IFileHashesService>();

        var userInfo = loginManager.UserInfo;
        if (userInfo is null) throw new NotSupportedException("User has to be logged in!");

        var users = User.FindByNexusId(connection.Db, userInfo.UserId.Value);
        if (!users.TryGetFirst(out var user)) throw new NotSupportedException("User has to be logged in!");

        var group = CollectionGroup.Load(connection.Db, groupId);
        Debug.Assert(group.IsValid());

        await fileHashesService.GetFileHashesDb();
        var installation = group.AsLoadoutItemGroup().AsLoadoutItem().Loadout.InstallationInstance;
        var locatorIds = installation.LocatorResultMetadata?.ToLocatorIds().ToArray() ?? [];
        var vanityVersion = fileHashesService.TryGetVanityVersion((installation.Store, locatorIds), out var tmpVanityVersion) ? tmpVanityVersion : VanityVersion.From("Unknown");

        var collectionManifest = LoadoutItemGroupToCollectionManifest(
            group: group.AsLoadoutItemGroup(),
            mappingCache: mappingCache,
            author: user,
            vanityVersion
        );

        var archiveFile = temporaryFileManager.CreateFile(ext: Extension.FromPath(".zip"));
        var streamFactory = await CreateArchive(archiveFile, jsonSerializerOptions, collectionManifest, cancellationToken);

        return (streamFactory, archiveFile, collectionManifest);
    }

    public static async ValueTask<GraphQlResult<Abstractions.NexusModsLibrary.Models.CollectionStatus, NotFound, CollectionDiscarded>> ChangeCollectionStatus(
        IServiceProvider serviceProvider,
        ManagedCollectionLoadoutGroupId groupId,
        Abstractions.NexusModsLibrary.Models.CollectionStatus newStatus,
        CancellationToken cancellationToken)
    {
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var managedGroup = ManagedCollectionLoadoutGroup.Load(connection.Db, groupId);
        var collection = managedGroup.Collection;

        if (collection.Status == newStatus) return newStatus;

        var result = await nexusModsLibrary.ChangeCollectionStatus(
            collection.CollectionId,
            newStatus: newStatus,
            cancellationToken: cancellationToken
        );

        if (result.HasErrors) return newStatus;
        using var tx = connection.BeginTransaction();

        tx.Add(collection.Id, CollectionMetadata.Status, newStatus);

        await tx.Commit();
        return newStatus;
    }

    /// <summary>
    /// Creates a new collection.
    /// </summary>
    public static async ValueTask<CollectionMetadata.ReadOnly> CreateCollection(
        IServiceProvider serviceProvider,
        CollectionGroupId groupId,
        Abstractions.NexusModsLibrary.Models.CollectionStatus initialCollectionStatus = default,
        CancellationToken cancellationToken = default)
    {
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var (streamFactory, archiveFile, collectionManifest) = await PrepareForUpload(serviceProvider, groupId, cancellationToken);
        await using var _ = archiveFile;

        var group = CollectionGroup.Load(connection.Db, groupId);
        using var tx = connection.BeginTransaction();

        var (collection, revisionId) = await nexusModsLibrary.CreateCollection(streamFactory, collectionManifest, cancellationToken);
        var uploadTime = DateTimeOffset.UtcNow;

        var defaultImageStreamFactory = new EmbeddedResourceStreamFactory<CollectionDownloader>("NexusMods.Collections.default-collection-image.webp");
        var defaultImageMimeType = "image/webp";

        var prefillResult = await nexusModsLibrary.PrefillCollectionMetadata(collection, defaultImageStreamFactory, defaultImageMimeType, cancellationToken);

        // TODO: handle result
        prefillResult.AssertHasData();

        var result = await nexusModsLibrary.PublishRevision(revisionId, cancellationToken);

        // TODO: handle result
        result.AssertHasData();

        tx.Add(groupId, ManagedCollectionLoadoutGroup.Collection, collection);
        tx.Add(groupId, ManagedCollectionLoadoutGroup.CurrentRevisionNumber, RevisionNumber.From(2));
        tx.Add(groupId, ManagedCollectionLoadoutGroup.LastUploadDate, uploadTime);
        tx.Add(groupId, ManagedCollectionLoadoutGroup.LastPublishedRevisionNumber, RevisionNumber.From(1));

        await tx.Commit();

        if (initialCollectionStatus is not Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted)
        {
            await ChangeCollectionStatus(serviceProvider, groupId.Value, newStatus: initialCollectionStatus, cancellationToken);
        }

        Tracking.AddEvent(Events.Collections.ShareCollection, EventMetadata.Create(name: $"{collection.Slug}", value: collectionManifest.Mods.Length));
        return CollectionMetadata.Load(connection.Db, collection.Id);
    }

    public static async ValueTask<CollectionMetadata.ReadOnly> UploadAndPublishRevision(
        IServiceProvider serviceProvider,
        ManagedCollectionLoadoutGroupId groupId,
        CancellationToken cancellationToken)
    {
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var connection = serviceProvider.GetRequiredService<IConnection>();
        var client = serviceProvider.GetRequiredService<IGraphQlClient>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(CollectionCreator));

        var managedGroup = ManagedCollectionLoadoutGroup.Load(connection.Db, groupId);

        var (collection, revisionId, revisionNumber) = await UploadDraftRevision(serviceProvider, groupId, cancellationToken);

        var previousRevisionResult = await client.QueryCollectionRevision(collection.Slug, RevisionNumber.From(revisionNumber.Value - 1), cancellationToken: cancellationToken);
        var currentRevisionResult = await client.QueryCollectionRevision(collection.Slug, revisionNumber, cancellationToken);

        if (currentRevisionResult.HasData)
        {
            var changelog = NexusModsLibrary.GenerateChangelog(currentRevisionResult.AssertHasData(), previousRevisionResult.HasData ? Optional<ICollectionRevision>.Create(previousRevisionResult.AssertHasData()) : Optional<ICollectionRevision>.None);
            if (changelog is not null)
            {
                var changelogResult = await client.CreateChangelog(revisionId, changelog: changelog, cancellationToken: cancellationToken);

                if (changelogResult.TryGetError(out Invalid? invalid))
                {
                    logger.LogWarning("Failed to create changelog for `{Slug}/{RevisionNumber}` because of invalid input: {Message}", collection.Slug, revisionNumber, invalid.Message);
                }
                else
                {
                    _ = changelogResult.AssertHasData();
                }
            }
        }

        var result = await nexusModsLibrary.PublishRevision(revisionId, cancellationToken);

        // TODO: handle result
        _ = result.AssertHasData();

        using var tx = connection.BeginTransaction();
        tx.Add(groupId, ManagedCollectionLoadoutGroup.LastPublishedRevisionNumber, managedGroup.CurrentRevisionNumber);
        tx.Add(groupId, ManagedCollectionLoadoutGroup.CurrentRevisionNumber, RevisionNumber.From(managedGroup.CurrentRevisionNumber.Value + 1));
        tx.Add(groupId, ManagedCollectionLoadoutGroup.LastUploadDate, DateTimeOffset.UtcNow);
        if (managedGroup.CurrentRevisionId.HasValue)
            tx.Retract(groupId, ManagedCollectionLoadoutGroup.CurrentRevisionId, managedGroup.CurrentRevisionId.Value);

        await tx.Commit();
        return collection;
    }

    public static async ValueTask<(CollectionMetadata.ReadOnly, RevisionId, RevisionNumber)> UploadDraftRevision(
        IServiceProvider serviceProvider,
        ManagedCollectionLoadoutGroupId groupId,
        CancellationToken cancellationToken)
    {
        var nexusModsLibrary = serviceProvider.GetRequiredService<NexusModsLibrary>();
        var connection = serviceProvider.GetRequiredService<IConnection>();

        var (streamFactory, archiveFile, collectionManifest) = await PrepareForUpload(serviceProvider, groupId.Value, cancellationToken);
        await using var _ = archiveFile;

        var managedCollectionLoadoutGroup = ManagedCollectionLoadoutGroup.Load(connection.Db, groupId);
        var collection = managedCollectionLoadoutGroup.Collection;
        var (revisionNumber, revisionId) = await nexusModsLibrary.UploadDraftRevision(collection, streamFactory, collectionManifest, cancellationToken);

        using var tx = connection.BeginTransaction();
        tx.Add(groupId, ManagedCollectionLoadoutGroup.CurrentRevisionNumber, revisionNumber);
        tx.Add(groupId, ManagedCollectionLoadoutGroup.CurrentRevisionId, revisionId);
        tx.Add(groupId, ManagedCollectionLoadoutGroup.LastUploadDate, DateTimeOffset.UtcNow);

        await tx.Commit();

        Tracking.AddEvent(Events.Collections.UploadRevision, EventMetadata.Create(name: $"{collection.Slug}", value: collectionManifest.Mods.Length));
        return (collection, revisionId, revisionNumber);
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
        User.ReadOnly author,
        VanityVersion vanityVersion)
    {
        Debug.Assert(group.IsValid());

        var gameId = group.AsLoadoutItem().Loadout.Installation.GameId;
        var gameDomain = mappingCache[gameId];

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
            }
            else if (libraryItem.TryGetAsDownloadedFile(out downloadedFile))
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
                GameVersions = [vanityVersion.Value],
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
