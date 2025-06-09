using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A message to create a progress task
/// </summary>
[MemoryPackable]
[PublicAPI]
public partial class CreateProgressTask : IRenderable<CreateProgressTask>
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
