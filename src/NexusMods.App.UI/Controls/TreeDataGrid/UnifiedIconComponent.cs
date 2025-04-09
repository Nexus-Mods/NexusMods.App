using JetBrains.Annotations;
using NexusMods.Icons;
using R3;
namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for a value assigned to a <see cref="UnifiedIcon"/> via the <see cref="IconValue"/>.
/// </summary>
[PublicAPI]
public sealed class UnifiedIconComponent : AValueComponent<IconValue>, IItemModelComponent<UnifiedIconComponent>, IComparable<UnifiedIconComponent>
{
    /// <inheritdoc/>
    public int CompareTo(UnifiedIconComponent? other) => other is null ? 1 : 0;

    /// <inheritdoc/>
    public UnifiedIconComponent(
        IconValue initialValue,
        IObservable<IconValue> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public UnifiedIconComponent(
        IconValue initialValue,
        Observable<IconValue> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public UnifiedIconComponent(IconValue value) : base(value) { }
}
