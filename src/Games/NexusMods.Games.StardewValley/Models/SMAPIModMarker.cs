using NexusMods.MnemonicDB.Abstractions.Attributes;

namespace NexusMods.Games.StardewValley.Models;

[Obsolete($"To be replaced with {nameof(SMAPIModLoadoutItem)}")]
public static class SMAPIModMarker
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIModMarker";
    
    /// <summary>
    /// Marker attribute for SMAPI Mods
    /// </summary>
    public static readonly MarkerAttribute IsSMAPIMod = new(Namespace, nameof(IsSMAPIMod));
}
