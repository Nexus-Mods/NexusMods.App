using System.Text;
using FluentAssertions;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Paths.Tests.FileSystem;

public class InMemoryFileSystemTests
{
    [Theory, AutoFileSystem]
    public void Test_GetFileEntry(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.AddEmptyFile(path);
        var entry = fs.GetFileEntry(path);
        entry.Path.Should().Be(path);
    }

    [Theory, AutoFileSystem]
    public void Test_GetFileEntry_MissingFile(InMemoryFileSystem fs, AbsolutePath path)
    {
        var entry = fs.GetFileEntry(path);
        entry.Path.Should().Be(path);
    }

    [Theory, AutoFileSystem]
    public void Test_EnumerateFiles(
        InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string fileName1,
        string fileName2,
        string subDirectoryName1)
    {
        var subDirectory1 = parentDirectory.CombineUnchecked(subDirectoryName1);

        var file1 = parentDirectory.CombineUnchecked(fileName1);
        var file2 = subDirectory1.CombineUnchecked(fileName2);

        fs.AddDirectory(parentDirectory);
        fs.AddDirectory(subDirectory1);
        fs.AddEmptyFile(file1);
        fs.AddEmptyFile(file2);

        fs.EnumerateFiles(parentDirectory, recursive: false)
            .Should()
            .OnlyContain(path => path == file1);
    }

    [Theory, AutoFileSystem]
    public void Test_EnumerateFiles_Recursive(
        InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string fileName1,
        string fileName2,
        string fileName3,
        string subDirectoryName1,
        string subDirectoryName2)
    {
        var subDirectory1 = parentDirectory.CombineUnchecked(subDirectoryName1);
        var subDirectory2 = parentDirectory.CombineUnchecked(subDirectoryName2);

        var file1 = parentDirectory.CombineUnchecked(fileName1);
        var file2 = subDirectory1.CombineUnchecked(fileName2);
        var file3 = subDirectory2.CombineUnchecked(fileName3);

        fs.AddDirectory(parentDirectory);
        fs.AddDirectory(subDirectory1);
        fs.AddDirectory(subDirectory2);
        fs.AddEmptyFile(file1);
        fs.AddEmptyFile(file2);
        fs.AddEmptyFile(file3);

        fs.EnumerateFiles(parentDirectory)
            .Should()
            .OnlyContain(path => path == file1 || path == file2 || path == file3);
    }

    [Theory, AutoFileSystem]
    public void Test_EnumerateDirectories(
        InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string subDirectoryName1,
        string subDirectoryName2,
        string subDirectoryName3)
    {
        var subDirectory1 = parentDirectory.CombineUnchecked(subDirectoryName1);
        var subDirectory2 = parentDirectory.CombineUnchecked(subDirectoryName2);
        var subDirectory3 = subDirectory1.CombineUnchecked(subDirectoryName3);

        fs.AddDirectory(parentDirectory);
        fs.AddDirectory(subDirectory1);
        fs.AddDirectory(subDirectory2);
        fs.AddDirectory(subDirectory3);

        fs.EnumerateDirectories(parentDirectory, recursive:false)
            .Should()
            .OnlyContain(path => path == subDirectory1 || path == subDirectory2);
    }

    [Theory, AutoFileSystem]
    public void Test_EnumerateDirectories_Recursive(
        InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string subDirectoryName1,
        string subDirectoryName2,
        string subDirectoryName3)
    {
        var subDirectory1 = parentDirectory.CombineUnchecked(subDirectoryName1);
        var subDirectory2 = parentDirectory.CombineUnchecked(subDirectoryName2);
        var subDirectory3 = subDirectory1.CombineUnchecked(subDirectoryName3);

        fs.AddDirectory(parentDirectory);
        fs.AddDirectory(subDirectory1);
        fs.AddDirectory(subDirectory2);
        fs.AddDirectory(subDirectory3);

        fs.EnumerateDirectories(parentDirectory)
            .Should()
            .OnlyContain(path => path == subDirectory1 || path == subDirectory2 || path == subDirectory3);
    }

