using JetBrains.Annotations;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Base model for a persisted job state, persisted jobs store their state in the database
/// and automatically resume processing when the application restarts.
/// </summary>
[PublicAPI]
public partial class PersistedJobState : IModelDefinition
{
    private const string Namespace = "NexusMods.Jobs";

    /// <summary>
    /// The status of the job.
    /// </summary>
    public static readonly EnumByteAttribute<JobStatus> Status = new(Namespace, nameof(Status)) { IsIndexed = true };

    /// <summary>
    /// The worker that is responsible for processing the job.
    /// </summary>
    public static readonly WorkerAttribute Worker = new(Namespace, nameof(Worker));
}
