using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Collections;

[Include<NexusCollectionItemLoadoutGroup>]
public partial class NexusCollectionReplicatedLoadoutGroup : IModelDefinition
{
    private const string Namespace = "NexusMods.Collections.NexusCollectionReplicatedLoadoutGroup";

    public static readonly MarkerAttribute Replicated = new(Namespace, nameof(Replicated)) { IsIndexed = true };
}
