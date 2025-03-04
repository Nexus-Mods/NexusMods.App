using Humanizer;
using Humanizer.Bytes;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Converters;

/// <summary>
/// NexusMods.Paths.Size To String Type Converter.
/// </summary>
public class SizeToStringTypeConverter : IBindingTypeConverter
{
    /// <inheritdoc/>
    public int GetAffinityForObjects(Type fromType, Type toType)
    {
        if (fromType == typeof(Size) && toType == typeof(string))
        {
            return 999;
        }

        return 0;
    }

    /// <inheritdoc/>
    public bool TryConvert(object? from, Type toType, object? conversionHint, out object result)
    {
        if (toType == typeof(string) && from is Size fromSize)
        {
            var byteSize = ByteSize.FromBytes(fromSize.Value);
            result = byteSize.Gigabytes < 1 ? byteSize.Humanize("0") : byteSize.Humanize("0.0");

            return true;
        }
        
        result = null!;
        return false;
    }
}
