using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Update the state of a task
/// </summary>
[MemoryPackable]
[PublicAPI]
public partial class UpdateTask : IRenderable<UpdateTask>
{
    /// <summary>
    /// The unique identifier for the task
    /// </summary>
    public required Guid TaskId { get; init; }
    
    /// <summary>
    /// Set the progress of a task
    /// </summary>
    public required double IncrementProgressBy { get; init; }
}
