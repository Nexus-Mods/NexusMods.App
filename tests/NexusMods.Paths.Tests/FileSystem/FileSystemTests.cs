using System.Reflection;
using FluentAssertions;

namespace NexusMods.Paths.Tests.FileSystem;

public class FileSystemTests
{
    [Fact]
    public void Test_EnumerateFiles()
    {
        var fs = new Paths.FileSystem();

        var file = fs.FromFullPath(Assembly.GetExecutingAssembly().Location);
        var directory = fs.FromFullPath(AppContext.BaseDirectory);
        fs.EnumerateFiles(directory, recursive: false)
            .Should()
            .Contain(file);
    }

    [Fact]
    public void Test_EnumerateDirectories()
    {
        var fs = new Paths.FileSystem();

        var directory = fs.FromFullPath(AppContext.BaseDirectory);
        var parentDirectory = directory.Parent;

        fs.EnumerateDirectories(parentDirectory, recursive: false)
            .Should()
            .Contain(directory);
    }

    [Fact]
    public void Test_EnumerateFileEntries()
    {
        var fs = new Paths.FileSystem();

        var file = fs.FromFullPath(Assembly.GetExecutingAssembly().Location);
        var directory = fs.FromFullPath(AppContext.BaseDirectory);

        fs.EnumerateFileEntries(directory, recursive: false)
            .Should()
            .Contain(x => x.Path == file);
    }
}
