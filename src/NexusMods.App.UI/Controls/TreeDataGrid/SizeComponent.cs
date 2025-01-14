using Humanizer;
using Humanizer.Bytes;
using JetBrains.Annotations;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Component for <see cref="Size"/>.
/// </summary>
[PublicAPI]
public sealed class SizeComponent : AFormattedValueComponent<Size>, IItemModelComponent<SizeComponent>, IComparable<SizeComponent>
{
    /// <inheritdoc/>
    public int CompareTo(SizeComponent? other) => Value.Value.CompareTo(other?.Value.Value ?? Size.Zero);
    private static string _FormatValue(Size value) => ByteSize.FromBytes(value.Value).Humanize();
    /// <inheritdoc/>
    protected override string FormatValue(Size value) => _FormatValue(value);

    /// <inheritdoc/>
    public SizeComponent(
        Size initialValue,
        IObservable<Size> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, _FormatValue(initialValue), valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public SizeComponent(
        Size initialValue,
        Observable<Size> valueObservable,
        bool subscribeWhenCreated = false) : base(initialValue, _FormatValue(initialValue), valueObservable, subscribeWhenCreated) { }

    /// <inheritdoc/>
    public SizeComponent(Size value) : base(value, _FormatValue(value)) { }
}
