using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Jobs.Tests.TestHelpers;

[Include<PersistedJobState>]
public partial class SlowResumableJobPersistedState : IModelDefinition
{
    private const string Namespace = "NexusMods.Jobs.Tests";

    public static readonly ULongAttribute Max = new(Namespace, nameof(Max));
    public static readonly ULongAttribute Current = new(Namespace, nameof(Current));
    
}
