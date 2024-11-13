using System.Text.Json;
using NexusMods.Abstractions.Collections.Json;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi.Extensions;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;

public partial class NexusModsLibrary
{
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
        if (collectionRevisionInfo is null) throw new NotImplementedException($"API call returned no data for `{slug}` `{revisionNumber}`");

        using var tx = _connection.BeginTransaction();
        var db = _connection.Db;

        var collectionEntityId = UpdateCollectionInfo(db, tx, slug, collectionRevisionInfo.Collection);
        var collectionRevisionEntityId = UpdateRevisionInfo(db, tx, revisionNumber, collectionEntityId, collectionRevisionInfo);

        UpdateFiles(db, tx, collectionRevisionEntityId, collectionRevisionInfo);

        var results = await tx.Commit();
        return CollectionRevisionMetadata.Load(results.Db, results[collectionRevisionEntityId]);
    }

    private static void UpdateFiles(
        IDb db,
        ITransaction tx,
        EntityId collectionRevisionEntityId,
        ICollectionRevisionInfo_CollectionRevision revisionInfo)
    {
        // TODO: use data from collection json file

        foreach (var apiModFile in revisionInfo.ModFiles)
        {
            var apiFileInfo = apiModFile.File!;

            var modEntityId = apiFileInfo.Mod.Resolve(db, tx);
            var fileEntityId = apiFileInfo.Resolve(db, tx, modEntityId);

            var revisionFileResolver = GraphQLResolver.Create(db, tx, CollectionRevisionModFile.FileId, ulong.Parse(apiModFile.Id));
            revisionFileResolver.Add(CollectionRevisionModFile.CollectionRevision, collectionRevisionEntityId);
            revisionFileResolver.Add(CollectionRevisionModFile.NexusModFile, fileEntityId);
            revisionFileResolver.Add(CollectionRevisionModFile.IsOptional, apiModFile.Optional);
        }
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
        resolver.Add(CollectionRevisionMetadata.Downloads, (ulong)revisionInfo.TotalDownloads);

        if (ulong.TryParse(revisionInfo.TotalSize, out var totalSize))
            resolver.Add(CollectionRevisionMetadata.TotalSize, Size.From(totalSize));
        else
            resolver.Add(CollectionRevisionMetadata.TotalSize, Size.Zero);

        if (float.TryParse(revisionInfo.OverallRating ?? "0.0", out var overallRating))
            resolver.Add(CollectionRevisionMetadata.OverallRating, overallRating / 100);

        resolver.Add(CollectionRevisionMetadata.TotalRatings, (ulong)(revisionInfo.OverallRatingCount ?? 0));
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
        resolver.Add(CollectionMetadata.Summary, collectionInfo.Summary);
        resolver.Add(CollectionMetadata.Endorsements, (ulong)collectionInfo.Endorsements);

        if (Uri.TryCreate(collectionInfo.TileImage?.ThumbnailUrl, UriKind.Absolute, out var tileImageUri))
            resolver.Add(CollectionMetadata.TileImageUri, tileImageUri);

        if (Uri.TryCreate(collectionInfo.HeaderImage?.Url, UriKind.Absolute, out var backgroundImageUri))
            resolver.Add(CollectionMetadata.BackgroundImageUri, backgroundImageUri);

        var user = collectionInfo.User.Resolve(db, tx);
        resolver.Add(CollectionMetadata.Author, user);

        return resolver.Id;
    }

    private async ValueTask<CollectionRoot> ParseCollectionJsonFile(
        NexusModsCollectionLibraryFile.ReadOnly collectionLibraryFile,
        CancellationToken cancellationToken)
    {
        if (!collectionLibraryFile.AsLibraryFile().TryGetAsLibraryArchive(out var archive))
            throw new InvalidOperationException("The source collection is not a library archive");

        var jsonFileEntity = archive.Children.FirstOrDefault(f => f.Path == "collection.json");

        await using var data = await _fileStore.GetFileStream(jsonFileEntity.AsLibraryFile().Hash, token: cancellationToken);
        var root = await JsonSerializer.DeserializeAsync<CollectionRoot>(data, _jsonSerializerOptions, cancellationToken: cancellationToken);

        if (root is null) throw new InvalidOperationException("Unable to deserialize collection JSON file");
        return root;
    }
}
