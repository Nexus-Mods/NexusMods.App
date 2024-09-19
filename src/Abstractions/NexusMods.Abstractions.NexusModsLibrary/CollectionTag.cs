using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.NexusModsLibrary;

/// <summary>
/// A tag for a collection.
/// </summary>
public partial class CollectionTag : IModelDefinition
{
    private const string Namespace = "NexusMods.Abstractions.NexusModsLibrary.CollectionTag";
    
    /// <summary>
    /// The name of the collection tag.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name)) { IsIndexed = true };
    
    /// <summary>
    /// The Nexus mods id of the collection tag.
    /// </summary>
    public static readonly ULongAttribute NexusId = new(Namespace, nameof(NexusId)) { IsIndexed = true };
}
