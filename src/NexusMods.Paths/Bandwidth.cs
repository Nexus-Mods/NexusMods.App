using Vogen;

namespace NexusMods.Paths;

/// <summary>
/// Represents bandwidth in bytes per second.
/// </summary>
[ValueObject<ulong>]
public partial struct Bandwidth
{
    /// <inheritdoc />
    public override string ToString() => _value.ToFileSizeString("/sec");
}