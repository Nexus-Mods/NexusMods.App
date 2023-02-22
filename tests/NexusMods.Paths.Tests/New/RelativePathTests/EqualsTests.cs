using NexusMods.Paths.New;

namespace NexusMods.Paths.Tests.New.RelativePathTests;

public class EqualsTests
{
    [Theory]
    [InlineData("/home/sewer56/nya.png", "/Home/Sewer56/Nya.png")]
    public void EqualsIsCaseInsensitive(string expected, string input) => AssertEquals(expected, input);

    [Theory]
    [InlineData(@"/home/sewer56/nya.png", @"\home\sewer56\nya.png")]
    [InlineData(@"c:/users/sewer/nya.png", @"C:\Users\Sewer\Nya.png")]
    public void EqualsIsSeparatorInsensitive(string expected, string input) => AssertEquals(expected, input);

    private static void AssertEquals(string expected, string input)
    {
        var expectedPath = new RelativePath2(expected);
        var inputPath = new RelativePath2(input);
        Assert.Equal(expectedPath, inputPath);
    }
}