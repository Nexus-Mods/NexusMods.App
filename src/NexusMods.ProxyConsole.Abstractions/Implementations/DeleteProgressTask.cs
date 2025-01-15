using MemoryPack;

namespace NexusMods.ProxyConsole.Abstractions.Implementations;

/// <summary>
/// A message to delete a progress task
/// </summary>
[MemoryPackable]
public partial class DeleteProgressTask : IRenderable
{
    /// <summary>
    /// The unique identifier for the task
    /// </summary>
    public required Guid TaskId { get; init; }
}