    [Theory, AutoFileSystem]
    public void Test_EnumerateFilesEntries(
        InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string fileName1,
        string fileName2,
        string subDirectoryName1)
    {
        var subDirectory1 = parentDirectory.CombineUnchecked(subDirectoryName1);

        var file1 = parentDirectory.CombineUnchecked(fileName1);
        var file2 = subDirectory1.CombineUnchecked(fileName2);

        fs.AddDirectory(parentDirectory);
        fs.AddDirectory(subDirectory1);
        fs.AddEmptyFile(file1);
        fs.AddEmptyFile(file2);

        fs.EnumerateFileEntries(parentDirectory, recursive: false)
            .Should()
            .OnlyContain(x => x.Path == file1);
    }

    [Theory, AutoFileSystem]
    public void Test_EnumerateFilesEntries_Recursive(
        InMemoryFileSystem fs,
        AbsolutePath parentDirectory,
        string fileName1,
        string fileName2,
        string fileName3,
        string subDirectoryName1,
        string subDirectoryName2)
    {
        var subDirectory1 = parentDirectory.CombineUnchecked(subDirectoryName1);
        var subDirectory2 = parentDirectory.CombineUnchecked(subDirectoryName2);

        var file1 = parentDirectory.CombineUnchecked(fileName1);
        var file2 = subDirectory1.CombineUnchecked(fileName2);
        var file3 = subDirectory2.CombineUnchecked(fileName3);

        fs.AddDirectory(parentDirectory);
        fs.AddDirectory(subDirectory1);
        fs.AddDirectory(subDirectory2);
        fs.AddEmptyFile(file1);
        fs.AddEmptyFile(file2);
        fs.AddEmptyFile(file3);

        fs.EnumerateFileEntries(parentDirectory)
            .Should()
            .OnlyContain(x => x.Path == file1 || x.Path == file2 || x.Path == file3);
    }

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

    [Theory, AutoFileSystem]
    public void Test_MoveFile_NoOverwrite(InMemoryFileSystem fs, AbsolutePath source, AbsolutePath dest, byte[] contents)
    {
        fs.FileExists(source).Should().BeFalse();
        fs.FileExists(dest).Should().BeFalse();

        fs.AddFile(source, contents);
        fs.FileExists(source).Should().BeTrue();

        fs.MoveFile(source, dest, false);
        fs.FileExists(source).Should().BeFalse();
        fs.FileExists(dest).Should().BeTrue();

        fs.GetFileEntry(dest).Size.Should().Be(Size.FromLong(contents.Length));
    }

    [Theory, AutoFileSystem]
    public void Test_MoveFile_NoOverwriteExists(InMemoryFileSystem fs,
        AbsolutePath source, AbsolutePath dest, byte[] contents)
    {
        fs.FileExists(source).Should().BeFalse();
        fs.FileExists(dest).Should().BeFalse();

        fs.AddFile(source, contents);
        fs.AddEmptyFile(dest);

        fs.FileExists(source).Should().BeTrue();
        fs.FileExists(dest).Should().BeTrue();

        fs.Invoking(x => x.MoveFile(source, dest, false))
            .Should()
            .Throw<IOException>();
    }

    [Theory, AutoFileSystem]
    public void Test_MoveFile_Overwrite(InMemoryFileSystem fs,
        AbsolutePath source, AbsolutePath dest, byte[] contents)
    {
        fs.FileExists(source).Should().BeFalse();
        fs.FileExists(dest).Should().BeFalse();

        fs.AddFile(source, contents);
        fs.AddEmptyFile(dest);

        fs.FileExists(source).Should().BeTrue();
        fs.FileExists(dest).Should().BeTrue();

        fs.MoveFile(source, dest, true);
        fs.FileExists(source).Should().BeFalse();
        fs.FileExists(dest).Should().BeTrue();

        fs.GetFileEntry(dest).Size.Should().Be(Size.FromLong(contents.Length));
    }
}
