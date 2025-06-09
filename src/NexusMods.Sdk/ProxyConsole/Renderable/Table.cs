using JetBrains.Annotations;
using MemoryPack;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// A table rendered as an ascii grid.
/// </summary>
[MemoryPackable]
[PublicAPI]
public partial class Table : IRenderable<Table>
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
