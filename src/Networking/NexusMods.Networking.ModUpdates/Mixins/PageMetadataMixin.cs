using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.Networking.ModUpdates.Mixins;

/// <summary>
/// Implements the MnemonicDB mod page mixin based on V2 API Results.
/// </summary>
public struct PageMetadataMixin : IModFeedItem
{
    private readonly NexusModsModPageMetadata.ReadOnly _metadata;

    private PageMetadataMixin(NexusModsModPageMetadata.ReadOnly metadata) => _metadata = metadata;
    
    /// <inheritodc/>
    public UidForMod GetModPageId() => new()
    {
        GameId = _metadata.Uid.GameId,
        ModId = _metadata.Uid.ModId, 
    };

    /// <summary/>
    public EntityId GetModPageEntityId() => _metadata.Id;
    
    /// <inheritodc/>
    public DateTime GetLastUpdatedDateUtc() => _metadata.UpdatedAt; // <= TODO: Change this with 'last file updated at' when V2 supports this field.

    /// <summary>
    /// Returns the database entries containing page metadata(s) as a mixin.
    /// </summary>
    public static IEnumerable<PageMetadataMixin> EnumerateDatabaseEntries(IDb db) => NexusModsModPageMetadata.All(db).Select(only => new PageMetadataMixin(only));
    
    /// <summary/>
    public static implicit operator NexusModsModPageMetadata.ReadOnly(PageMetadataMixin mixin) => mixin._metadata;

    /// <summary/>
    public static implicit operator PageMetadataMixin(NexusModsModPageMetadata.ReadOnly metadata) => new(metadata);
}
