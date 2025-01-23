using System.Diagnostics;
using System.Text.Json;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.NexusWebApi.Extensions;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;
using GameIdCache = Dictionary<GameDomain, GameId>;
using ResolvedEntitiesLookup = Dictionary<UidForFile, ValueTuple<NexusModsModPageMetadataId, NexusModsFileMetadataId>>;

public partial class NexusModsLibrary
{
    /// <summary>
    /// Gets or adds the provided collection revision.
    /// </summary>
    public async Task<CollectionRevisionMetadata.ReadOnly> GetOrAddCollectionRevision(
        NexusModsCollectionLibraryFile.ReadOnly collectionLibraryFile,
        CollectionSlug slug,
        RevisionNumber revisionNumber,
        CancellationToken cancellationToken)
    {
        var revisions = CollectionRevisionMetadata
            .FindByRevisionNumber(_connection.Db, revisionNumber)
            .Where(r => r.Collection.Slug == slug);

        if (revisions.TryGetFirst(out var revision)) return revision;

        var collectionRoot = await ParseCollectionJsonFile(collectionLibraryFile, cancellationToken);
        var gameIds = await CacheGameIds(collectionRoot, cancellationToken);

        var apiResult = await _gqlClient.CollectionRevisionInfo.ExecuteAsync(
            slug: slug.Value,
            revisionNumber: (int)revisionNumber.Value,
            viewAdultContent: true,
            cancellationToken: cancellationToken
        );

        var collectionRevisionInfo = apiResult.Data?.CollectionRevision;
        if (collectionRevisionInfo is null) throw new NotSupportedException($"API call returned no data for collection slug `{slug}` revision `{revisionNumber}`");

        using var tx = _connection.BeginTransaction();
        var db = _connection.Db;

        var collectionEntityId = UpdateCollectionInfo(db, tx, slug, collectionRevisionInfo.Collection);
        var collectionRevisionEntityId = UpdateRevisionInfo(db, tx, revisionNumber, collectionEntityId, collectionRevisionInfo);

        var resolvedEntitiesLookup = ResolveModFiles(db, tx, collectionRoot, gameIds, collectionRevisionInfo);
        UpdateFiles(db, tx, collectionRevisionEntityId, collectionRevisionInfo, collectionRoot, gameIds, resolvedEntitiesLookup);

        var results = await tx.Commit();
        return CollectionRevisionMetadata.Load(results.Db, results[collectionRevisionEntityId]);
    }

    /// <summary>
    /// Gets all revision numbers that are newer than the current revision.
    /// </summary>
    public async ValueTask<RevisionNumber[]> GetNewerRevisionNumbers(
        CollectionRevisionMetadata.ReadOnly currentRevision,
        CancellationToken cancellationToken)
    {
        var revisionNumbers = await GetAllRevisionNumbers(currentRevision.Collection, cancellationToken);

        var currentValue = currentRevision.RevisionNumber.Value;
        var newer = revisionNumbers
            .TakeWhile(other => other.Value > currentValue)
            .ToArray();

        return newer;
    }

    /// <summary>
    /// Gets all revision numbers for the given collection in descending order.
    /// </summary>
    public async ValueTask<RevisionNumber[]> GetAllRevisionNumbers(
        CollectionMetadata.ReadOnly collection,
        CancellationToken cancellationToken)
    {
        var gameDomain = await _mappingCache.TryGetDomainAsync(collection.GameId, cancellationToken);

        var apiResult = await _gqlClient.CollectionRevisionNumbers.ExecuteAsync(
            slug: collection.Slug.Value,
            domainName: gameDomain.Value.Value,
            viewAdultContent: true,
            cancellationToken: cancellationToken
        );

        var revisions = apiResult.Data?.Collection.Revisions;
        if (revisions is null) throw new NotSupportedException($"API call returned no data for collection slug `{collection.Slug}`");

        var revisionNumbers = revisions
            .Select(static x => x.RevisionNumber)
            .OrderDescending()
            .Select(static number => RevisionNumber.From((ulong)number))
            .ToArray();

        return revisionNumbers;
    }

