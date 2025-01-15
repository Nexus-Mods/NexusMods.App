using JetBrains.Annotations;
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
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public StringComponent(
        string initialValue,
        Observable<string> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public StringComponent(string value) : base(value) { }
}
