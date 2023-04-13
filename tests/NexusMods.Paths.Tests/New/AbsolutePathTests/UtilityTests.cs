using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

/// <summary>
/// Tests for utility methods.
/// </summary>
public class UtilityTests
{
    [SkippableTheory]
    [InlineData("/", "/foo", true)]
    [InlineData("/foo", "/foo/bar", true)]
    [InlineData("C:\\", "C:\\foo", false)]
    [InlineData("C:\\foo", "C:\\foo\\bar", false)]
    public void InFolder(string parent, string child, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());

        var parentPath = AbsolutePath.FromFullPath(parent);
        var childPath = AbsolutePath.FromFullPath(child);

        childPath.InFolder(parentPath).Should().BeTrue();
    }

    [SkippableTheory]
    [InlineData("foo", "/", "/foo", true)]
    [InlineData("foo/bar", "/", "/foo/bar", true)]
    [InlineData("bar", "/foo", "/foo/bar", true)]
    [InlineData("foo", "C:\\", "C:\\foo", false)]
    [InlineData("foo\\bar", "C:\\", "C:\\foo\\bar", false)]
    [InlineData("bar", "C:\\foo", "C:\\foo\\bar", false)]
    public void RelativeTo(string expected, string parent, string child, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());

        var childPath = AbsolutePath.FromFullPath(child);
        var parentPath = AbsolutePath.FromFullPath(parent);

        childPath
            .RelativeTo(parentPath)
            .ToString()
            .Should()
            .Be(expected);
    }

    [SkippableTheory]
    [InlineData("/", "/foo", true)]
    [InlineData("/", "/foo/bar", true)]
    [InlineData("C:\\", "C:\\foo", false)]
    [InlineData("C:\\", "C:\\foo\\bar", false)]
    public void TopParent(string expected, string item, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());

        var path = AbsolutePath.FromFullPath(item);

        path.TopParent.GetFullPath()
            .Should()
            .Be(expected);
    }

    [SkippableTheory]
    [InlineData("/", true, true)]
    [InlineData("/foo", false, true)]
    [InlineData("foo", false, true)]
    [InlineData("C:\\", true, false)]
    [InlineData("C:", false, false)]
    [InlineData("C:\\foo", false, false)]
    [InlineData("foo", false, false)]
    public void Test_IsRootDirectory(string input, bool expected, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());
        AbsolutePath.IsRootDirectory(input).Should().Be(expected);
    }
}