    private static ResolvedEntitiesLookup ResolveModFiles(
        IDb db,
        ITransaction tx,
        CollectionRoot collectionRoot,
        GameIdCache gameIds,
        ICollectionRevisionInfo_CollectionRevision collectionRevision)
    {
        var res = new ResolvedEntitiesLookup();

        foreach (var modFile in collectionRevision.ModFiles)
        {
            var file = modFile.File;
            if (file is null) continue;

            var modEntityId = file.Mod.Resolve(db, tx);
            var fileEntityId = file.Resolve(db, tx, modEntityId);

            var id = new UidForFile(
                fileId: FileId.From((uint)modFile.FileId),
                gameId: GameId.From((uint)modFile.GameId)
            );

            res[id] = (modEntityId, fileEntityId);
        }

        foreach (var collectionMod in collectionRoot.Mods)
        {
            if (collectionMod.Source.Type != ModSourceType.NexusMods) continue;
            var fileId = new UidForFile(fileId: collectionMod.Source.FileId, gameId: gameIds[collectionMod.DomainName]);
            if (res.ContainsKey(fileId)) continue;

            // TODO: use normal API to query information about this file
            throw new NotImplementedException();
        }

        return res;
    }

    private async ValueTask<GameIdCache> CacheGameIds(
        CollectionRoot collectionRoot,
        CancellationToken cancellationToken)
    {
        var gameIds = new GameIdCache();

        foreach (var collectionMod in collectionRoot.Mods)
        {
            var gameDomain = collectionMod.DomainName;
            if (gameIds.ContainsKey(gameDomain)) continue;

            var gameId = await _mappingCache.TryGetIdAsync(gameDomain, cancellationToken);
            if (!gameId.HasValue) throw new NotSupportedException($"Unable to resolve game id for domain `{gameDomain}`");

            gameIds[gameDomain] = gameId.Value;
        }

        return gameIds;
    }

    private static void UpdateFiles(
        IDb db,
        ITransaction tx,
        CollectionRevisionMetadataId collectionRevisionEntityId,
        ICollectionRevisionInfo_CollectionRevision revisionInfo,
        CollectionRoot collectionRoot,
        GameIdCache gameIds,
        ResolvedEntitiesLookup resolvedEntitiesLookup)
    {
        for (var i = 0; i < collectionRoot.Mods.Length; i++)
        {
            var collectionMod = collectionRoot.Mods[i];

            var downloadEntity = new CollectionDownload.New(tx)
            {
                ArrayIndex = i,
                CollectionRevisionId = collectionRevisionEntityId,
                IsOptional = collectionMod.Optional,
                Name = collectionMod.Name,
            };

            var source = collectionMod.Source;
            switch (source.Type)
            {
                case ModSourceType.NexusMods:
                    HandleNexusModsDownload(db, tx, downloadEntity, collectionMod, gameIds, resolvedEntitiesLookup);
                    break;
                case ModSourceType.Direct or ModSourceType.Browse:
                    HandleExternalDownload(tx, downloadEntity, collectionMod);
                    break;
                case ModSourceType.Bundle:
                    HandleBundledFiles(tx, downloadEntity, collectionMod);
                    break;
                default:
                    throw new NotSupportedException($"The mod source type `{source.Type}` is not supported");
            }
        }
    }

    private static void HandleNexusModsDownload(
        IDb db,
        ITransaction tx,
        CollectionDownload.New downloadEntity,
        Mod collectionMod,
        GameIdCache gameIds,
        ResolvedEntitiesLookup resolvedEntitiesLookup)
    {
        var modId = new UidForMod
        {
            GameId = gameIds[collectionMod.DomainName],
            ModId = collectionMod.Source.ModId,
        };

        var fileId = new UidForFile(fileId: collectionMod.Source.FileId, gameId: gameIds[collectionMod.DomainName]);

        Debug.Assert(resolvedEntitiesLookup.ContainsKey(fileId), message: "Should've resolved all mod files earlier");
        var (_, fileMetadataId) = resolvedEntitiesLookup[fileId];

        _ = new CollectionDownloadNexusMods.New(tx, downloadEntity.Id)
        {
            CollectionDownload = downloadEntity,
            FileUid = fileId,
            ModUid = modId,
            FileMetadataId = fileMetadataId,
        };
    }

    private static void HandleExternalDownload(
        ITransaction tx,
        CollectionDownload.New downloadEntity,
        Mod collectionMod)
    {
        var source = collectionMod.Source;
        var uri = source.Url;
        if (uri is null) throw new NotSupportedException("External mod doesn't have a url!");

        _ = new CollectionDownloadExternal.New(tx, downloadEntity.Id)
        {
            CollectionDownload = downloadEntity,
            Md5 = source.Md5,
            Size = source.FileSize,
            Uri = uri,
        };
    }

