using DynamicData.Kernel;
using Humanizer;
using Humanizer.Bytes;
using JetBrains.Annotations;
using NexusMods.Abstractions.UI;
using NexusMods.Abstractions.UI.Extensions;
using NexusMods.App.UI.Extensions;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public sealed class SizeComponent : AFormattedValueComponent<Size>
{
    public SizeComponent(
        IObservable<Size> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<Size> initialValue = default,
        Optional<string> initialFormattedValue = default) : base(defaultValue: Size.Zero, valueObservable, subscribeWhenCreated, initialValue, initialFormattedValue) { }

    public SizeComponent(
        Observable<Size> valueObservable,
        bool subscribeWhenCreated = false,
        Optional<Size> initialValue = default,
        Optional<string> initialFormattedValue = default) : base(defaultValue: Size.Zero, valueObservable, subscribeWhenCreated, initialValue, initialFormattedValue) { }

    public SizeComponent(Size value) : base(value, _FormatValue(value)) { }

    private static string _FormatValue(Size value) => ByteSize.FromBytes(value.Value).Humanize();
    protected override string FormatValue(Size value) => _FormatValue(value);
}
