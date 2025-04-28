using JetBrains.Annotations;
using NexusMods.Cascade;
using NexusMods.Cascade.Rules;
using NexusMods.Cascade.Structures;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.BuiltInEntities;
using NexusMods.MnemonicDB.Abstractions.Cascade;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.Paths;

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

    /// <summary>
    /// All the ancestors of a given item
    /// </summary>
    public static readonly Flow<KeyedValue<EntityId, EntityId>> ItemAncestors =
        LoadoutItem.Parent.Ancestors();

    public static readonly Flow<(EntityId LoadoutItemGroupId, int FileCount, Size FileSize)> FileCounts =
        Pattern.Create()
            .With(ItemAncestors, out var itemId, out var groupId)
            .Db(itemId, LoadoutFile.Size, out var fileSize)
            .Return(groupId, fileSize.Count(), fileSize.Sum());

}
