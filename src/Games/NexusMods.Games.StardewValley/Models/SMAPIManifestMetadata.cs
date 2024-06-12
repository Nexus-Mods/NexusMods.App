using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using File = NexusMods.Abstractions.Loadouts.Files.File;

// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;

/// <summary>
/// Marker for manifest files.
/// </summary>
[Include<File>]
public partial class SMAPIManifestMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIManifestMetadata";
    
    /// <summary>
    /// Marker attribute for SMAPI Manifests
    /// </summary>
    public static readonly BooleanAttribute SMAPIManifest = new(Namespace, "SMAPIManifest");
}
