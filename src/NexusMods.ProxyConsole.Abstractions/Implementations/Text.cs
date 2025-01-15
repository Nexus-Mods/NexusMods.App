using System;
using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.ProxyConsole.Abstractions.Implementations;

/// <summary>
/// A text renderable.
/// </summary>
[MemoryPackable]
public partial class Text : IRenderable
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
