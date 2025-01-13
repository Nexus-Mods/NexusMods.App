using DynamicData.Kernel;
using Humanizer;
using Humanizer.Bytes;
using JetBrains.Annotations;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public static class SharedComponents
{
    private const string Prefix = "SharedComponent_";

    public sealed class Name : AValueComponent<string>, IItemModelComponent<Name>, IComparable<Name>
    {
        public static ComponentKey GetKey() => ComponentKey.From(Prefix + "Name");
        public int CompareTo(Name? other) => string.CompareOrdinal(Value.Value, other?.Value.Value);

        public Name(
            string defaultValue,
            IObservable<string> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<string> initialValue = default) : base(defaultValue, valueObservable, subscribeWhenCreated, initialValue) { }

        public Name(
            string defaultValue,
            Observable<string> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<string> initialValue = default) : base(defaultValue, valueObservable, subscribeWhenCreated, initialValue) { }

        public Name(string value) : base(value) { }
    }

    public sealed class FileSize : AFormattedValueComponent<Size>, IItemModelComponent<FileSize>, IComparable<FileSize>
    {
        public FileSize(
            Size defaultValue,
            IObservable<Size> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<Size> initialValue = default) : base(defaultValue, valueObservable, subscribeWhenCreated, initialValue, initialValue.Convert(_FormatValue)) { }

        public FileSize(
            Size defaultValue,
            Observable<Size> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<Size> initialValue = default) : base(defaultValue, valueObservable, subscribeWhenCreated, initialValue, initialValue.Convert(_FormatValue)) { }

        public FileSize(Size value) : base(value, _FormatValue(value)) { }

        public static ComponentKey GetKey() => ComponentKey.From(Prefix + "Size");
        public int CompareTo(FileSize? other) => Value.Value.CompareTo(other?.Value.Value ?? Size.Zero);

        private static string _FormatValue(Size value) => ByteSize.FromBytes(value.Value).Humanize();
        protected override string FormatValue(Size value) => _FormatValue(value);
    }

    public sealed class DownloadedDate : DateComponent, IItemModelComponent<DownloadedDate>, IComparable<DownloadedDate>
    {
        public DownloadedDate(
            IObservable<DateTimeOffset> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<DateTimeOffset> initialValue = default) : base(valueObservable, subscribeWhenCreated, initialValue) { }

        public DownloadedDate(
            Observable<DateTimeOffset> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<DateTimeOffset> initialValue = default) : base(valueObservable, subscribeWhenCreated, initialValue) { }

        public DownloadedDate(DateTimeOffset value) : base(value) { }

        public static ComponentKey GetKey() => ComponentKey.From(Prefix + "DownloadedDate");
        public int CompareTo(DownloadedDate? other) => Value.Value.CompareTo(other?.Value.Value ?? DateTimeOffset.UnixEpoch);
    }

    public sealed class InstalledDate : DateComponent, IItemModelComponent<InstalledDate>, IComparable<InstalledDate>
    {
        public InstalledDate(
            IObservable<DateTimeOffset> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<DateTimeOffset> initialValue = default) : base(valueObservable, subscribeWhenCreated, initialValue) { }

        public InstalledDate(
            Observable<DateTimeOffset> valueObservable,
            bool subscribeWhenCreated = false,
            Optional<DateTimeOffset> initialValue = default) : base(valueObservable, subscribeWhenCreated, initialValue) { }

        public InstalledDate(DateTimeOffset value) : base(value) { }

        public static ComponentKey GetKey() => ComponentKey.From(Prefix + "InstalledDate");
        public int CompareTo(InstalledDate? other) => Value.Value.CompareTo(other?.Value.Value ?? DateTimeOffset.UnixEpoch);
    }
}
