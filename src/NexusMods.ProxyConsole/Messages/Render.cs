using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.ProxyConsole.Messages;

/// <summary>
/// A render message
/// </summary>
[MemoryPackable]
public partial class Render : IMessage
{
    /// <summary>
    /// The renderable to render.
    /// </summary>
    public required IRenderable Renderable { get; init; }
}
