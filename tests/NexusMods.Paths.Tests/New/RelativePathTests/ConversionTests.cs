using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests.New.RelativePathTests;

public class ConversionTests
{
    [Fact]
    public void CanConvertWithExplicitConversions()
    {
        Assert.Equal("foo", (string)"foo".ToRelativePath());
        Assert.Equal((RelativePath)"foo", "foo".ToRelativePath());
    }
    
    [Fact]
    public void ObjectMethods()
    {
        Assert.Equal(@"foo\bar", ((RelativePath)@"foo\bar").ToString());

        Assert.Equal((RelativePath)@"foo\bar", (RelativePath)@"foo/bar");
        Assert.NotEqual((RelativePath)@"foo\bar", (object)42);
        Assert.True((RelativePath)@"foo\bar" == (RelativePath)@"foo/bar");
        Assert.True((RelativePath)@"foo\bar" != (RelativePath)@"foo/baz");

        Assert.Equal(((RelativePath)@"foo\bar").GetHashCode(), ((RelativePath)@"Foo\bar").GetHashCode());
    }
    
    [Fact]
    public void CanGetFilenameFromRelativePath()
    {
        Assert.Equal((RelativePath)"bar.dds", @"foo\bar.dds".ToRelativePath().FileName);
    }
}