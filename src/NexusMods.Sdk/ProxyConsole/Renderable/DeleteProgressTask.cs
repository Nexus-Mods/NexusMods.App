using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A message to delete a progress task
/// </summary>
[MemoryPackable]
[PublicAPI]
public partial class DeleteProgressTask : IRenderable<DeleteProgressTask>
{
    /// <summary>
    /// The unique identifier for the task
    /// </summary>
    public required Guid TaskId { get; init; }
}
