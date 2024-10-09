using TransparentValueObjects;
namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Machine friendly name for the game, should be devoid of special characters
/// that may conflict with URLs or file paths.
///
/// Sometimes used as a Game ID.
/// </summary>
/// <remarks>
///    Usually we match these with NexusMods' URLs.
/// </remarks>
[ValueObject<string>]
public readonly partial struct GameDomain : IAugmentWith<DefaultValueAugment, JsonAugment, DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static GameDomain DefaultValue { get; } = From("Unknown");

    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}
