using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests.New.RelativePathTests;

/// <summary>
/// Contains all tests related to file extensions.
/// </summary>
public class ExtensionTests
{
    [Fact]
    public void CanReplaceExtensions()
    {
        Assert.Equal(new Extension(".dds"), ((RelativePath)@"foo\bar.dds").Extension);
        Assert.Equal(@"foo\bar.zip".ToRelativePath(), @"foo\bar.dds".ToRelativePath().ReplaceExtension(new Extension(".zip")));
        Assert.NotEqual(@"foo\bar\z.zip".ToRelativePath(), @"foo\bar.dds".ToRelativePath().ReplaceExtension(new Extension(".zip")));
        Assert.Equal(@"foo\bar.zip".ToRelativePath(), @"foo\bar".ToRelativePath().ReplaceExtension(new Extension(".zip")));
    }
    
    [Fact]
    public void CanAddExtension()
    {
        var pathA = @"foo\bar.zip".ToRelativePath();
        var pathB = @"foo\bar".ToRelativePath();
        Assert.Equal(pathA, pathB.WithExtension(KnownExtensions.Zip));
    }
}