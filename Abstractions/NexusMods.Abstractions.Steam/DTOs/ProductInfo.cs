using NexusMods.Abstractions.Steam.Values;

namespace NexusMods.Abstractions.Steam.DTOs;

/// <summary>
/// Information about a product (a game) on Steam.
/// </summary>
public class ProductInfo
{
    /// <summary>
    /// The revision number of this product info.
    /// </summary>
    public required uint ChangeNumber { get; init; }
    
    /// <summary>
    /// The app id of the product.
    /// </summary>
    public required AppId AppId { get; init; }
    
    /// <summary>
    /// The depots of the product.
    /// </summary>
    public required Depot[] Depots { get; init; }
}
