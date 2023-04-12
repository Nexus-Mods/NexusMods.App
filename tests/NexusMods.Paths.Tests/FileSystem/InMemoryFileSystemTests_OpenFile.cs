using FluentAssertions;
using NexusMods.Paths.TestingHelpers;

namespace NexusMods.Paths.Tests.FileSystem;

// ReSharper disable once InconsistentNaming
public class InMemoryFileSystemTests_OpenFile
{
    [Theory, AutoFileSystem]
    public void Test_FileNotFound(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.ReadFile(path))
            .Should()
            .Throw<FileNotFoundException>();
    }

    [Theory, AutoFileSystem]
    public void Test_ArgumentException_FileModeCreate(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.OpenFile(path, FileMode.Create, FileAccess.Read, FileShare.None))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory, AutoFileSystem]
    public void Test_ArgumentException_FileModeCreateNew(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.OpenFile(path, FileMode.CreateNew, FileAccess.Read, FileShare.None))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory, AutoFileSystem]
    public void Test_ArgumentException_FileModeTruncate(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.OpenFile(path, FileMode.Truncate, FileAccess.Read, FileShare.None))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory, AutoFileSystem]
    public void Test_ArgumentException_FileModeAppend(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.OpenFile(path, FileMode.Append, FileAccess.Read, FileShare.None))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory, AutoFileSystem]
    public async Task Test_Open(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        fs.AddEmptyFile(path);

        await OpenWrite(fs, path, FileMode.Open, contents);
        await VerifyContents(fs, path, contents);
    }

    [Theory, AutoFileSystem]
    public async Task OpenOrCreate(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        fs.FileExists(path).Should().BeFalse();
        await OpenWrite(fs, path, FileMode.OpenOrCreate, contents);
        await VerifyContents(fs, path, contents);
    }

    [Theory, AutoFileSystem]
    public async Task OpenOrCreate_Existing(InMemoryFileSystem fs,
        AbsolutePath path, byte[] contents1, byte[] contents2)
    {
        fs.AddFile(path, contents1);
        fs.FileExists(path).Should().BeTrue();
        await VerifyContents(fs, path, contents1);

        await OpenWrite(fs, path, FileMode.OpenOrCreate, contents2);
        await VerifyContents(fs, path, contents2);
    }

    [Theory, AutoFileSystem]
    public async Task Test_Create(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        fs.FileExists(path).Should().BeFalse();

        await OpenWrite(fs, path, FileMode.Create, contents);
        await VerifyContents(fs, path, contents);
    }

    [Theory, AutoFileSystem]
    public async Task Test_Create_Overwrite(InMemoryFileSystem fs, AbsolutePath path, byte[] contents1, byte[] contents2)
    {
        fs.AddFile(path, contents1);
        fs.FileExists(path).Should().BeTrue();
        await VerifyContents(fs, path, contents1);

        await OpenWrite(fs, path, FileMode.Create, contents2);
        await VerifyContents(fs, path, contents2);
    }

    [Theory, AutoFileSystem]
    public async Task Test_CreateNew(InMemoryFileSystem fs, AbsolutePath path, byte[] contents)
    {
        fs.FileExists(path).Should().BeFalse();
        await OpenWrite(fs, path, FileMode.CreateNew, contents);
        await VerifyContents(fs, path, contents);
    }

    [Theory, AutoFileSystem]
    public void Test_CreateNew_Existing(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.AddEmptyFile(path);
        fs.FileExists(path).Should().BeTrue();
        fs.Invoking(x => x.OpenFile(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            .Should()
            .Throw<IOException>();
    }

    [Theory, AutoFileSystem]
    public async Task Test_Truncate(InMemoryFileSystem fs, AbsolutePath path, byte[] contents1, byte[] contents2)
    {
        fs.AddFile(path, contents1);
        fs.FileExists(path).Should().BeTrue();
        await VerifyContents(fs, path, contents1);

        await OpenWrite(fs, path, FileMode.Truncate, contents2);
        await VerifyContents(fs, path, contents2);
    }

    [Theory, AutoFileSystem]
    public void Test_Append(InMemoryFileSystem fs, AbsolutePath path)
    {
        fs.Invoking(x => x.OpenFile(path, FileMode.Append, FileAccess.Write, FileShare.None))
            .Should()
            .Throw<NotImplementedException>();
    }

    private static Stream Open(IFileSystem fs, AbsolutePath path, FileMode mode, FileAccess access)
    {
        return fs.OpenFile(path, mode, access, FileShare.None);
    }

    private static async Task OpenWrite(IFileSystem fs, AbsolutePath path, FileMode mode, byte[] contents)
    {
        await using var stream = Open(fs, path, mode, FileAccess.Write);
        await stream.WriteAsync(contents);
    }

    private static async Task VerifyContents(IFileSystem fileSystem, AbsolutePath path, byte[] expected)
    {
        var actual = await fileSystem.ReadAllBytesAsync(path);
        actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }
}
