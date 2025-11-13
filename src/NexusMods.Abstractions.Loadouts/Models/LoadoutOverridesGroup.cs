using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Files found in the game folder that are not part of the loadout are first placed in this group,
/// then later can be moved to other parts of the loadout.
/// </summary>
[Include<LoadoutItemGroup>]
public partial class LoadoutOverridesGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutOverridesGroup";

    public static readonly ReferenceAttribute<Loadout> OverridesFor = new(Namespace, nameof(OverridesFor));
}
