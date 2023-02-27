using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Tests.New.RelativePathTests;

/// <summary>
/// Tests related to joining of paths.
/// </summary>
public class CombinePathsTests
{
    [Fact]
    public void CanJoinPaths_WithBackslash()
    {
        var pathA = @"foo\bar\baz\quz".ToRelativePath();
        var pathB = @"foo".ToRelativePath();
        var pathC = @"bar\baz\quz".ToRelativePath();
        var pathD = @"quz".ToRelativePath();
        Assert.Equal(pathA, pathB.Join(pathC));
        Assert.Equal(pathA, pathB.Join(@"bar\baz").Join(pathD));
    }
    
    [Fact]
    public void CanJoinPaths_WithForwardSlash()
    {
        var pathA = @"foo/bar/baz/quz".ToRelativePath();
        var pathB = @"foo".ToRelativePath();
        var pathC = @"bar/baz/quz".ToRelativePath();
        var pathD = @"quz".ToRelativePath();
        Assert.Equal(pathA, pathB.Join(pathC));
        Assert.Equal(pathA, pathB.Join(@"bar/baz").Join(pathD));
    }
}