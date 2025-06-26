using NexusMods.Abstractions.Games.FileHashes.Attributes.Steam;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Steam.Models;

/// <summary>
/// Steam entitlements
/// </summary>
public partial class SteamLicenses : IModelDefinition
{
    private const string Namespace = "NexusMods.Networking.Steam.Models";

    /// <summary>
    /// The app id of the game this license is for.
    /// </summary>
    public static readonly AppIdsAttribute AppIds = new(nameof(Namespace), nameof(AppIds)) { IsIndexed = true };
    
    /// <summary>
    /// The package id of the game
    /// </summary>
    public static readonly PackageIdAttribute PackageId = new(nameof(Namespace), nameof(PackageId)) { IsIndexed = true, IsUnique = true };
}
