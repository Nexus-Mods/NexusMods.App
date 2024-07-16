using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public partial class PersistedJobState : IModelDefinition
{
    private const string Namespace = "NexusMods.Jobs";

    public static readonly EnumAttribute<JobStatus> Status = new(Namespace, nameof(Status)) { IsIndexed = true };
}
