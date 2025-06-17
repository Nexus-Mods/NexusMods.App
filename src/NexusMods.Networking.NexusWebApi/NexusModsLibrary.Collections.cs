using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.Query.SliceDescriptors;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.NexusWebApi.Extensions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Hashes;
using StrawberryShake;
using EntityId = NexusMods.MnemonicDB.Abstractions.EntityId;

namespace NexusMods.Networking.NexusWebApi;
using GameIdCache = Dictionary<GameDomain, GameId>;
using ResolvedEntitiesLookup = Dictionary<UidForFile, ValueTuple<NexusModsModPageMetadataId, NexusModsFileMetadataId>>;
using ModAndDownload = (Mod Mod, CollectionDownload.ReadOnly Download);

public partial class NexusModsLibrary
{
    private async ValueTask<PresignedUploadUrl> UploadCollectionArchive(IStreamFactory archiveStreamFactory, CancellationToken cancellationToken)
    {
        var result = await _gqlClient.GetCollectionRevisionUploadUrl.ExecuteAsync(cancellationToken: cancellationToken);
        result.EnsureNoErrors();

        var presignedUploadUrl = PresignedUploadUrl.FromApi(result.Data?.CollectionRevisionUploadUrl!);

        await using var stream = await archiveStreamFactory.GetStreamAsync();
        using HttpContent httpContent = new StreamContent(stream);
        httpContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

        var response = await _httpClient.PutAsync(presignedUploadUrl, httpContent, cancellationToken: cancellationToken);
        if (response.IsSuccessStatusCode) return presignedUploadUrl;

        var responseMessage = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
        _logger.LogError("Failed to upload collection archive, response code={ResponseCode}, response reason=`{ResponseReason}`, response message=`{ResponseMessage}`", response.StatusCode, response.ReasonPhrase, responseMessage);

        response.EnsureSuccessStatusCode();
        return presignedUploadUrl;
    }

    /// <summary>
    /// Edits the collection name.
    /// </summary>
    public async ValueTask<CollectionMetadata.ReadOnly> EditCollectionName(CollectionMetadata.ReadOnly collection, string newName, CancellationToken cancellationToken)
    {
        var result = await _gqlClient.EditCollectionName.ExecuteAsync(
            collectionId: (int)collection.CollectionId.Value,
            name: newName,
            cancellationToken: cancellationToken
        );

        result.EnsureNoErrors();

        var data = result.Data?.EditCollection!;
        if (!data.Success) throw new NotImplementedException();

        using var tx = _connection.BeginTransaction();
        var db = _connection.Db;

        var collectionEntityId = UpdateCollectionInfo(db, tx, data.Collection);
        var commitResult = await tx.Commit();

        collection = CollectionMetadata.Load(commitResult.Db, commitResult[collectionEntityId]);
        return collection;
    }

    /// <summary>
    /// Uploads a collection revision, either creating a new revision or updating an existing draft revision.
    /// </summary>
    public async ValueTask<CollectionRevisionMetadata.ReadOnly> UploadCollectionRevision(
        CollectionMetadata.ReadOnly collection,
        IStreamFactory archiveStreamFactory,
        CollectionRoot collectionManifest,
        CancellationToken cancellationToken)
    {
        var payload = ManifestToPayload(collectionManifest);
        var presignedUploadUrl = await UploadCollectionArchive(archiveStreamFactory, cancellationToken: cancellationToken);

        var result = await _gqlClient.CreateOrUpdateRevision.ExecuteAsync(
            payload: payload,
            collectionId: (int)collection.CollectionId.Value,
            uuid: presignedUploadUrl.UUID,
            cancellationToken: cancellationToken
        );

        result.EnsureNoErrors();

        var data = result.Data?.CreateOrUpdateRevision!;
        if (!data.Success) throw new NotImplementedException();

        var revision = await AddCollectionToDatabase(collectionManifest, data.Revision.Collection, data.Revision, cancellationToken);
        return revision;
    }

