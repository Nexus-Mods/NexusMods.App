using System.Runtime.InteropServices;
using FluentAssertions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class PathHelperTests
{
    private static IOSInformation CreateOSInformation(bool isUnix)
    {
        return isUnix ? new OSInformation(OSPlatform.Linux) : new OSInformation(OSPlatform.Windows);
    }

    [Theory]
    [InlineData(true, "", true)]
    [InlineData(true, "/", true)]
    [InlineData(true, "/foo", true)]
    [InlineData(true, "/foo/bar", true)]
    [InlineData(true, "/foo/bar.txt", true)]
    [InlineData(true, "foo", true)]
    [InlineData(true, "foo/bar", true)]
    [InlineData(true, "foo/", false)]
    [InlineData(true, "foo/bar/", false)]
    [InlineData(true, "/foo/", false)]
    [InlineData(false, "", true)]
    [InlineData(false, "C:/", true)]
    [InlineData(false, "C:/foo", true)]
    [InlineData(false, "C:/foo/bar", true)]
    [InlineData(false, "C:/foo/bar.txt", true)]
    [InlineData(false, "foo", true)]
    [InlineData(false, "foo/bar", true)]
    [InlineData(false, "foo/", false)]
    [InlineData(false, "foo/bar/", false)]
    [InlineData(false, "C:/foo/", false)]
    [InlineData(false, "C:\\", false)]
    [InlineData(false, "C:\\foo", false)]
    [InlineData(false, "C:\\foo\\", false)]
    [InlineData(false, "foo\\bar", false)]
    public void Test_IsSanitized(bool isUnix, string path, bool expected)
    {
        var actual = PathHelpers.IsSanitized(path, CreateOSInformation(isUnix));
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "", "")]
    [InlineData(true, "/", "/")]
    [InlineData(true, "/foo/", "/foo")]
    [InlineData(true, "foo/", "foo")]
    [InlineData(false, "", "")]
    [InlineData(false, "C:/", "C:/")]
    [InlineData(false, "C:\\", "C:/")]
    [InlineData(false, "C:\\foo", "C:/foo")]
    [InlineData(false, "C:\\foo\\", "C:/foo")]
    public void Test_Sanitize(bool isUnix, string input, string expectedOutput)
    {
        var actualOutput = PathHelpers.Sanitize(input, CreateOSInformation(isUnix));
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [MemberData(nameof(TestData_IsValidWindowsDriveChar))]
    public void Test_IsValidWindowsDriveChar(char input, bool expectedResult)
    {
        var actualResult = PathHelpers.IsValidWindowsDriveChar(input);
        actualResult.Should().Be(expectedResult);
    }

    public static IEnumerable<object[]> TestData_IsValidWindowsDriveChar()
    {
        for (var i = (uint)'A'; i <= 'Z'; i++)
        {
            yield return new object[] { (char)i, true };
        }

        for (var i = (uint)'a'; i <= 'z'; i++)
        {
            yield return new object[] { (char)i, true };
        }

        for (var i = 0; i <= 9; i++)
        {
            yield return new object[] { i.ToString()[0], false };
        }
    }

    [Theory]
    [InlineData(true, "/", 1)]
    [InlineData(true, "/foo", 1)]
    [InlineData(true, "/foo/", 1)]
    [InlineData(true, "/foo/bar", 1)]
    [InlineData(true, "foo", -1)]
    [InlineData(true, "foo/bar", -1)]
    [InlineData(false, "C:/", 3)]
    [InlineData(false, "C:/foo", 3)]
    [InlineData(false, "C:/foo/", 3)]
    [InlineData(false, "C:/foo/bar", 3)]
    [InlineData(false, "foo", -1)]
    [InlineData(false, "foo/bar", -1)]
    public void Test_GetRootLength(bool isUnix, string path, int expectedRootLength)
    {
        var actualRootLength = PathHelpers.GetRootLength(path, CreateOSInformation(isUnix));
        actualRootLength.Should().Be(expectedRootLength);
    }

    [Theory]
    [InlineData(true, "/", true)]
    [InlineData(true, "/foo", true)]
    [InlineData(true, "/foo/", true)]
    [InlineData(true, "/foo/bar", true)]
    [InlineData(true, "foo", false)]
    [InlineData(true, "foo/bar", false)]
    [InlineData(false, "C:/", true)]
    [InlineData(false, "C:/foo", true)]
    [InlineData(false, "C:/foo/", true)]
    [InlineData(false, "C:/foo/bar", true)]
    [InlineData(false, "foo", false)]
    [InlineData(false, "foo/bar", false)]
    public void Test_IsRooted(bool isUnix, string path, bool expectedResult)
    {
        var actualResult = PathHelpers.IsRooted(path, CreateOSInformation(isUnix));
        actualResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(true, "/", "/")]
    [InlineData(true, "/foo", "/")]
    [InlineData(true, "/foo/", "/")]
    [InlineData(true, "/foo/bar", "/")]
    [InlineData(true, "foo", "")]
    [InlineData(true, "foo/bar", "")]
    [InlineData(false, "C:/", "C:/")]
    [InlineData(false, "C:/foo", "C:/")]
    [InlineData(false, "C:/foo/", "C:/")]
    [InlineData(false, "C:/foo/bar", "C:/")]
    [InlineData(false, "foo", "")]
    [InlineData(false, "foo/bar", "")]
    public void Test_GetRootedPart(bool isUnix, string path, string expectedRootPart)
    {
        var actualRootPart = PathHelpers.GetRootPart(path, CreateOSInformation(isUnix)).ToString();
        actualRootPart.Should().Be(expectedRootPart);
    }

    [Theory]
    [InlineData(true, "/", true)]
    [InlineData(true, "/foo", false)]
    [InlineData(true, "/foo/", false)]
    [InlineData(true, "/foo/bar", false)]
    [InlineData(true, "foo", false)]
    [InlineData(true, "foo/bar", false)]
    [InlineData(false, "C:/", true)]
    [InlineData(false, "C:/foo", false)]
    [InlineData(false, "C:/foo/", false)]
    [InlineData(false, "C:/foo/bar", false)]
    [InlineData(false, "foo", false)]
    [InlineData(false, "foo/bar", false)]
    public void Test_IsRootDirectory(bool isUnix, string path, bool expected)
    {
        var actual = PathHelpers.IsRootDirectory(path, CreateOSInformation(isUnix));
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "/", "foo", "/foo")]
    [InlineData(true, "/foo", "bar", "/foo/bar")]
    [InlineData(true, "foo", "bar", "foo/bar")]
    [InlineData(true, "/", "foo/bar", "/foo/bar")]
    [InlineData(true, "", "foo", "foo")]
    [InlineData(true, "foo", "", "foo")]
    [InlineData(false, "C:/", "foo", "C:/foo")]
    [InlineData(false, "C:/foo", "bar", "C:/foo/bar")]
    [InlineData(false, "C:/", "foo/bar", "C:/foo/bar")]
    [InlineData(false, "", "", "")]
    public void Test_JoinParts(bool isUnix, string left, string right, string expectedResult)
    {
        var actualResult1 = PathHelpers.JoinParts(left, right, CreateOSInformation(isUnix));
        actualResult1.Should().Be(expectedResult);

        var actualResult2 = PathHelpers.JoinParts(left.AsSpan(), right.AsSpan(), CreateOSInformation(isUnix));
        actualResult2.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("", "", 1)]
    [InlineData("a", "", 2)]
    [InlineData("", "a", 2)]
    [InlineData("a", "a", 3)]
    public void Test_GetMaxJoinedPartLength(string left, string right, int expectedMaxLength)
    {
        var actualMaxLength = PathHelpers.GetMaxJoinedPartLength(left, right);
        actualMaxLength.Should().Be(expectedMaxLength);
    }

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("foo", "", 3)]
    [InlineData("", "foo", 3)]
    [InlineData("foo", "bar", 7)]
    [InlineData("foo/", "bar", 7)]
    public void Test_GetExactJoinedPartLength(string left, string right, int expectedLength)
    {
        var actualLength = PathHelpers.GetExactJoinedPartLength(left, right);
        actualLength.Should().Be(expectedLength);
    }

    [Theory]
    [InlineData(true, "", "")]
    [InlineData(true, "foo", "foo")]
    [InlineData(true, "foo/bar", "bar")]
    [InlineData(true, "/", "")]
    [InlineData(true, "/foo", "foo")]
    [InlineData(true, "/foo/bar", "bar")]
    [InlineData(false, "C:/", "")]
    [InlineData(false, "C:/foo", "foo")]
    [InlineData(false, "C:/foo/bar", "bar")]
    public void Test_GetFileName(bool isUnix, string path, string expectedFileName)
    {
        var actualFileName = PathHelpers.GetFileName(path, CreateOSInformation(isUnix)).ToString();
        actualFileName.Should().Be(expectedFileName);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(".", "")]
    [InlineData("foo", "")]
    [InlineData("foo.txt", ".txt")]
    [InlineData("foo.bar.baz.txt", ".txt")]
    public void Test_GetExtension(string path, string expectedExtension)
    {
        var actualExtension = PathHelpers.GetExtension(path).ToString();
        actualExtension.Should().Be(expectedExtension);
    }

    [Theory]
    [InlineData("", ".txt", "")]
    [InlineData("foo", ".txt", "foo.txt")]
    [InlineData("foo.bar", ".txt", "foo.txt")]
    public void Test_ReplaceExtension(string input, string newExtension, string expectedOutput)
    {
        var actualOutput = PathHelpers.ReplaceExtension(input, newExtension);
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(true, "", 0)]
    [InlineData(true, "foo.txt", 0)]
    [InlineData(true, "foo/bar.txt", 1)]
    [InlineData(true, "/", 1)]
    [InlineData(true, "/foo.txt", 1)]
    [InlineData(true, "/foo/bar.txt", 2)]
    [InlineData(false, "C:/", 1)]
    [InlineData(false, "C:/foo.txt", 1)]
    [InlineData(false, "C:/foo/bar.txt", 2)]
    public void Test_GetDirectoryDepth(bool isUnix, string input, int expectedDepth)
    {
        var actualDepth = PathHelpers.GetDirectoryDepth(input, CreateOSInformation(isUnix));
        actualDepth.Should().Be(expectedDepth);
    }

    [Theory]
    [InlineData(true, "/", "/")]
    [InlineData(true, "/foo", "/")]
    [InlineData(true, "/foo/bar", "/foo")]
    [InlineData(true, "", "")]
    [InlineData(true, "foo", "")]
    [InlineData(true, "foo/bar", "foo")]
    [InlineData(false, "C:/", "C:/")]
    [InlineData(false, "C:/foo", "C:/")]
    [InlineData(false, "C:/foo/bar", "C:/foo")]
    [InlineData(false, "", "")]
    [InlineData(false, "foo", "")]
    [InlineData(false, "foo/bar", "foo")]
    public void Test_GetDirectoryName(bool isUnix, string input, string expectedOutput)
    {
        var actualOutput = PathHelpers.GetDirectoryName(input, CreateOSInformation(isUnix)).ToString();
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(true, "", "", false)]
    [InlineData(true, "foo", "", false)]
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
        var actual = PathHelpers.InFolder(child, parent, CreateOSInformation(isUnix));
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "", "", "")]
    [InlineData(true, "foo/bar", "foo", "bar")]
    [InlineData(true, "/foo", "/", "foo")]
    [InlineData(true, "/foo/bar", "/foo", "bar")]
    [InlineData(false, "C:/foo", "C:/", "foo")]
    [InlineData(false, "C:/foo/bar", "C:/foo", "bar")]
    public void Test_RelativeTo(bool isUnix, string child, string parent, string expectedOutput)
    {
        var actualOutput = PathHelpers.RelativeTo(child, parent, CreateOSInformation(isUnix)).ToString();
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(true, "", "")]
    [InlineData(true, "/", "/")]
    [InlineData(false, "C:/", "C:/")]
    [InlineData(true, "foo/bar", "foo")]
    [InlineData(true, "foo/bar/baz", "foo")]
    public void Test_GetTopParent(bool isUnix, string path, string expectedOutput)
    {
        var actualOutput = PathHelpers.GetTopParent(path, CreateOSInformation(isUnix)).ToString();
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(true, "", 0, "")]
    [InlineData(true, "/", 0, "/")]
    [InlineData(true, "/", 1, "")]
    [InlineData(true, "/foo", 1, "foo")]
    [InlineData(true, "/foo/bar", 1, "foo/bar")]
    [InlineData(true, "/foo/bar", 2, "bar")]
    [InlineData(true, "/foo/bar", 3, "")]
    [InlineData(false, "C:/", 0, "C:/")]
    [InlineData(false, "C:/", 1, "")]
    [InlineData(false, "C:/foo", 1, "foo")]
    [InlineData(false, "C:/foo/bar", 1, "foo/bar")]
    [InlineData(false, "C:/foo/bar", 2, "bar")]
    [InlineData(false, "C:/foo/bar", 3, "")]
    public void Test_DropParents(bool isUnix, string path, int count, string expectedOutput)
    {
        var actualOutput = PathHelpers.DropParents(path, count, CreateOSInformation(isUnix)).ToString();
        actualOutput.Should().Be(expectedOutput);
    }
}
