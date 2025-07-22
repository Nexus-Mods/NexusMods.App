using NexusMods.App.UI.Controls.Filters;
using R3;

namespace NexusMods.App.UI.Controls.Search;

/// <summary>
/// Interface for adapters that support search functionality.
/// </summary>
public interface ISearchableAdapter
{
    /// <summary>
    /// The filter property for controlling search results.
    /// </summary>
    ReactiveProperty<Filter> Filter { get; }
}
