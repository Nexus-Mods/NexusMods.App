using FluentAssertions;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Paths.Tests.FileSystem;

public class TestingHelpersTest
{
    [Theory, AutoFileSystem(useSharedFileSystem:true)]
    public void Test_UseSharedFileSystem_InMemoryFileSystem(InMemoryFileSystem fs1, InMemoryFileSystem fs2)
    {
        fs1.Should().BeSameAs(fs2);
    }

    [Theory, AutoFileSystem(useSharedFileSystem:false)]
    public void Test_DontUseSharedFileSystem_InMemoryFileSystem(InMemoryFileSystem fs1, InMemoryFileSystem fs2)
    {
        fs1.Should().NotBeSameAs(fs2);
    }

    [Theory, AutoFileSystem(useSharedFileSystem:true)]
    public void Test_UseSharedFileSystem_IFileSystem(IFileSystem fs1, IFileSystem fs2)
    {
        fs1.Should().BeSameAs(fs2);
    }

    [Theory, AutoFileSystem(useSharedFileSystem:false)]
    public void Test_DontUseSharedFileSystem_IFileSystem(IFileSystem fs1, IFileSystem fs2)
    {
        fs1.Should().NotBeSameAs(fs2);
    }

    [Theory, AutoFileSystem(useSharedFileSystem:true)]
    public void Test_UseSharedFileSystem_AbsolutePath(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.AddEmptyFile(path);
        fs.FileExists(path).Should().BeTrue();
        path.FileExists.Should().BeTrue();
    }

    [Theory, AutoFileSystem(useSharedFileSystem:false)]
    public void Test_DontUseSharedFileSystem_AbsolutePath(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.AddEmptyFile(path);
        fs.FileExists(path).Should().BeTrue();
        path.FileExists.Should().BeFalse();
    }
}
