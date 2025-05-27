namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents UI documentation metadata for a type of sort order
/// </summary>
public record SortOrderUiMetadata
{
    /// <summary>
    /// Display name for this sort order type
    /// </summary>
    public required string SortOrderName { get; init; }
    
    /// <summary>
    /// Heading for more details load order override information
    /// </summary>
    /// <example>
    /// "Load Order for REDmods in Cyberpunk 2077 - First Loaded Wins"
    /// </example>
    public required string OverrideInfoTitle { get; init; }
    
    /// <summary>
    /// Detailed description of the load order and its override behavior
    /// </summary>
    public required string OverrideInfoMessage { get; init; }
    
    /// <summary>
    /// Short tooltip message to explain the winning index number in the load order
    /// </summary>
    public required string WinnerIndexToolTip { get; init; }
    
    /// <summary>
    /// Header text for the index column
    /// </summary>
    public required string IndexColumnHeader { get; init; }
    
    /// <summary>
    /// Header text for the display name column
    /// </summary>
    public required string DisplayNameColumnHeader { get; init; }

    /// <summary>
    /// Title text to display in case there are no sortable items to sort
    /// </summary>
    public required string EmptyStateMessageTitle { get; init; }
    
    /// <summary>
    /// Contents text to display in case there are no sortable items to sort
    /// </summary>
    public required string EmptyStateMessageContents { get; init; }
    
    /// <summary>
    /// Url for further information about the load order
    /// </summary>
    public required string LearnMoreUrl { get; init; }
}
