namespace NexusMods.Paths.Tests.New.RelativePathTests;

public class HashingTests
{
    [Theory]
    [InlineData("/home/sewer56/nya.png", "/Home/Sewer56/Nya.png")]
    public void HashIsCaseInsensitive(string expected, string input) => AssertHashEquals(expected, input);

    [Theory]
    [InlineData(@"/home/sewer56/nya.png", @"\home\sewer56\nya.png")]
    [InlineData(@"c:/users/sewer/nya.png", @"C:\Users\Sewer\Nya.png")]
    public void HashIsSeparatorInsensitive(string expected, string input) => AssertHashEquals(expected, input);

    private static void AssertHashEquals(string expected, string input)
    {
        var expectedPath = new RelativePath(expected);
        var inputPath = new RelativePath(input);
        Assert.Equal(expectedPath.GetHashCode(), inputPath.GetHashCode());
    }
}