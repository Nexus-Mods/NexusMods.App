using System.Text;
using AutoFixture.Xunit2;
using FluentAssertions;

namespace NexusMods.Paths.Tests;

public class FileSystemTests
{
    [Fact]
    public void Test_FromFullPath_SameReference()
    {
        var fs = FileSystem.Shared;
        var a = fs.FromFullPath("/home");
        var b = AbsolutePath.FromFullPath("/home");
        a.FileSystem.Should().BeSameAs(fs);
        b.FileSystem.Should().BeSameAs(fs);
        a.Should().Be(b);
    }

    [Fact]
    public void Test_FromDirectoryAndFileName_SameReference()
    {
        var fs = FileSystem.Shared;
        var a = fs.FromDirectoryAndFileName("/", "home");
        var b = AbsolutePath.FromDirectoryAndFileName("/", "home");
        a.FileSystem.Should().BeSameAs(fs);
        b.FileSystem.Should().BeSameAs(fs);
        a.Should().Be(b);
    }

    [Theory, AutoData]
    public async Task Test_InMemoryFileSystem_OpenFile(string fileName, string contents)
    {
        var bytes = Encoding.UTF8.GetBytes(contents);

        var fs = new InMemoryFileSystem();
        var path = fs.FromFullPath($"/foo/{fileName}");

        fs.AddFile(path, bytes);

        await using var stream = fs.ReadFile(path);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var actualBytes = new byte[ms.Length];
        ms.Position = 0;

        var count = await ms.ReadAsync(actualBytes);
        count.Should().Be(bytes.Length);

        actualBytes.Should().BeEquivalentTo(bytes);
    }
}
