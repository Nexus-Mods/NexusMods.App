using System.Diagnostics;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Extensions;

/// <summary>
/// Extensions to GraphQL fragments.
/// </summary>
public static class FragmentExtensions
{
    /// <summary>
    /// Resolves a category.
    /// </summary>
    public static EntityId Resolve(this ICollectionRevisionInfo_CollectionRevision_Collection_Category category, IDb db, ITransaction tx)
    {
        var resolver = GraphQLResolver.Create(db, tx, CollectionCategory.NexusId, (ulong)category.Id);
        resolver.Add(CollectionCategory.Name, category.Name);
        return resolver.Id;
    }

    /// <summary>
    /// Resolves the IUserFragment to an entity in the database, inserting or updating as necessary.
    /// </summary>
    public static EntityId Resolve(this IUserFragment userFragment, IDb db, ITransaction tx)
    {
        var userResolver = GraphQLResolver.Create(db, tx, User.NexusId, (ulong)userFragment.MemberId);
        userResolver.Add(User.Name, userFragment.Name);
        userResolver.Add(User.AvatarUri, new Uri(userFragment.Avatar));
        return userResolver.Id;
    }

    /// <summary>
    /// Resolves the <see cref="IModFragment"/> to an entity in the database, inserting or updating as necessary.
    /// </summary>
    /// <param name="modFileFragment">Fragment obtained from the GraphQL API call.</param>
    /// <param name="db">Provides DB access.</param>
    /// <param name="tx">The current transaction for inserting items into database.,</param>
    /// <param name="modPageEid">ID of the mod page entity.</param>
    public static EntityId Resolve(this IModFileFragment modFileFragment, IDb db, ITransaction tx, EntityId modPageEid)
    {
        var nexusFileResolver = GraphQLResolver.Create(db, tx, NexusModsFileMetadata.Uid, UidForFile.FromV2Api(modFileFragment.Uid));
        nexusFileResolver.Add(NexusModsFileMetadata.ModPageId, modPageEid);
        nexusFileResolver.Add(NexusModsFileMetadata.Name, modFileFragment.Name);
        nexusFileResolver.Add(NexusModsFileMetadata.Version, modFileFragment.Version);
        nexusFileResolver.Add(NexusModsFileMetadata.UploadedAt,  DateTimeOffset.FromUnixTimeSeconds(modFileFragment.Date).DateTime);

        if (ulong.TryParse(modFileFragment.SizeInBytes, out var size))
        {
            nexusFileResolver.Add(NexusModsFileMetadata.Size, Size.From(size));
        }
        else
        {
            Debug.WriteLine($"Unable to parse `{modFileFragment.SizeInBytes}` as bytes for Uid `{modFileFragment.Uid}`");
            nexusFileResolver.Add(NexusModsFileMetadata.Size, Size.Zero);
        }

        return nexusFileResolver.Id;
    }

    /// <summary>
    /// Resolves the IModFragment to an entity in the database, inserting or updating as necessary.
    /// </summary>
    public static EntityId Resolve(this IModFragment modFragment, IDb db, ITransaction tx, bool setFilesTimestamp = false)
    {
        var nexusModResolver = GraphQLResolver.Create(db, tx, NexusModsModPageMetadata.Uid, UidForMod.FromV2Api(modFragment.Uid));
        nexusModResolver.Add(NexusModsModPageMetadata.Name, modFragment.Name);
        nexusModResolver.Add(NexusModsModPageMetadata.GameDomain, GameDomain.From(modFragment.Game.DomainName));
        nexusModResolver.Add(NexusModsModPageMetadata.UpdatedAt, modFragment.UpdatedAt.UtcDateTime);

        if (Uri.TryCreate(modFragment.PictureUrl, UriKind.Absolute, out var fullSizedPictureUri))
            nexusModResolver.Add(NexusModsModPageMetadata.FullSizedPictureUri, fullSizedPictureUri);

        if (Uri.TryCreate(modFragment.ThumbnailUrl, UriKind.Absolute, out var thumbnailUri))
            nexusModResolver.Add(NexusModsModPageMetadata.ThumbnailUri, thumbnailUri);

        if (setFilesTimestamp)
            nexusModResolver.Add(NexusModsModPageMetadata.DataUpdatedAt, DateTimeOffset.UtcNow);

        return nexusModResolver.Id;
    }

    private static async Task<byte[]> DownloadImage(HttpClient client, string? uri, CancellationToken token)
    {
        if (uri is null) return [];
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var imageUri)) return [];

        return await client.GetByteArrayAsync(imageUri, token);
    }
    
}
