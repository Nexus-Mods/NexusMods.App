using System.Collections.Immutable;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Games;

/// <summary>
/// Record containing all store identifiers for a game.
/// </summary>
[PublicAPI]
public record StoreIdentifiers(GameId GameId)
{
    /// <summary>
    /// All Steam App IDs for the game.
    /// </summary>
    /// <remarks>
    /// Use https://steamdb.info/ to get the IDs. Look up a game, something like https://steamdb.info/app/489830/
    /// and you'll find the App ID in the table at the top as well as in the URL.
    /// </remarks>
    public ImmutableArray<uint> SteamAppIds { get; init; } = ImmutableArray<uint>.Empty;

    /// <summary>
    /// All GOG Product IDs for the game.
    /// </summary>
    /// <remarks>
    /// Use https://www.gogdb.org/ to get the IDs. Look up a game, something like https://www.gogdb.org/product/2093619782
    /// and you'll find the Product ID in the table at the top as well as in the URL.
    /// </remarks>
    public ImmutableArray<long> GOGProductIds { get; init; } = ImmutableArray<long>.Empty;

    /// <summary>
    /// All Epic Games Store Catalog Item IDs for the game.
    /// </summary>
    /// <remarks>
    /// Use https://egdata.app/ to get the IDs. Look up a game, something like https://egdata.app/items/5beededaad9743df90e8f07d92df153f
    /// and you'll find the Item ID in the table at the top as well as in the URL.
    /// </remarks>
    public ImmutableArray<string> EGSCatalogItemId { get; init; } = ImmutableArray<string>.Empty;

    public ImmutableArray<string> XboxPackageIdentifiers { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> OriginManifestIds { get; init; } = ImmutableArray<string>.Empty;
    public ImmutableArray<string> EADesktopSoftwareIds { get; init; } = ImmutableArray<string>.Empty;
}
