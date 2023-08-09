namespace NexusMods.Common.UserInput;

/// <summary>
/// Represents a group of items from which the user has to pick, one, at least one or many.
/// </summary>
/// <typeparam name="TId">Type of unique identifier for this option.</typeparam>
/// <typeparam name="TOptionId">Type of unique identifier for each option.</typeparam>
public record ChoiceGroup<TId, TOptionId>
{
    /// <summary>
    /// Unique identifier for this item.
    /// </summary>
    public required TId Id { get; init; }

    /// <summary>
    /// Name of the group displayed to the user.
    /// For example 'Pick a Texture'.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// The logic to use for picking this option, such as one, multiple etc.
    /// </summary>
    public required ChoiceType Type { get; init; }

    /// <summary>
    /// Contains all the items the user can select from.
    /// </summary>
    public required Option<TOptionId>[] Options { get; init; }
}
