namespace NexusMods.FileExtractor;

internal static class PathsHelper
{
    private const string CharsToTrim = " .";

    internal static bool IsInvalidChar(char c) => c is '.' or ' ';

    internal static string FixDirectoryName(ReadOnlySpan<char> input)
    {
        const string directorySeparators = "/\\";
        var trimmed = input.TrimEnd(directorySeparators);
        return FixFileName(trimmed);
    }

    /// <summary>
    /// Fixes a file name from the archive to work on all platforms properly and consistently.
    /// </summary>
    internal static string FixFileName(ReadOnlySpan<char> input)
    {
        var output = input.TrimEnd(CharsToTrim);
        return output.ToString();
    }
}
