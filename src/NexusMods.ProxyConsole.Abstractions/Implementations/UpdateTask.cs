using MemoryPack;

namespace NexusMods.ProxyConsole.Abstractions.Implementations;

/// <summary>
/// Update the state of a task
/// </summary>
[MemoryPackable]
public partial class UpdateTask : IRenderable
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
