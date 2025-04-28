using JetBrains.Annotations;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Resources.DB;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Abstractions.Telemetry;
using NexusMods.Cascade;
using NexusMods.Cascade.Rules;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// Represents a remote mod page on NexusMods.
/// </summary>
[PublicAPI]
public partial class NexusModsModPageMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.NexusModsLibrary.NexusModsModPageMetadata";

    /// <summary>
    /// The ID of the mod page.
    /// </summary>
    public static readonly UidForModAttribute Uid = new(Namespace, nameof(Uid)) { IsIndexed = true };

    /// <summary>
    /// The name of the mod page.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// The game of the mod page.
    /// </summary>
    /// <remarks>
    ///     This will be deprecated in the future, since V2 API only needs <see cref="Uid"/>
    ///     which contains the <see cref="GameId"/> The <see cref="GameDomain"/> is a legacy field of the V1 API.
    /// </remarks>
    public static readonly GameDomainAttribute GameDomain = new(Namespace, nameof(GameDomain)) { IsIndexed = true };

    /// <summary>
    /// The last time the mod page was updated (UTC). This is useful for cache invalidation.
    /// </summary>
    public static readonly TimestampAttribute UpdatedAt = new(Namespace, nameof(UpdatedAt));

    /// <summary>
    /// The last time a full update of this page info was performed in the data store.
    /// This is used for local caching.
    /// </summary>
    public static readonly TimestampAttribute DataUpdatedAt = new(Namespace, nameof(DataUpdatedAt));
    
    /// <summary>
    /// Uri for the full sized picture of the mod.
    /// </summary>
    public static readonly UriAttribute FullSizedPictureUri = new(Namespace, nameof(FullSizedPictureUri)) { IsOptional = true };

    public static readonly ReferenceAttribute<PersistedDbResource> ThumbnailResource = new(Namespace, nameof(ThumbnailResource)) { IsOptional = true };

    /// <summary>
    /// Uri for the thumbnail of the full sized picture.
    /// </summary>
    public static readonly UriAttribute ThumbnailUri = new(Namespace, nameof(ThumbnailUri)) { IsOptional = true };

    /// <summary>
    /// Back-reference to all files from this page.
    /// </summary>
    public static readonly BackReferenceAttribute<NexusModsFileMetadata> Files = new(NexusModsFileMetadata.ModPage);


    public static class Queries
    {
        /// <summary>
        /// A count of how many files are in the library for a given ModPage, along with the extracted GameId,
        /// and the total file size of all files.
        /// </summary>
        public static readonly Flow<(EntityId ModPageId, GameId GameId, int FileCount, Size FileSizes, DateTimeOffset MostRecentAdded)> FileStats =
            Pattern.Create()
                .Db(out var libraryItemId, NexusModsLibraryItem.ModPageMetadataId, out var modPageId, out var txId)
                .Db(modPageId, NexusModsModPageMetadata.Uid, out var uid)
                .Project(uid, uid => uid.GameId, out var gameId)
                .DbOrDefault(libraryItemId, LibraryFile.Size, out var fileSize)
                .Db(txId, Transaction.Timestamp, out var timestamp)
                .Return(modPageId, gameId, libraryItemId.Count(), fileSize.Sum(), timestamp.Max());
    }
}
