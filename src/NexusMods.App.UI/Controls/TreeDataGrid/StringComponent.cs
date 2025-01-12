using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public sealed class StringComponent : AValueComponent<string>, IItemModelComponent<StringComponent>, IComparable<StringComponent>
{
    public int CompareTo(StringComponent? other) => string.CompareOrdinal(Value.Value, other?.Value.Value);

    public StringComponent(
        string initialValue,
        IObservable<string> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    public StringComponent(
        string initialValue,
        Observable<string> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    public StringComponent(string value) : base(value) { }
}
