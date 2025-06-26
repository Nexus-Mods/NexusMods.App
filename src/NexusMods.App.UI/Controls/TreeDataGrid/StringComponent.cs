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
        // Default: no opinion on any filter, as we don't know what type of string we're dealing with from context.
        return FilterResult.Indeterminate;
    }
}
