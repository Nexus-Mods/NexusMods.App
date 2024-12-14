using DynamicData.Kernel;
using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Represents a component holding a boolean value.
/// </summary>
[PublicAPI]
public sealed class BooleanComponent : AValueComponent<bool>
{
    public BooleanComponent(
        IObservable<bool> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<bool> initialValue = default) : base(defaultValue: false, valueObservable, subscribeWhenCreated, initialValue) { }

    public BooleanComponent(
        Observable<bool> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<bool> initialValue = default) : base(defaultValue: false, valueObservable, subscribeWhenCreated, initialValue) { }

    public BooleanComponent(bool value) : base(value) { }
}
