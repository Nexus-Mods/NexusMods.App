using MemoryPack;
using NexusMods.ProxyConsole.Abstractions;

namespace NexusMods.ProxyConsole.Abstractions.Implementations;

/// <summary>
/// A table rendered as an ascii grid.
/// </summary>
[MemoryPackable]
public partial class Table : IRenderable
{
    /// <summary>
    /// The columns of the table.
    /// </summary>
    public required IRenderable[] Columns { get; init; }

    /// <summary>
    /// The rows of the table.
    /// </summary>
    public required IRenderable[][] Rows { get; init; }
}
