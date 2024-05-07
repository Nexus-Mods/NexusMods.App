using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using StardewModdingAPI;
using StardewModdingAPI.Toolkit;
// ReSharper disable InconsistentNaming
namespace NexusMods.Games.StardewValley.Models;

/// <summary>
/// Marker attribute for SMAPI
/// </summary>
public static class SMAPIMarker
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIMarker";
    
    public static readonly StringAttribute Version = new(Namespace, "Version");
    
    /// <summary>
    /// Returns true if the mod contains the SMAPI marker.
    /// </summary>
    public static bool IsSMAPI(this Mod.Model mod) => mod.Contains(Version);
    
    /// <summary>
    /// Returns the mod with the SMAPI marker.
    /// </summary>
    public static Mod.Model? SMAPIMod(this Loadout.Model loadout) => loadout.Mods.FirstOrDefault(mod => mod.IsSMAPI());
}
