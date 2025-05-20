using R3;
namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component based on <see cref="uint"/>; for XAML binding convenience.
/// </summary>
public class UInt32Component : AValueComponent<uint>, IItemModelComponent<UInt32Component>, IComparable<UInt32Component>
{
    public UInt32Component(uint initialValue, IObservable<uint> valueObservable, bool subscribeWhenCreated = false, bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated,
        observeOutsideUiThread
    ) { }
    public UInt32Component(uint initialValue, Observable<uint> valueObservable, bool subscribeWhenCreated = false, bool observeOutsideUiThread = false) : base(initialValue, valueObservable, subscribeWhenCreated,
        observeOutsideUiThread
    ) { }
    public UInt32Component(uint value) : base(value) { }
    /// <inheritdoc />
    public int CompareTo(UInt32Component? other) => Value.Value.CompareTo(other?.Value.Value ?? 0);
}
