using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization.Attributes;
// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;

public static class SMAPIModMarker
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIModMarker";
    
    /// <summary>
    /// Marker attribute for SMAPI Mods
    /// </summary>
    public static readonly BooleanAttribute SMAPIMod = new(Namespace, "SMAPIMod");
    
    /// <summary>
    /// Returns true if the mod contains the SMAPI marker.
    /// </summary>
    public static bool IsSMAPIMod(this Mod.Model mod) => mod.Contains(SMAPIMod);
    
    /// <summary>
    /// Returns all the mods with the SMAPI marker.
    /// </summary>
    public static IEnumerable<Mod.Model> SMAPIMods(this Loadout.Model loadout) 
        => loadout.Mods.Where(mod => mod.IsSMAPIMod());
}
