using DynamicData.Kernel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public sealed class StringComponent : AValueComponent<string>, IItemModelComponent<StringComponent>, IComparable<StringComponent>
{
    public int CompareTo(StringComponent? other) => string.CompareOrdinal(Value.Value, other?.Value.Value);

    public StringComponent(
        string defaultValue,
        IObservable<string> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<string> initialValue = default) : base(defaultValue, valueObservable, subscribeWhenCreated, initialValue) { }

    public StringComponent(
        string defaultValue,
        Observable<string> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<string> initialValue = default) : base(defaultValue, valueObservable, subscribeWhenCreated, initialValue) { }

    public StringComponent(string value) : base(value) { }
}
