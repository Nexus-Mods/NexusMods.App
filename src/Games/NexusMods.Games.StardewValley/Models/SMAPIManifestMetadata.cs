using NexusMods.Abstractions.Loadouts.Files;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

// ReSharper disable InconsistentNaming

namespace NexusMods.Games.StardewValley.Models;

/// <summary>
/// Marker for manifest files.
/// </summary>
[Include<StoredFile>]
public partial class SMAPIManifestMetadata : IModelDefinition
{
    private const string Namespace = "NexusMods.Games.StardewValley.Models.SMAPIManifestMetadata";
    
    /// <summary>
    /// Marker attribute for SMAPI Manifests
    /// </summary>
    public static readonly MarkerAttribute SMAPIManifest = new(Namespace, nameof(SMAPIManifest));
}
