using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class AbsolutePathTests
{
    [Theory]
    [InlineData(true, "/", "/", "")]
    [InlineData(true, "/foo", "/", "foo")]
    [InlineData(true, "/foo/bar", "/foo", "bar")]
    [InlineData(false, "C:/", "C:/", "")]
    [InlineData(false, "C:/foo", "C:/", "foo")]
    [InlineData(false, "C:/foo/bar", "C:/foo", "bar")]
    public void Test_FromSanitizedFullPath(bool isUnix, string input, string expectedDirectory, string expectedFileName)
    {
        var os = CreateOSInformation(isUnix);
        var fs = new InMemoryFileSystem(os);
        var actualPath = AbsolutePath.FromSanitizedFullPath(input, fs);
        actualPath.Directory.Should().Be(expectedDirectory);
        actualPath.FileName.Should().Be(expectedFileName);
        actualPath.GetFullPath().Should().Be(input);
    }

    [Theory]
    [InlineData(true, "/", "", "/", "", "/")]
    [InlineData(true, "/", "foo", "/", "foo", "/foo")]
    [InlineData(true, "/foo", "bar", "/foo", "bar", "/foo/bar")]
    [InlineData(false, "C:\\", "", "C:/", "", "C:/")]
    [InlineData(false, "C:\\", "foo", "C:/", "foo", "C:/foo")]
    [InlineData(false, "C:\\foo", "bar", "C:/foo", "bar", "C:/foo/bar")]
    public void Test_FromUnsanitizedDirectoryAndFileName(
        bool isUnix,
        string directory, string fileName,
        string expectedDirectory, string expectedFileName, string expectedFullPath)
    {
        var os = CreateOSInformation(isUnix);
        var fs = new InMemoryFileSystem(os);
        var actualPath = AbsolutePath.FromUnsanitizedDirectoryAndFileName(directory, fileName, fs);
        actualPath.Directory.Should().Be(expectedDirectory);
        actualPath.FileName.Should().Be(expectedFileName);
        actualPath.GetFullPath().Should().Be(expectedFullPath);
    }

    [Theory]
    [InlineData("/", "")]
    [InlineData("/foo", "")]
    [InlineData("/foo.txt", ".txt")]
    public void Test_Extension(string input, string expectedExtension)
    {
        var path = CreatePath(input, isUnix: true);
        var actualExtension = path.Extension;
        actualExtension.ToString().Should().Be(expectedExtension);
    }

    [Theory]
    [InlineData(true, "/", "/", "", "/")]
    [InlineData(true, "/foo", "/", "", "/")]
    [InlineData(true, "/foo/bar", "/", "foo", "/foo")]
    [InlineData(false, "C:/", "C:/", "", "C:/")]
    [InlineData(false, "C:/foo", "C:/", "", "C:/")]
    [InlineData(false, "C:/foo/bar", "C:/", "foo", "C:/foo")]
    public void Test_Parent(bool isUnix, string input, string expectedDirectory, string expectedFileName, string expectedFullPath)
    {
        var path = CreatePath(input, isUnix);
        var actualParent = path.Parent;
        actualParent.Directory.Should().Be(expectedDirectory);
        actualParent.FileName.Should().Be(expectedFileName);
        actualParent.GetFullPath().Should().Be(expectedFullPath);
    }

    [Theory]
    [InlineData("/", "")]
    [InlineData("/foo", "foo")]
    [InlineData("/foo.txt", "foo")]
    public void Test_GetFileNameWithoutExtension(string input, string expectedFileName)
    {
        var path = CreatePath(input);
        var actualFileName = path.GetFileNameWithoutExtension();
        actualFileName.Should().Be(expectedFileName);
    }

    [Theory]
    [InlineData("/foo", ".txt", "/foo.txt")]
    public void Test_AppendExtension(string input, string extension, string expectedFullPath)
    {
        var path = CreatePath(input);
        var actualPath = path.AppendExtension(new Extension(extension));
        actualPath.GetFullPath().Should().Be(expectedFullPath);
    }

    [Theory]
    [InlineData("/foo.txt", ".md", "/foo.md")]
    public void Test_ReplaceExtension(string input, string extension, string expectedFullPath)
    {
        var path = CreatePath(input);
        var newPath = path.ReplaceExtension(new Extension(extension));
        newPath.GetFullPath().Should().Be(expectedFullPath);
    }

    [Theory]
    [InlineData(true, "/", "/")]
    [InlineData(true, "/foo/bar/baz", "/")]
    [InlineData(false, "C:/", "C:/")]
    [InlineData(false, "C:/foo/bar/baz", "C:/")]
    public void Test_GetRootDirectory(bool isUnix, string input, string expectedRootDirectory)
    {
        var path = CreatePath(input, isUnix);
        var actualRootDirectory = path.GetRootDirectory();
        actualRootDirectory.GetFullPath().Should().Be(expectedRootDirectory);
    }

    [Theory]
    [InlineData(true, "/foo", "/", "foo")]
    [InlineData(true, "/foo/bar/baz", "/", "foo/bar/baz")]
    [InlineData(true, "/foo/bar/baz", "/foo", "bar/baz")]
    [InlineData(false, "C:/foo", "C:/", "foo")]
    [InlineData(false, "C:/foo/bar/baz", "C:/", "foo/bar/baz")]
    [InlineData(false, "C:/foo/bar/baz", "C:/foo", "bar/baz")]
    public void Test_RelativeTo(bool isUnix, string child, string parent, string expectedOutput)
    {
        var childPath = CreatePath(child, isUnix);
        var parentPath = CreatePath(parent, isUnix);
        var actualOutput = childPath.RelativeTo(parentPath);
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData("/foo/bar/baz", "/f")]
    public void Test_RelativeTo_PathException(string child, string parent)
    {
        var childPath = CreatePath(child);
        var parentPath = CreatePath(parent);

        Action act = () => childPath.RelativeTo(parentPath);
        act.Should().ThrowExactly<PathException>();
    }

    [Theory]
    [InlineData(true, "", "", true)]
    [InlineData(true, "foo", "", true)]
    [InlineData(true, "", "foo", false)]
    [InlineData(true, "foo/bar", "foo", true)]
    [InlineData(true, "foo", "bar", false)]
    [InlineData(true, "/", "/", true)]
    [InlineData(true, "/foo", "/", true)]
    [InlineData(true, "/foo/bar/baz", "/", true)]
    [InlineData(true, "/foo/bar/baz", "/foo", true)]
    [InlineData(true, "/foo/bar/baz", "/foo/bar", true)]
    [InlineData(true, "/foobar", "/foo", false)]
    [InlineData(false, "C:/", "C:/", true)]
    [InlineData(false, "C:/foo", "C:/", true)]
    [InlineData(false, "C:/foo/bar/baz", "C:/", true)]
    [InlineData(false, "C:/foo/bar/baz", "C:/foo", true)]
    [InlineData(false, "C:/foo/bar/baz", "C:/foo/bar", true)]
    [InlineData(false, "C:/foobar", "C:/foo", false)]
    public void Test_InFolder(bool isUnix, string child, string parent, bool expected)
    {
        var childPath = CreatePath(child, isUnix);
        var parentPath = CreatePath(parent, isUnix);
        var actual = childPath.InFolder(parentPath);
        actual.Should().Be(expected);
    }

    private static AbsolutePath CreatePath(string input, bool isUnix = true)
    {
        var os = CreateOSInformation(isUnix);
        var fs = new InMemoryFileSystem(os);
        var path = AbsolutePath.FromSanitizedFullPath(input, fs);
        return path;
    }

    private static IOSInformation CreateOSInformation(bool isUnix)
    {
        return isUnix ? new OSInformation(OSPlatform.Linux) : new OSInformation(OSPlatform.Windows);
    }
}
