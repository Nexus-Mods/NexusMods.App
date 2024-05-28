using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Games.StardewValley.Models;

/// <summary>
/// Marker attribute for SMAPI
/// </summary>
public static class SMAPIMarker
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIMarker";
    
    public static readonly StringAttribute Version = new(Namespace, nameof(Version));
}
