using MemoryPack;

namespace NexusMods.ProxyConsole.Abstractions.Implementations;

/// <summary>
/// A message to create a progress task
/// </summary>
[MemoryPackable]
public partial class CreateProgressTask : IRenderable
{
    /// <summary>
    /// The text to display
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// The unique identifier for the task
    /// </summary>
    public required Guid TaskId { get; init; }
}
