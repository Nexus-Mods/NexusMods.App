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
    }

    [Theory, AutoFileSystem]
    public void Test_CreateDirectory_SubDirectory(InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string subDirectoryName)
    {
        var subDirectory = parentDirectory.CombineUnchecked(subDirectoryName);

        fs.CreateDirectory(subDirectory);

        fs.DirectoryExists(subDirectory).Should().BeTrue();
        fs.DirectoryExists(parentDirectory).Should().BeTrue();
    }

    [Theory, AutoFileSystem]
    public void Test_DirectoryExists(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.DirectoryExists(path).Should().BeFalse();
    }

    [Theory, AutoFileSystem]
    public void Test_FileExists(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.FileExists(path).Should().BeFalse();
        fs.AddEmptyFile(path);
        fs.FileExists(path).Should().BeTrue();
    }

    [Theory, AutoFileSystem]
    public void Test_DeleteFile(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.FileExists(path).Should().BeFalse();
        fs.AddEmptyFile(path);
        fs.FileExists(path).Should().BeTrue();
        fs.DeleteFile(path);
        fs.FileExists(path).Should().BeFalse();
    }

    [Theory, AutoFileSystem]
    public void Test_DeleteFile_NotExists(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.FileExists(path).Should().BeFalse();
        fs.Invoking(x => x.DeleteFile(path))
            .Should()
            .Throw<FileNotFoundException>();
    }

    [Theory, AutoFileSystem]
    public void Test_DeleteDirectory(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.DirectoryExists(path).Should().BeFalse();
        fs.CreateDirectory(path);
        fs.DirectoryExists(path).Should().BeTrue();
        fs.DeleteDirectory(path, false);
        fs.DirectoryExists(path).Should().BeFalse();
    }

    [Theory, AutoFileSystem]
    public void Test_DeleteDirectory_NotExist(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.DeleteDirectory(path, false))
            .Should()
            .Throw<DirectoryNotFoundException>();
    }

    [Theory, AutoFileSystem]
    public void Test_DeleteDirectory_Recursive(
        InMemoryFileSystem fs,
        AbsolutePath directory,
        string subDirectoryName,
        string fileName1,
        string fileName2)
    {
        fs.CreateDirectory(directory);

        var subDirectory = directory.CombineUnchecked(subDirectoryName);
        fs.CreateDirectory(subDirectory);

        var file1 = directory.CombineUnchecked(fileName1);
        var file2 = subDirectory.CombineUnchecked(fileName2);

        fs.AddEmptyFile(file1);
        fs.AddEmptyFile(file2);

        fs.DirectoryExists(directory).Should().BeTrue();
        fs.DirectoryExists(subDirectory).Should().BeTrue();
        fs.FileExists(file1).Should().BeTrue();
        fs.FileExists(file2).Should().BeTrue();

        fs.DeleteDirectory(directory, true);

        fs.DirectoryExists(directory).Should().BeFalse();
        fs.DirectoryExists(subDirectory).Should().BeFalse();
        fs.FileExists(file1).Should().BeFalse();
        fs.FileExists(file2).Should().BeFalse();
    }

    [Theory, AutoFileSystem]
    public void Test_DeleteDirectory_NotEmpty(
        InMemoryFileSystem fs,
        AbsolutePath directory,
        string fileName)
    {
        fs.CreateDirectory(directory);

        var file = directory.CombineUnchecked(fileName);
        fs.AddEmptyFile(file);

        fs.DirectoryExists(directory).Should().BeTrue();
        fs.FileExists(file).Should().BeTrue();

        fs.Invoking(x => x.DeleteDirectory(directory, false))
            .Should()
            .Throw<IOException>();
    }
}
