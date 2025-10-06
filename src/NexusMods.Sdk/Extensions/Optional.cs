using System.Diagnostics.CodeAnalysis;
using DynamicData.Kernel;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

[PublicAPI]
public static class OptionalExtensions
{
    [SuppressMessage("ReSharper", "ConvertNullableToShortForm")]
    public static Nullable<T> OrNull<T>(this Optional<T> optional) where T : struct
    {
        if (optional.HasValue) return optional.Value;
        return null;
    }
}
