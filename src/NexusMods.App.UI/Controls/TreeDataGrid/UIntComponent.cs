using JetBrains.Annotations;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for <see cref="uint"/>.
/// </summary>
[PublicAPI]
public sealed class UIntComponent : AValueComponent<uint>, IItemModelComponent<UIntComponent>, IComparable<UIntComponent>
{
    /// <inheritdoc/>
    public int CompareTo(UIntComponent? other)
    {
        // Note(sewer): Prevent boxing in the case 'other' is null.
        return other is null 
            ? 1 
            : Value.Value.CompareTo(other.Value.Value);
    }

    /// <inheritdoc/>
    public UIntComponent(
        uint initialValue,
        IObservable<uint> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public UIntComponent(
        uint initialValue,
        Observable<uint> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public UIntComponent(uint value) : base(value) { }
}
