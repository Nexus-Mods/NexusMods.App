using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Games.StardewValley.Models;

[Obsolete("Will be removed with a future migration")]
[Include<LoadoutItemGroup>]
public partial class SMAPIModLoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.StardewValley.SMAPIModLoadoutItem";

    public static readonly ReferenceAttribute<SMAPIManifestLoadoutFile> Manifest = new(Namespace, nameof(Manifest));
}
