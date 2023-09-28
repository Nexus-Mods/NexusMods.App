namespace NexusMods.Common;

/// <summary>
/// concrete implementation for id generator using standard libraries as far as possible
/// </summary>
// ReSharper disable once InconsistentNaming
public class IDGenerator : IIDGenerator
{
    /// <inheritdoc/>
    public string UUIDv4()
    {
        return Guid.NewGuid().ToString("N");
    }
}