    /// <summary>
    /// Uploads a new collection to Nexus Mods and adds it to the app.
    /// </summary>
    public async ValueTask<CollectionRevisionMetadata.ReadOnly> CreateCollection(
        IStreamFactory archiveStreamFactory,
        CollectionRoot collectionManifest,
        CancellationToken cancellationToken)
    {
        var payload = ManifestToPayload(collectionManifest);
        var presignedUploadUrl = await UploadCollectionArchive(archiveStreamFactory, cancellationToken: cancellationToken);

        var result = await _gqlClient.CreateCollection.ExecuteAsync(
            payload: payload,
            uuid: presignedUploadUrl.UUID,
            cancellationToken: cancellationToken
        );

        result.EnsureNoErrors();

        var data = result.Data?.CreateCollection!;
        if (!data.Success) throw new NotImplementedException();

        var revision = await AddCollectionToDatabase(collectionManifest, data.Collection, data.Revision, cancellationToken);
        return revision;
    }

    private static CollectionPayload ManifestToPayload(CollectionRoot manifest)
    {
        return new CollectionPayload
        {
            CollectionSchemaId = 1,
            AdultContent = false, // TODO
            CollectionManifest = new CollectionManifest
            {
                Info = new CollectionManifestInfo
                {
                    Author = manifest.Info.Author,
                    AuthorUrl = manifest.Info.AuthorUrl,
                    Description = manifest.Info.Description,
                    DomainName = manifest.Info.DomainName.Value,
                    GameVersions = [],
                    Name = manifest.Info.Name,
                    Summary = string.Empty, // TODO
                },
                Mods = manifest.Mods.Select(x => new CollectionManifestMod
                {
                    DomainName = x.DomainName.Value,
                    Name = x.Name,
                    Optional = x.Optional,
                    Version = x.Version,
                    Author = string.Empty, // TODO
                    Source = new CollectionManifestModSource
                    {
                        AdultContent = null, // TODO
                        Url = x.Source.Url?.ToString(),
                        FileExpression = x.Source.FileExpression.ToString(),
                        FileId = (int)x.Source.FileId.Value,
                        ModId = (int)x.Source.ModId.Value,
                        FileSize = (int)x.Source.FileSize.Value,
                        LogicalFilename = x.Source.LogicalFilename,
                        Md5 = x.Source.Md5.ToString(),
                        Type = ConvertSourceType(x.Source.Type),
                        UpdatePolicy = ConvertUpdatePolicy(x.Source.UpdatePolicy),
                    },
                }).ToArray(),
            },
        };

        static ModSource ConvertSourceType(ModSourceType sourceType)
        {
            return sourceType switch
            {
                ModSourceType.NexusMods => ModSource.Nexus,
                ModSourceType.Bundle => ModSource.Bundle,
                ModSourceType.Browse => ModSource.Browse,
                ModSourceType.Direct => ModSource.Direct,
                _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, null)
            };
        }

