namespace NexusMods.Backend.FileExtractor;

internal static class PathsHelper
{
    private const string CharsToTrim = " .";

    internal static bool IsInvalidChar(char c) => c is '.' or ' ';
    internal static bool IsDirectorySeparator(char c) => c is '/' or '\\';

    /// <summary>
    /// Removes all invalid characters from all parts of the path.
    /// </summary>
    internal static string FixPath(ReadOnlySpan<char> input)
    {
        const string directorySeparators = "/\\";

        Span<char> result = stackalloc char[input.Length];
        var resultIndex = 0;

        var shouldCheck = true;
        for (var inputIndex = input.Length - 1; inputIndex >= 0; inputIndex--)
        {
            var current = input[inputIndex];
            if (IsDirectorySeparator(current))
            {
                shouldCheck = true;
                result[resultIndex++] = current;
                continue;
            }

            var isInvalidChar = IsInvalidChar(current);
            if (!isInvalidChar) shouldCheck = false;
            else if (shouldCheck) continue;

            result[resultIndex++] = current;
        }

        var resultSlice = result[..resultIndex];
        resultSlice.Reverse();

        var trimmed = resultSlice.TrimEnd(directorySeparators);
        return trimmed.Length == 0 ? string.Empty : trimmed.ToString();
    }

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
