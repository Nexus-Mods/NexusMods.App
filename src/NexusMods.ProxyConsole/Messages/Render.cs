using MemoryPack;
using NexusMods.Sdk.ProxyConsole;

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
