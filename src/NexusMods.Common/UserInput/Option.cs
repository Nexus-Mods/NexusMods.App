namespace NexusMods.Common.UserInput;

/// <summary>
/// Represents an individual selectable item presented to the user.
/// </summary>
/// <typeparam name="TId">Type of unique identifier for the item.</typeparam>
public record Option<TId>
{
    /// <summary>
    /// Uniquely identifies this option.
    /// </summary>
    public required TId Id { get; init; }

    /// <summary>
    /// Name of the individual selectable option, e.g.
    /// 'Checked Suit', 'Purple Suit'
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Short description of this item, e.g.
    /// 'Equips Joker with a Blue Suit'
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// URL to the image associated with this selection.
    /// This can be the path to a file or to a remote resource.
    /// </summary>
    public AssetUrl? ImageUrl { get; init; }

    /// <summary>
    /// Text displayed to the user when they hover over the option with the mouse.
    /// </summary>
    public string? HoverText { get; init; }

    /// <summary>
    /// The logic to use for picking this option, such as one, multiple etc.
    /// </summary>
    public OptionState Type { get; set; } = OptionState.Available;
}
