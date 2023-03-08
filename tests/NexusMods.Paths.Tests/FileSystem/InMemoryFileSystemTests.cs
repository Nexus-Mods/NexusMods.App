using System.Text;
using AutoFixture.Xunit2;
using FluentAssertions;

namespace NexusMods.Paths.Tests.FileSystem;

public class InMemoryFileSystemTests
{
    [Theory, AutoData]
    public async Task Test_OpenFile(string fileName, string contents)
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

    [Theory, AutoData]
    public void Test_OpenFile_FileNotFound(string fileName)
    {
        var fs = new InMemoryFileSystem();

        fs.Invoking(x => x.ReadFile(fs.FromFullPath(fileName)))
            .Should()
            .Throw<FileNotFoundException>();
    }
}
