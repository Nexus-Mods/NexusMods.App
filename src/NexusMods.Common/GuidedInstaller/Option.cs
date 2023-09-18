using JetBrains.Annotations;
using NexusMods.Common.GuidedInstaller.ValueObjects;

namespace NexusMods.Common.GuidedInstaller;

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
    /// Gets the <see cref="AssetUrl"/> of the image.
    /// </summary>
    public AssetUrl? ImageUrl { get; init; }

    /// <summary>
    /// Gets the hover text of the option.
    /// </summary>
    public string? HoverText { get; init; }
}
