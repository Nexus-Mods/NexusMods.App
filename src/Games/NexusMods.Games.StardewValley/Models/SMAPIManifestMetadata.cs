using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.Attributes;
using File = NexusMods.Abstractions.Loadouts.Files.File;

// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;

/// <summary>
/// Marker for manifest files.
/// </summary>


public static class SMAPIManifestMetadata
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIManifestMetadata";
    
    /// <summary>
    /// Marker attribute for SMAPI Manifests
    /// </summary>
    public static readonly BooleanAttribute SMAPIManifest = new(Namespace, "SMAPIManifest");
    
    /// <summary>
    /// Returns true if the file contains the SMAPI manifest marker.
    /// </summary>
    public static bool IsSMAPIManifest(this File.Model file) => file.Contains(SMAPIManifest);
    
    /// <summary>
    /// Returns all the files with the SMAPI manifest marker.
    /// </summary>
    public static IEnumerable<File.Model> SMAPIManifests(this Loadout.Model loadout) 
        => loadout.Files.Where(manifest => manifest.IsSMAPIManifest());
}
