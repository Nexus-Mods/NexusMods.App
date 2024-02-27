using JetBrains.Annotations;
using TransparentValueObjects;

namespace NexusMods.Abstractions.Diagnostics;

/// <summary>
/// Represents a message of a diagnostic.
/// </summary>
[PublicAPI]
[ValueObject<string>]
public readonly partial struct DiagnosticMessage : IAugmentWith<DefaultValueAugment>
{
    /// <summary>
    /// Gets the default value.
    /// </summary>
    public static DiagnosticMessage DefaultValue { get; } = From("");
}
