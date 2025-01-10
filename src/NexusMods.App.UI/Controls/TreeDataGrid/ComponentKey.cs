using TransparentValueObjects;

namespace NexusMods.App.UI.Controls;

[ValueObject<string>]
public readonly partial struct ComponentKey : IAugmentWith<
    DefaultEqualityComparerAugment,
    DefaultValueAugment,
    JsonAugment>
{
    public static ComponentKey DefaultValue { get; } = From(string.Empty);

    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer => StringComparer.OrdinalIgnoreCase;
}
