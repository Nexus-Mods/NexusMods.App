using JetBrains.Annotations;
using Vogen;

namespace NexusMods.Common.GuidedInstaller.ValueObjects;

/// <summary>
/// Represents a unique identifier of an <see cref="Option"/>.
/// </summary>
[PublicAPI]
[ValueObject<Guid>]
public readonly partial struct OptionId
{
    /// <summary>
    /// The "None" option.
    /// </summary>
    public static readonly OptionId None = From(Guid.Empty);
}
