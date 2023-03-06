using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Paths.Tests.New.Helpers;

public static class TestStringHelpers
{
    private static Random _random = new();

    /// <summary>
    /// Normalizes the separator character from '/' used in test parameters to platform specific.
    /// </summary>
    [return: NotNullIfNotNull(nameof(original))]
    public static string? NormalizeSeparator(this string? original)
    {
        return original?.Replace('/', Path.DirectorySeparatorChar);
    }

    public static string RandomString(int length, string charSet)
    {
        return new string(Enumerable.Repeat(charSet, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static string RandomStringUpper(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    public static string RandomStringLower(int length) => RandomString(length, "abcdefghijklmnopqrstuvwxyz");

    public static string RandomStringUpperWithEmoji(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");
    public static string RandomStringLowerWithEmoji(int length) => RandomString(length, "abcdefghijklmnopqrstuvwxyz⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");

}
