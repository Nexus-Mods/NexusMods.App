namespace NexusMods.FileExtractor;

internal static class PathsHelper
{
    internal static bool IsInvalidChar(char c) => c is '.' or ' ';

    /// <summary>
    /// Fixes a file name from the archive to work on all platforms properly and consistently.
    /// </summary>
    internal static string FixFileName(ReadOnlySpan<char> input)
    {
        const string charsToTrim = " .";
        var output = input.TrimEnd(charsToTrim);
        return output.ToString();
    }
}
