using TransparentValueObjects;

namespace NexusMods.Networking.NexusWebApi.Errors;

/// <summary>
/// Represents an error code.
/// </summary>
[ValueObject<string>]
public readonly partial struct ErrorCode : IAugmentWith<DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}
