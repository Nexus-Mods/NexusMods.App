using System.IO.Enumeration;
using System.Runtime.CompilerServices;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal class Common
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
    {
        var ignoreCase = true;
        if (options.MatchType == MatchType.Win32)
            return FileSystemName.MatchesWin32Expression(expression.AsSpan(), name, ignoreCase);

        if (options.MatchType == MatchType.Simple)
            return FileSystemName.MatchesSimpleExpression(expression.AsSpan(), name, ignoreCase);

        ThrowHelpers.ArgumentOutOfRange(nameof(options));
        return false;
    }
}
