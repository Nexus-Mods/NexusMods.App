using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a group of items.
/// </summary>
[PublicAPI]
[Include<LoadoutItem>]
public partial class LoadoutItemGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutItemGroup";

    /// <summary>
    /// Marker.
    /// </summary>
    public static readonly MarkerAttribute Group = new(Namespace, nameof(Group)) { IsIndexed = true };

    /// <summary>
    /// Children of the group.
    /// </summary>
    public static readonly BackReferenceAttribute<LoadoutItem> Children = new(LoadoutItem.Parent);
}
