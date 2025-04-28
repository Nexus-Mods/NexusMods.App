using JetBrains.Annotations;
using NexusMods.Abstractions.GuidedInstallers.ValueObjects;

namespace NexusMods.Abstractions.GuidedInstallers;

/// <summary>
/// Represents a single option inside an <see cref="OptionGroup"/>.
/// </summary>
[PublicAPI]
public record Option
{
    /// <summary>
    /// Gets the unique identifier of the option.
    /// </summary>
    public required OptionId Id { get; init; }

    /// <summary>
    /// Gets the type of the option.
    /// </summary>
    public required OptionType Type { get; init; }

    /// <summary>
    /// Gets the name of the option.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the option.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the image of the option.
    /// </summary>
    public OptionImage? Image { get; init; }

    /// <summary>
    /// Gets the hover text of the option.
    /// </summary>
    public string? HoverText { get; init; }
}
