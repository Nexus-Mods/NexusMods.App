using JetBrains.Annotations;
using NexusMods.App.UI.Controls.Filters;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for version fields that supports version-specific filtering.
/// </summary>
/// <remarks>
/// Note(sewer): If you need filtering in the style of 'semantic versioning', you could add it either here
/// or make a `SemanticVersionComponent` type based on this one.
///
/// At the time of implementation, this matches by string because this is only used in Library, where
/// mod author supplied version strings are not guaranteed to be semantic.
/// </remarks>
[PublicAPI]
public sealed class VersionComponent : AValueComponent<string>, IItemModelComponent<VersionComponent>, IComparable<VersionComponent>
{
    /// <inheritdoc/>
    public VersionComponent(
        string initialValue,
        IObservable<string> valueObservable,
        bool subscribeWhenCreated = false,
        bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated, observeOutsideUiThread) { }

    /// <inheritdoc/>
    public VersionComponent(
        string initialValue,
        Observable<string> valueObservable,
        bool subscribeWhenCreated = false,
        bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated, observeOutsideUiThread) { }

    /// <inheritdoc/>
    public VersionComponent(string value) : base(value) { }

    /// <inheritdoc/>
    public int CompareTo(VersionComponent? other) => string.CompareOrdinal(Value.Value, other?.Value.Value);

    /// <inheritdoc/>
    public FilterResult MatchesFilter(Filter filter)
    {
        return filter switch
        {
            Filter.VersionFilter versionFilter => Value.Value.Contains(
                versionFilter.VersionPattern, 
                StringComparison.OrdinalIgnoreCase)
                ? FilterResult.Pass : FilterResult.Fail,
            _ => FilterResult.Indeterminate // Default: no opinion
        };
    }
} 