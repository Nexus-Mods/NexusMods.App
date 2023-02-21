using NexusMods.Paths.HighPerformance.Backports.System.Globalization;

namespace NexusMods.Paths.Tests.New.Conversions;

/// <summary>
/// Tests for .NET 8 backported code to change case.
/// </summary>
public class VectorisedChangeCase
{
    private static Random _random = new Random();
    
    [Fact]
    public void Vectorised_ToUpper() => Vectorised_ToUpper_Common(RandomStringLower);

    /// <summary>
    /// Tests non-ASCII fallback.
    /// </summary>
    [Fact]
    public void Vectorised_ToUpper_WithEmoji() => Vectorised_ToUpper_Common(RandomStringLowerWithEmoji);

    [Fact]
    public void Vectorised_ToLower() => Vectorised_ToLower_Common(RandomStringUpper);
    
    /// <summary>
    /// Tests non-ASCII fallback.
    /// </summary>
    [Fact]
    public void Vectorised_ToLower_WithEmoji() => Vectorised_ToLower_Common(RandomStringUpperWithEmoji);

    private void Vectorised_ToLower_Common(Func<int, string> getStringWithLength)
    {
        // Note: We run this over such a big value range so this hits all implementations on a supported processor
        const int maxChars = 257;
        Span<char> actualBuf = stackalloc char[maxChars];
        for (int x = 0; x < maxChars; x++)
        {
            var text = getStringWithLength(x);
            var expected = text.ToLowerInvariant();
            TextInfo.ChangeCase<TextInfo.ToLowerConversion>(text, actualBuf);
            var actual = actualBuf[..expected.Length];
            Assert.True(expected.AsSpan().Equals(actual, StringComparison.Ordinal));
        }
    }
    
    private static void Vectorised_ToUpper_Common(Func<int, string> getStringWithLength)
    {
        // Note: We run this over such a big value range so this hits all implementations on a supported processor
        const int maxChars = 257;
        Span<char> actualBuf = stackalloc char[maxChars];
        for (int x = 0; x < maxChars; x++)
        {
            var text = getStringWithLength(x);
            var expected = text.ToUpperInvariant();
            TextInfo.ChangeCase<TextInfo.ToUpperConversion>(text, actualBuf);
            var actual = actualBuf[..expected.Length];
            Assert.True(expected.AsSpan().Equals(actual, StringComparison.Ordinal));
        }
    }

    private static string RandomString(int length, string charSet)
    {
        return new string(Enumerable.Repeat(charSet, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
    
    private static string RandomStringUpper(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ");
    private static string RandomStringLower(int length) => RandomString(length, "abcdefghijklmnopqrstuvwxyz");
    
    private static string RandomStringUpperWithEmoji(int length) => RandomString(length, "ABCDEFGHIJKLMNOPQRSTUVWXYZ⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");
    private static string RandomStringLowerWithEmoji(int length) => RandomString(length, "abcdefghijklmnopqrstuvwxyz⚠️🚦🔺🏒😕🏞🖌🖕🌷☠⛩🍸👳🍠🚦📟💦🚏🌥🏪🌖😱");
}