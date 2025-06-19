using System.Diagnostics;
using JetBrains.Annotations;

namespace NexusMods.Sdk;

/// <summary>
/// Extension methods for <see cref="Version"/>.
/// </summary>
[PublicAPI]
public static class VersionExtensions
{
    /// <summary>
    /// Non-throw ToString alternative for <see cref="Version"/> that supports a <paramref name="maxFieldCount"/>.
    /// </summary>
    /// <returns>The string representation of the version with up to <paramref name="maxFieldCount"/> fields</returns>
    public static string ToSafeString(this Version version, int maxFieldCount)
    {
        Debug.Assert(maxFieldCount is > 0 and < 5);

        var fieldCount = maxFieldCount;
        if (fieldCount >= 3 && version.Build == -1)
            fieldCount = 2;
        if (fieldCount == 4 && version.Revision == -1)
            fieldCount = 3;

        return version.ToString(fieldCount);
    }
}
