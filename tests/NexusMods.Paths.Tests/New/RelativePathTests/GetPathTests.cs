using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Tests.New.RelativePathTests;

public class GetPathTests
{
    [Fact]
    public void CanGetPathsAsStrings()
    {
        var path = @"\foo\bar\baz".ToRelativePath();
        Assert.Equal(@"\foo\bar\baz", path.ToString());
        Assert.Equal("", new RelativePath("").ToString());
    }
}
