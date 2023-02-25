using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Tests.New.Conversions;

/// <summary>
/// Tests normalizing the string for comparisons used in <see cref="RelativePath"/>.
/// </summary>
public class NormalizeString
{
    [Theory]
    [InlineData("/home/sewer/nya.png", "/home/Sewer/Nya.png")]
    [InlineData("/home/sewer/nya.png", @"\home\Sewer\Nya.png")]
    public unsafe void NormalizeBasicPaths(string expected, string value)
    {
        var copy = new string(value.AsSpan());
        
        // Please don't do this in production; unless string doesn't escape the method :)
        fixed (char* copyPtr = copy)
        {
            var copySpan = new Span<char>(copyPtr, value.Length);
            copySpan.NormalizeStringCaseAndPathSeparator();
            Assert.Equal(expected, copy);
        }
    }
}