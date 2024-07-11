using JetBrains.Annotations;
using NexusMods.Abstractions.Library;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents an item inside a Loadout.
/// </summary>
[PublicAPI]
public partial class LoadoutItem : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadoutItem";

    /// <summary>
    /// Name of the item.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));

    /// <summary>
    /// Loadout that contains the item.
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout)) { IsIndexed = true };

    /// <summary>
    /// Optional parent of the item.
    /// </summary>
    public static readonly ReferenceAttribute<LoadoutItemGroup> Parent = new(Namespace, nameof(Parent)) { IsIndexed = true, IsOptional = true };

    /// <summary>
    /// Optional source of the item.
    /// </summary>
    public static readonly ReferenceAttribute<LibraryItem> Source = new(Namespace, nameof(Source)) { IsIndexed = true, IsOptional = true };
}