    private static void HandleBundledFiles(
        ITransaction tx,
        CollectionDownload.New downloadEntity,
        Mod collectionMod)
    {
        var source = collectionMod.Source;

        _ = new CollectionDownloadBundled.New(tx, downloadEntity.Id)
        {
            CollectionDownload = downloadEntity,
            BundledPath = source.FileExpression,
        };
    }

    private static EntityId UpdateRevisionInfo(
        IDb db,
        ITransaction tx,
        RevisionNumber revisionNumber,
        EntityId collectionEntityId,
        ICollectionRevisionInfo_CollectionRevision revisionInfo)
    {
        var revisionId = RevisionId.From((ulong)revisionInfo.Id);
        var resolver = GraphQLResolver.Create(db, tx, CollectionRevisionMetadata.RevisionId, revisionId);

        resolver.Add(CollectionRevisionMetadata.RevisionId, revisionId);
        resolver.Add(CollectionRevisionMetadata.RevisionNumber, revisionNumber);
        resolver.Add(CollectionRevisionMetadata.CollectionId, collectionEntityId);
        resolver.Add(CollectionRevisionMetadata.IsAdult, revisionInfo.AdultContent);

        if (ulong.TryParse(revisionInfo.TotalSize, out var totalSize))
            resolver.Add(CollectionRevisionMetadata.TotalSize, Size.From(totalSize));
        else
            resolver.Add(CollectionRevisionMetadata.TotalSize, Size.Zero);

        if (float.TryParse(revisionInfo.OverallRating ?? "0.0", out var overallRating))
            resolver.Add(CollectionRevisionMetadata.OverallRating, overallRating / 100);
        if (revisionInfo.OverallRatingCount is not null)
            resolver.Add(CollectionRevisionMetadata.TotalRatings, (ulong)revisionInfo.OverallRatingCount.Value);

        return resolver.Id;
    }

    private static EntityId UpdateCollectionInfo(
        IDb db,
        ITransaction tx,
        CollectionSlug slug,
        ICollectionRevisionInfo_CollectionRevision_Collection collectionInfo)
    {
        var resolver = GraphQLResolver.Create(db, tx, CollectionMetadata.Slug, slug);

        resolver.Add(CollectionMetadata.Name, collectionInfo.Name);
        resolver.Add(CollectionMetadata.GameId, GameId.From((uint)collectionInfo.Game.Id));
        resolver.Add(CollectionMetadata.Summary, collectionInfo.Summary);
        resolver.Add(CollectionMetadata.Endorsements, (ulong)collectionInfo.Endorsements);
        resolver.Add(CollectionMetadata.TotalDownloads, (ulong)collectionInfo.TotalDownloads);

        if (Uri.TryCreate(collectionInfo.TileImage?.ThumbnailUrl, UriKind.Absolute, out var tileImageUri))
            resolver.Add(CollectionMetadata.TileImageUri, tileImageUri);

        if (Uri.TryCreate(collectionInfo.HeaderImage?.Url, UriKind.Absolute, out var backgroundImageUri))
            resolver.Add(CollectionMetadata.BackgroundImageUri, backgroundImageUri);

        if (collectionInfo.Category is not null)
            resolver.Add(CollectionMetadata.Category, collectionInfo.Category.Resolve(db, tx));

        var user = collectionInfo.User.Resolve(db, tx);
        resolver.Add(CollectionMetadata.Author, user);

        return resolver.Id;
    }

    /// <summary>
    /// Parses the collection json file.
    /// </summary>
    public async ValueTask<CollectionRoot> ParseCollectionJsonFile(
        NexusModsCollectionLibraryFile.ReadOnly collectionLibraryFile,
        CancellationToken cancellationToken)
    {
        var jsonFileEntity = GetCollectionJsonFile(collectionLibraryFile);

        await using var data = await _fileStore.GetFileStream(jsonFileEntity.AsLibraryFile().Hash, token: cancellationToken);
        var root = await JsonSerializer.DeserializeAsync<CollectionRoot>(data, _jsonSerializerOptions, cancellationToken: cancellationToken);

        if (root is null) throw new InvalidOperationException("Unable to deserialize collection JSON file");
        return root;
    }

    /// <summary>
    /// Gets the collection JSON file.
    /// </summary>
    public LibraryArchiveFileEntry.ReadOnly GetCollectionJsonFile(NexusModsCollectionLibraryFile.ReadOnly collectionLibraryFile)
    {
        if (!collectionLibraryFile.AsLibraryFile().TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The source collection is not a library archive");

        var jsonFileEntity = archive.Children.FirstOrDefault(f => f.Path == "collection.json");
        return jsonFileEntity;
    }
}
