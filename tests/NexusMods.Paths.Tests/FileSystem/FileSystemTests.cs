using FluentAssertions;

namespace NexusMods.Paths.Tests.FileSystem;

public class FileSystemTests
{
    [Fact]
    public void Test_FromFullPath_SameReference()
    {
        var fs = Paths.FileSystem.Shared;
        var a = fs.FromFullPath("/home");
        var b = AbsolutePath.FromFullPath("/home");
        a.FileSystem.Should().BeSameAs(fs);
        b.FileSystem.Should().BeSameAs(fs);
        a.Should().Be(b);
    }

    [Fact]
    public void Test_FromDirectoryAndFileName_SameReference()
    {
        var fs = Paths.FileSystem.Shared;
        var a = fs.FromDirectoryAndFileName("/", "home");
        var b = AbsolutePath.FromDirectoryAndFileName("/", "home");
        a.FileSystem.Should().BeSameAs(fs);
        b.FileSystem.Should().BeSameAs(fs);
        a.Should().Be(b);
    }
}
