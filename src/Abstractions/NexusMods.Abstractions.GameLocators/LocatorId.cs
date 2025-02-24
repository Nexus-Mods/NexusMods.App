using System.Collections;
using JetBrains.Annotations;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.ValueSerializers;
using TransparentValueObjects;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Represents an opaque ID created by locators.
/// </summary>
/// <remarks>
/// The meaning of an ID is dependent on the locator.
/// </remarks>
[ValueObject<string>]
[PublicAPI]
public readonly partial struct LocatorId : IAugmentWith<JsonAugment, DefaultValueAugment, DefaultEqualityComparerAugment>
{
    /// <inheritdoc/>
    public static LocatorId DefaultValue { get; } = From(string.Empty);

    /// <inheritdoc/>
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer { get; } = StringComparer.OrdinalIgnoreCase;
}

/// <summary>
/// Tuple of <see cref="GameStore"/> and multiple <see cref="LocatorId"/>.
/// </summary>
[PublicAPI]
public record struct LocatorIdsWithGameStore(GameStore GameStore, LocatorId[] LocatorIds) : IEnumerable<LocatorId>
{
    /// <inheritdoc/>
    public IEnumerator<LocatorId> GetEnumerator() => LocatorIds.AsEnumerable().GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Implicit conversion.
    /// </summary>
    public static implicit operator LocatorIdsWithGameStore((GameStore, LocatorId[]) tuple) => new(tuple.Item1, tuple.Item2);
}

/// <summary>
/// Attribute for many <see cref="LocatorId"/>.
/// </summary>
[PublicAPI]
public sealed class LocatorIdsAttribute(string ns, string name) : CollectionAttribute<LocatorId, string, Utf8Serializer>(ns, name)
{
    /// <inheritdoc/>
    protected override string ToLowLevel(LocatorId value) => value.Value;

    /// <inheritdoc/>
    protected override LocatorId FromLowLevel(string value, AttributeResolver resolver) => LocatorId.From(value);
}

