using System.Text;
using FluentAssertions;
using NexusMods.Paths.Tests.AutoData;

namespace NexusMods.Paths.Tests.FileSystem;

public class InMemoryFileSystemTests
{
    [Theory, AutoFileSystem]
    public async Task Test_OpenFile(InMemoryFileSystem fs, AbsolutePath path, string contents)
    {
        var bytes = Encoding.UTF8.GetBytes(contents);

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

    [Theory, AutoFileSystem]
    public void Test_OpenFile_FileNotFound(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.ReadFile(path))
            .Should()
            .Throw<FileNotFoundException>();
    }

    [Theory, AutoFileSystem]
    public void Test_CreateDirectory(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.CreateDirectory(path);
        fs.DirectoryExists(path).Should().BeTrue();
        path.DirectoryExists().Should().BeTrue();
    }

    [Theory, AutoFileSystem]
    public void Test_DirectoryExists(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.DirectoryExists(path).Should().BeFalse();
        path.DirectoryExists().Should().BeFalse();
    }
}
