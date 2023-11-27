using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Common.GuidedInstaller.ValueObjects;

/// <summary>
/// Represents a unique identifier of an <see cref="Option"/>.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct OptionId : IAugmentWith<DefaultValueAugment>
{
    /// <summary>
    /// The "None" option.
    /// </summary>
    public static OptionId DefaultValue { get; } = From(Guid.Empty);
}
