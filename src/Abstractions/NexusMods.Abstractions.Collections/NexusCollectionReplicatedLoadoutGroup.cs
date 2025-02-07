using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

/// <summary>
/// Represents a collection mod that was installed using the "replicated"-method. Instead of using an installer,
/// the collection contains information about where to put each file of the mod, making these an exact replica of
/// what the collection author had on their machine.
/// </summary>
[Include<NexusCollectionItemLoadoutGroup>]
public partial class NexusCollectionReplicatedLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionReplicatedLoadoutGroup";

    public static readonly MarkerAttribute Replicated = new(Namespace, nameof(Replicated)) { IsIndexed = true };
}
