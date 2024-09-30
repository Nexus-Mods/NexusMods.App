using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusModsLibrary.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Extensions;

/// <summary>
/// Extensions to GraphQL fragments.
/// </summary>
public static class FragmentExtensions
{
    /// <summary>
    /// Resolves the IUserFragment to an entity in the database, inserting or updating as necessary.
    /// </summary>
    public static async Task<EntityId> Resolve(this IUserFragment userFragment, IDb db, ITransaction tx, HttpClient client, CancellationToken token)
    {
        var userResolver = GraphQLResolver.Create(db, tx, User.NexusId, (ulong)userFragment.MemberId);
        userResolver.Add(User.Name, userFragment.Name);
        userResolver.Add(User.Avatar, new Uri(userFragment.Avatar));
        
        var avatarImage = await DownloadImage(client, userFragment.Avatar, token);
        userResolver.Add(User.AvatarImage,avatarImage);
        return userResolver.Id;
    }
    
    /// <summary>
    /// Resolves the IModFragment to an entity in the database, inserting or updating as necessary.
    /// </summary>
    public static EntityId Resolve(this IModFileFragment modFileFragment, IDb db, ITransaction tx, EntityId modEId)
    {
        var nexusFileResolver = GraphQLResolver.Create(db, tx, (NexusModsFileMetadata.FileId, FileId.From((uint)modFileFragment.FileId)), (NexusModsFileMetadata.ModPageId,  modEId));
        nexusFileResolver.Add(NexusModsFileMetadata.ModPageId, modEId);
        nexusFileResolver.Add(NexusModsFileMetadata.Name, modFileFragment.Name);
        nexusFileResolver.Add(NexusModsFileMetadata.Version, modFileFragment.Version);
        if (ulong.TryParse(modFileFragment.SizeInBytes, out var size))
            nexusFileResolver.Add(NexusModsFileMetadata.Size, Size.From(size));
        return nexusFileResolver.Id;
    }

    /// <summary>
    /// Resolves the IModFragment to an entity in the database, inserting or updating as necessary.
    /// </summary>
    public static EntityId Resolve(this IModFragment modFragment, IDb db, ITransaction tx)
    {
        var nexusModResolver = GraphQLResolver.Create(db, tx, NexusModsModPageMetadata.ModId, ModId.From((uint)modFragment.ModId));
        
        nexusModResolver.Add(NexusModsModPageMetadata.Name, modFragment.Name);
        nexusModResolver.Add(NexusModsModPageMetadata.GameDomain, GameDomain.From(modFragment.Game.DomainName));
        nexusModResolver.Add(NexusModsModPageMetadata.GameId, GameId.From((uint)modFragment.Game.Id));
        nexusModResolver.Add(NexusModsModPageMetadata.UpdatedAt, modFragment.UpdatedAt.UtcDateTime);

        if (Uri.TryCreate(modFragment.PictureUrl, UriKind.Absolute, out var fullSizedPictureUri))
            nexusModResolver.Add(NexusModsModPageMetadata.FullSizedPictureUri, fullSizedPictureUri);

        if (Uri.TryCreate(modFragment.ThumbnailUrl, UriKind.Absolute, out var thumbnailUri))
            nexusModResolver.Add(NexusModsModPageMetadata.ThumbnailUri, thumbnailUri);
        return nexusModResolver.Id;
    }

    private static async Task<byte[]> DownloadImage(HttpClient client, string? uri, CancellationToken token)
    {
        if (uri is null) return [];
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var imageUri)) return [];
        
        return await client.GetByteArrayAsync(imageUri, token);
    }
    
}
