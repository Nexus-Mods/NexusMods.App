using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A text renderable.
/// </summary>
[MemoryPackable]
[PublicAPI]
public partial class Text : IRenderable<Text>
{
    /// <summary>
    /// Formatting template, for the text
    /// </summary>
    public required string Template { get; init; }

    /// <summary>
    /// The arguments to be used in the template
    /// </summary>
    public string[] Arguments { get; init; } = [];
}
