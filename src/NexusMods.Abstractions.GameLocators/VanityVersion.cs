using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Represents a version that only exists to be displayed to humans. It carries no data
/// that should be used functions, and effectively has no real value.
/// </summary>
[ValueObject<string>]
[PublicAPI]
public readonly partial struct VanityVersion : IAugmentWith<JsonAugment, DefaultValueAugment, DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static VanityVersion DefaultValue { get; } = From("0.0.0");

    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

/// <summary>
/// Attribute for <see cref="VanityVersion"/>.
/// </summary>
[PublicAPI]
public sealed class VanityVersionAttribute(string ns, string name) : ScalarAttribute<VanityVersion, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc />
    protected override string ToLowLevel(VanityVersion value) => value.Value;

    /// <inheritdoc />
    protected override VanityVersion FromLowLevel(string value, AttributeResolver resolver) => VanityVersion.From(value);
}
