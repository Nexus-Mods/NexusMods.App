using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a loadout item with a target path.
/// </summary>
[Include<LoadoutItem>]
[PublicAPI]
public partial class LoadoutItemWithTargetPath : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutItemWithTargetPath";

    /// <summary>
    /// The target path.
    /// </summary>
    public static readonly GamePathParentAttribute TargetPath = new(Namespace, nameof(TargetPath)) { IsIndexed = true };
}
