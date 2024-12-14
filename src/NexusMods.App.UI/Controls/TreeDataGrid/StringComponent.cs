using DynamicData.Kernel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Represents a component holding a boolean value.
/// </summary>
[PublicAPI]
public sealed class StringComponent : AValueComponent<string>
{
    public StringComponent(
        IObservable<string> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<string> initialValue = default) : base(defaultValue: string.Empty, valueObservable, subscribeWhenCreated, initialValue) { }

    public StringComponent(
        Observable<string> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<string> initialValue = default) : base(defaultValue: string.Empty, valueObservable, subscribeWhenCreated, initialValue) { }

    public StringComponent(string value) : base(value) { }
}
