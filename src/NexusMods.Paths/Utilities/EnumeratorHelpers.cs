using System.IO.Enumeration;
using System.Runtime.CompilerServices;

namespace NexusMods.Paths.Utilities.Internal.Enumerators;

internal class Common
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
    {
        switch (options.MatchType)
        {
            case MatchType.Win32:
                return FileSystemName.MatchesWin32Expression(expression.AsSpan(), name);
            case MatchType.Simple:
                return FileSystemName.MatchesSimpleExpression(expression.AsSpan(), name);
            default:
                ThrowHelpers.ArgumentOutOfRange(nameof(options));
                return false;
        }
    }
}
