using JetBrains.Annotations;
using NexusMods.App.UI.Controls.TreeDataGrid.Filters;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for <see cref="string"/>.
/// </summary>
[PublicAPI]
public sealed class StringComponent : AValueComponent<string>, IItemModelComponent<StringComponent>, IComparable<StringComponent>
{
    /// <inheritdoc/>
    public int CompareTo(StringComponent? other) => string.CompareOrdinal(Value.Value, other?.Value.Value);

    /// <inheritdoc/>
    public StringComponent(
        string initialValue,
        IObservable<string> valueObservable,
        bool subscribeWhenCreated = false,
        bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated, observeOutsideUiThread) { }

    /// <inheritdoc/>
    public StringComponent(
        string initialValue,
        Observable<string> valueObservable,
        bool subscribeWhenCreated = false,
        bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated, observeOutsideUiThread) { }

    /// <inheritdoc/>
    public StringComponent(string value) : base(value) { }

    /// <inheritdoc/>
    public FilterResult MatchesFilter(Filter filter)
    {
        return filter switch
        {
            Filter.NameFilter nameFilter => Value.Value.Contains(
                nameFilter.SearchText, 
                nameFilter.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase)
                ? FilterResult.Pass : FilterResult.Fail,
            Filter.VersionFilter versionFilter => Value.Value.Contains(
                versionFilter.VersionPattern, 
                StringComparison.OrdinalIgnoreCase)
                ? FilterResult.Pass : FilterResult.Fail,
            _ => FilterResult.Indeterminate // Default: no opinion
        };
    }
}