        static UpdatePolicy ConvertUpdatePolicy(Abstractions.Collections.Json.UpdatePolicy sourceUpdatePolicy)
        {
            return sourceUpdatePolicy switch
            {
                Abstractions.Collections.Json.UpdatePolicy.ExactVersionOnly => UpdatePolicy.Exact,
                Abstractions.Collections.Json.UpdatePolicy.PreferExact => UpdatePolicy.Prefer,
                Abstractions.Collections.Json.UpdatePolicy.LatestVersion => UpdatePolicy.Latest,
                _ => throw new ArgumentOutOfRangeException(nameof(sourceUpdatePolicy), sourceUpdatePolicy, null)
            };
        }
    }
    
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

        var apiResult = await _gqlClient.CollectionRevisionInfo.ExecuteAsync(
            slug: slug.Value,
            revisionNumber: (int)revisionNumber.Value,
            viewAdultContent: true,
            cancellationToken: cancellationToken
        );

        var collectionRevisionInfo = apiResult.Data?.CollectionRevision;
        if (collectionRevisionInfo is null) throw new NotSupportedException($"API call returned no data for collection slug `{slug}` revision `{revisionNumber}`");

        var revisionMetadata = await AddCollectionToDatabase(collectionRoot, collectionRevisionInfo.Collection, collectionRevisionInfo, cancellationToken);
        return revisionMetadata;
    }

    private async ValueTask<CollectionRevisionMetadata.ReadOnly> AddCollectionToDatabase(
        CollectionRoot collectionManifest,
        ICollectionFragment collectionFragment,
        ICollectionRevisionFragment collectionRevisionFragment,
        CancellationToken cancellationToken)
    {
        var gameIds = await CacheGameIds(collectionManifest, cancellationToken);

        using var tx = _connection.BeginTransaction();
        var db = _connection.Db;

        var collectionEntityId = UpdateCollectionInfo(db, tx, collectionFragment);
        var collectionRevisionEntityId = UpdateRevisionInfo(db, tx, collectionEntityId, collectionRevisionFragment);

        var resolvedEntitiesLookup = ResolveModFiles(db, tx, collectionManifest, gameIds, collectionRevisionFragment);
        UpdateFiles(db, tx, collectionRevisionEntityId, collectionRevisionFragment, collectionManifest, gameIds, resolvedEntitiesLookup);

        var results = await tx.Commit();
        var revisionMetadata = CollectionRevisionMetadata.Load(results.Db, results[collectionRevisionEntityId]);

        using var ruleTx = _connection.BeginTransaction();
        AddCollectionDownloadRules(ruleTx, collectionManifest, revisionMetadata);
        await ruleTx.Commit();

        return revisionMetadata.Rebase(_connection.Db);
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

    private static void AddCollectionDownloadRules(
        ITransaction tx,
        CollectionRoot collectionRoot,
        CollectionRevisionMetadata.ReadOnly revisionMetadata)
    {
        var collectionDownloads = revisionMetadata.Downloads.ToArray();

        var modsAndDownloads = GatherDownloads(collectionDownloads, collectionRoot);

        var md5ToDownload = modsAndDownloads
            .Select(static tuple => (tuple.Mod.Source.Md5, tuple.Download))
            .Where(static tuple => tuple.Md5 != default(Md5Value))
            .DistinctBy(static tuple => tuple.Md5)
            .ToDictionary(static tuple => tuple.Md5, static tuple => tuple.Download);

        var tagToDownload = modsAndDownloads
            .Select(static tuple => (tuple.Mod.Source.Tag, tuple.Download))
            .Where(static tuple => !string.IsNullOrWhiteSpace(tuple.Tag))
            .DistinctBy(static tuple => tuple.Tag, StringComparer.Ordinal)
            .ToDictionary(static tuple => tuple.Tag!, static tuple => tuple.Download, StringComparer.Ordinal);

        var fileExpressionToDownload = modsAndDownloads
            .Select(static tuple => (tuple.Mod.Source.FileExpression, tuple.Download))
            .Where(static tuple => tuple.FileExpression != default(RelativePath))
            .Select(static tuple => (FileExpression: tuple.FileExpression.ToString(), tuple.Download))
            .DistinctBy(static tuple => tuple.FileExpression, StringComparer.Ordinal)
            .ToDictionary(static tuple => tuple.FileExpression, static tuple => tuple.Download, StringComparer.Ordinal);

        for (var i = 0; i < collectionRoot.ModRules.Length; i++)
        {
            var rule = collectionRoot.ModRules[i];
            var sourceDownload = VortexModReferenceToCollectionDownload(rule.Source, md5ToDownload, tagToDownload, fileExpressionToDownload);
            var otherDownload = VortexModReferenceToCollectionDownload(rule.Other, md5ToDownload, tagToDownload, fileExpressionToDownload);
            var ruleType = ToRuleType(rule.Type);

            if (!sourceDownload.HasValue || !otherDownload.HasValue || !ruleType.HasValue) continue;

            _ = new CollectionDownloadRules.New(tx)
            {
                SourceId = sourceDownload.Value,
                OtherId = otherDownload.Value,
                RuleType = ruleType.Value,
                ArrayIndex = i,
            };
        }
    }

    private static Optional<CollectionDownloadRuleType> ToRuleType(VortexModRuleType vortexModRuleType)
    {
        return vortexModRuleType switch
        {
            VortexModRuleType.Before => CollectionDownloadRuleType.Before,
            VortexModRuleType.After => CollectionDownloadRuleType.After,
            _ => Optional<CollectionDownloadRuleType>.None,
        };
    }

    private static Optional<CollectionDownload.ReadOnly> VortexModReferenceToCollectionDownload(
        VortexModReference reference,
        Dictionary<Md5Value, CollectionDownload.ReadOnly> md5ToDownload,
        Dictionary<string, CollectionDownload.ReadOnly> tagToDownload,
        Dictionary<string, CollectionDownload.ReadOnly> fileExpressionToDownload)
    {
        // https://github.com/Nexus-Mods/Vortex/blob/1bc2a0bca27353df617f5a0b0f331cf9d23eea9c/src/extensions/mod_management/util/dependencies.ts#L28-L62
        // https://github.com/Nexus-Mods/Vortex/blob/1bc2a0bca27353df617f5a0b0f331cf9d23eea9c/src/extensions/mod_management/util/testModReference.ts#L285-L299

        var md5 = reference.FileMD5;
        if (md5 != default(Md5Value) && md5ToDownload.TryGetValue(md5, out var download)) return download;

        var tag = reference.Tag;
        if (!string.IsNullOrWhiteSpace(tag) && tagToDownload.TryGetValue(tag, out download)) return download;

        var fileExpression = reference.FileExpression;
        if (!string.IsNullOrWhiteSpace(fileExpression) && fileExpressionToDownload.TryGetValue(fileExpression, out download)) return download;

        return Optional<CollectionDownload.ReadOnly>.None;
    }

    private static List<ModAndDownload> GatherDownloads(CollectionDownload.ReadOnly[] items, CollectionRoot root)
    {
        var map = items.ToDictionary(static download => download.ArrayIndex, static download => download);
        var list = new List<ModAndDownload>();

        foreach (var kv in map)
        {
            var (index, download) = kv;
            var mod = root.Mods[index];

            list.Add((mod, download));
        }

        return list;
    }

    private static ResolvedEntitiesLookup ResolveModFiles(
        IDb db,
        ITransaction tx,
        CollectionRoot collectionRoot,
        GameIdCache gameIds,
        ICollectionRevisionFragment collectionRevision)
    {
        var res = new ResolvedEntitiesLookup();
        var modPageIds = new Dictionary<UidForMod, EntityId>();

        foreach (var modFile in collectionRevision.ModFiles)
        {
            var file = modFile.File;
            if (file is null) continue;

            // NOTE(erri120): Need to re-use the entity ids we get back from the `Resolve`
            // method. Otherwise, if a collection contains two files from the same mod page
            // and the mod page isn't in the DB, we'll end up creating two mod pages, one for
            // each file.
            var uidForMod = UidForMod.FromV2Api(file.Mod.Uid);
            if (!modPageIds.TryGetValue(uidForMod, out var modEntityId))
            {
                modEntityId = file.Mod.Resolve(db, tx, setFilesTimestamp: false);
                modPageIds[uidForMod] = modEntityId;
            }

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
        ICollectionRevisionFragment revisionInfo,
        CollectionRoot collectionRoot,
        GameIdCache gameIds,
        ResolvedEntitiesLookup resolvedEntitiesLookup)
    {
        // NOTE(erri120): This exists because we can't replace downloads, there is no consistent identity generator for them.
        // Usually, revisions don't get added twice; they are immutable once published. However, it's possible to add draft
        // revisions to the app, which is what we do when uploading collections.
        var existingDownloads = CollectionDownload.FindByCollectionRevision(db, collectionRevisionEntityId);
        foreach (var entity in existingDownloads)
        {
            tx.Delete(entity, recursive: false);

            // TODO: figure out what we want to do here
            var references = db.Datoms(new ReferencesSlice(entity));
            if (references.Count > 0) throw new NotImplementedException($"Collection download `{entity.Name}` (index={entity.ArrayIndex}) for collection `{collectionRoot.Info.Name}`/`{revisionInfo.RevisionNumber}` can't be deleted because it gets referenced by `{references.Count}` datoms, likely because it got installed");
        }

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

            if (!string.IsNullOrWhiteSpace(collectionMod.Instructions))
                downloadEntity.Instructions = collectionMod.Instructions;

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

    private EntityId UpdateRevisionInfo(
        IDb db,
        ITransaction tx,
        EntityId collectionEntityId,
        ICollectionRevisionFragment revisionInfo)
    {
        var revisionId = RevisionId.From((ulong)revisionInfo.Id);
        var resolver = GraphQLResolver.Create(db, tx, CollectionRevisionMetadata.RevisionId, revisionId);

        resolver.Add(CollectionRevisionMetadata.RevisionNumber, RevisionNumber.From((ulong)revisionInfo.RevisionNumber));
        resolver.Add(CollectionRevisionMetadata.CollectionId, collectionEntityId);
        resolver.Add(CollectionRevisionMetadata.IsAdult, revisionInfo.AdultContent);

        var status = ToStatus(_logger, revisionInfo);
        if (status.HasValue) resolver.Add(CollectionRevisionMetadata.Status, status.Value);

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

    private static Optional<NexusMods.Abstractions.NexusModsLibrary.Models.CollectionStatus> ToStatus(ICollectionFragment collectionFragment)
    {
        if (collectionFragment.CollectionStatus is null) return Optional<NexusMods.Abstractions.NexusModsLibrary.Models.CollectionStatus>.None;
        return collectionFragment.CollectionStatus.Value switch
        {
            CollectionStatus.Unlisted => Abstractions.NexusModsLibrary.Models.CollectionStatus.Unlisted,
            CollectionStatus.Listed => Abstractions.NexusModsLibrary.Models.CollectionStatus.Listed,
            _ => Optional<Abstractions.NexusModsLibrary.Models.CollectionStatus>.None,
        };
    }

    private static Optional<RevisionStatus> ToStatus(ILogger logger, ICollectionRevisionFragment revisionFragment)
    {
        // NOTE(erri120): no idea why the revision fragment has two strings for the status
        Debug.Assert(revisionFragment.Status.Equals(revisionFragment.RevisionStatus, StringComparison.OrdinalIgnoreCase), $"weird things happening: {revisionFragment.Status} != {revisionFragment.RevisionStatus}");

        if (revisionFragment.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
            return RevisionStatus.Draft;
        if (revisionFragment.Status.Equals("published", StringComparison.OrdinalIgnoreCase))
            return RevisionStatus.Published;
        logger.LogWarning("Unknown revision status: `{Status}`", revisionFragment.Status);
        return Optional<RevisionStatus>.None;
    }

    private static EntityId UpdateCollectionInfo(
        IDb db,
        ITransaction tx,
        ICollectionFragment collectionInfo)
    {
        var id = CollectionId.From((ulong)collectionInfo.Id);
        var slug = CollectionSlug.From(collectionInfo.Slug);
        var resolver = GraphQLResolver.Create(db, tx, CollectionMetadata.Slug, slug);

        resolver.Add(CollectionMetadata.CollectionId, id);
        resolver.Add(CollectionMetadata.Name, collectionInfo.Name);
        resolver.Add(CollectionMetadata.GameId, GameId.From((uint)collectionInfo.Game.Id));
        resolver.Add(CollectionMetadata.Summary, collectionInfo.Summary);
        resolver.Add(CollectionMetadata.Endorsements, (ulong)collectionInfo.Endorsements);
        resolver.Add(CollectionMetadata.TotalDownloads, (ulong)collectionInfo.TotalDownloads);

        var status = ToStatus(collectionInfo);
        if (status.HasValue) resolver.Add(CollectionMetadata.Status, status.Value);

        if (float.TryParse(collectionInfo.RecentRating ?? "0.0", CultureInfo.InvariantCulture, out var recentRating))
            resolver.Add(CollectionMetadata.RecentRating, recentRating);
        if (collectionInfo.RecentRatingCount is not null)
            resolver.Add(CollectionMetadata.RecentRatingCount, collectionInfo.RecentRatingCount.Value);

        if (float.TryParse(collectionInfo.OverallRating ?? "0.0", CultureInfo.InvariantCulture, out var overallRating))
            resolver.Add(CollectionMetadata.OverallRating, overallRating);
        if (collectionInfo.OverallRatingCount is not null)
            resolver.Add(CollectionMetadata.OverallRatingCount, collectionInfo.OverallRatingCount.Value);

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
    public ValueTask<CollectionRoot> ParseCollectionJsonFile(
        NexusModsCollectionLibraryFile.ReadOnly collectionLibraryFile,
        CancellationToken cancellationToken)
    {
        var jsonFileEntity = GetCollectionJsonFile(collectionLibraryFile);
        return ParseCollectionJsonFile(jsonFileEntity, cancellationToken);
    }

    /// <summary>
    /// Parses the collection json file.
    /// </summary>
    public async ValueTask<CollectionRoot> ParseCollectionJsonFile(
        LibraryArchiveFileEntry.ReadOnly jsonFileEntity,
        CancellationToken cancellationToken)
    {
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
