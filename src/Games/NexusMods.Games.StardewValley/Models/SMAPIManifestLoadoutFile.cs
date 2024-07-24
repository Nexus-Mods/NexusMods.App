using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.StardewValley.Models;

[Include<LoadoutFile>]
public partial class SMAPIManifestLoadoutFile : IModelDefinition
{
    private const string Namespace = "NexusMods.StardewValley.SMAPIManifestLoadoutFile";

    public static readonly MarkerAttribute IsManifestMarker = new(Namespace, nameof(IsManifestMarker));
}
