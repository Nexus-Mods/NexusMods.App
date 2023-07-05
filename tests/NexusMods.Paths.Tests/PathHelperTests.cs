using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentAssertions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class PathHelperTests
{
    private static IOSInformation CreateOSInformation(bool isUnix)
    {
        return isUnix ? OSInformation.FakeUnix : OSInformation.FakeWindows;
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
    [InlineData(true, "/            ", false)]
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
    [InlineData(false, "C:\\\\foo", false)]
    [InlineData(false, "foo\\bar", false)]
    public void Test_IsSanitized(bool isUnix, string path, bool expected)
    {
        var actual = PathHelpers.IsSanitized(path, CreateOSInformation(isUnix));
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "/foo/bar", false)]
    [InlineData(true, "foo/bar", true)]
    [InlineData(false, "C:/foo/bar", false)]
    [InlineData(false, "foo/bar", true)]
    public void Test_IsSanitized_Relative(bool isUnix, string path, bool expected)
    {
        var actual = PathHelpers.IsSanitized(path, CreateOSInformation(isUnix), isRelative: true);
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "", "")]
    [InlineData(true, "/", "/")]
    [InlineData(true, "/foo/", "/foo")]
    [InlineData(true, "foo/", "foo")]
    [InlineData(true, "/foo\\bar", "/foo/bar")]
    [InlineData(false, "", "")]
    [InlineData(false, "C:/", "C:/")]
    [InlineData(false, "C:\\", "C:/")]
    [InlineData(false, "C:\\foo", "C:/foo")]
    [InlineData(false, "C:\\foo\\", "C:/foo")]
    [InlineData(false, "C:\\\\foo", "C:/foo")]
    [InlineData(false, "C:\\\\\\\\\\foo\\\\\\bar\\\\\\baz\\\\\\\\", "C:/foo/bar/baz")]
    public void Test_Sanitize(bool isUnix, string input, string expectedOutput)
    {
        var actualOutput = PathHelpers.Sanitize(input, CreateOSInformation(isUnix));
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(true, "", "", true)]
    [InlineData(true,"foo", "", false)]
    [InlineData(true,"", "foo", false)]
    [InlineData(true,"foo", "foo", true)]
    [InlineData(true,"foo", "FOO", true)]
    [InlineData(true,"/foo", "/foo", true)]
    [InlineData(true,"/foo", "/FOO", true)]
    [InlineData(false, "C:/", "C:/", true)]
    [InlineData(false, "C:/foo", "C:/foo", true)]
    [InlineData(false, "C:/foo", "C:/FOO", true)]
    public void Test_Equals(bool isUnix, string left, string right, bool expected)
    {
        var actual = PathHelpers.PathEquals(left, right, CreateOSInformation(isUnix));
        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, "", "", 0)]
    [InlineData(true, "", "foo", -1)]
    [InlineData(true, "foo", "", 1)]
    [InlineData(true, "foo", "foo", 0)]
    [InlineData(true, "foo", "FOO", 0)]
    [InlineData(true, "/foo", "/foo", 0)]
    [InlineData(true, "/foo", "/FOO", 0)]
    [InlineData(true, "/FOO", "/foo", 0)]
    [InlineData(true, "/foo", "/bar", 1)]
    [InlineData(true, "/bar", "/foo", -1)]
    [InlineData(false, "C:/foo", "C:/foo", 0)]
    [InlineData(false, "C:/foo", "C:/FOO", 0)]
    [InlineData(false, "C:/FOO", "C:/foo", 0)]
    [InlineData(false, "C:/foo", "C:/bar", 1)]
    [InlineData(false, "C:/bar", "C:/foo", -1)]
    public void Test_Compare(bool isUnix, string left, string right, int expected)
    {
        var actual = PathHelpers.Compare(left, right, CreateOSInformation(isUnix));
        actual = actual switch
        {
            0 => 0,
            > 0 => 1,
            < 0 => -1,
        };

        actual.Should().Be(expected);
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

    [Theory]
    [InlineData(true, false, "", "")]
    [InlineData(true, false, "foo/bar", "foo+bar")]
    [InlineData(true, false,"/", "/")]
    [InlineData(true, false,"/foo", "/+foo")]
    [InlineData(true, false,"/foo/bar/baz", "/+foo+bar+baz")]
    [InlineData(false, false,"C:/", "C:/")]
    [InlineData(false, false,"C:/foo", "C:/+foo")]
    [InlineData(false, false,"C:/foo/bar/baz", "C:/+foo+bar+baz")]
    [InlineData(true, true, "", "")]
    [InlineData(true, true, "foo/bar", "bar+foo")]
    [InlineData(true, true,"/", "/")]
    [InlineData(true, true,"/foo", "foo+/")]
    [InlineData(true, true,"/foo/bar/baz", "baz+bar+foo+/")]
    [InlineData(false, true,"C:/", "C:/")]
    [InlineData(false, true,"C:/foo", "foo+C:/")]
    [InlineData(false, true,"C:/foo/bar/baz", "baz+bar+foo+C:/")]
    public void Test_WalkParts(bool isUnix, bool isReverse, string path, string expectedOutput)
    {
        var sb = new StringBuilder();

        // ReSharper disable once InconsistentNaming
        PathHelpers.WalkParts(path, ref sb, (ReadOnlySpan<char> part, ref StringBuilder sb_) =>
        {
            if (sb_.Length != 0) sb_.Append('+');
            sb_.Append(part);
            return true;
        }, CreateOSInformation(isUnix), isReverse);

        var actualOutput = sb.ToString();
        actualOutput.Should().Be(expectedOutput);

        sb = new StringBuilder();
        PathHelpers.WalkParts(path, part =>
        {
            if (sb.Length != 0) sb.Append('+');
            sb.Append(part);
            return true;
        }, CreateOSInformation(isUnix), isReverse);

        actualOutput = sb.ToString();
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [InlineData(true, false, "/foo/bar/baz", 1, "/")]
    [InlineData(true, false, "/foo/bar/baz", 2, "/+foo")]
    [InlineData(true, false, "/foo/bar/baz", 3, "/+foo+bar")]
    [InlineData(true, false, "/foo/bar/baz", 4, "/+foo+bar+baz")]
    [InlineData(true, true, "/foo/bar/baz", 1, "baz")]
    [InlineData(true, true, "/foo/bar/baz", 2, "baz+bar")]
    [InlineData(true, true, "/foo/bar/baz", 3, "baz+bar+foo")]
    [InlineData(true, true, "/foo/bar/baz", 4, "baz+bar+foo+/")]
    [InlineData(false, false, "C:/foo/bar/baz", 1, "C:/")]
    [InlineData(false, false, "C:/foo/bar/baz", 2, "C:/+foo")]
    [InlineData(false, false, "C:/foo/bar/baz", 3, "C:/+foo+bar")]
    [InlineData(false, false, "C:/foo/bar/baz", 4, "C:/+foo+bar+baz")]
    [InlineData(false, true, "C:/foo/bar/baz", 1, "baz")]
    [InlineData(false, true, "C:/foo/bar/baz", 2, "baz+bar")]
    [InlineData(false, true, "C:/foo/bar/baz", 3, "baz+bar+foo")]
    [InlineData(false, true, "C:/foo/bar/baz", 4, "baz+bar+foo+C:/")]
    public void Test_WalkPartsPartially(bool isUnix, bool isReverse, string path, int stopAfterN, string expectedOutput)
    {
        var sb = new StringBuilder();
        var counter = 0;

        PathHelpers.WalkParts(path, part =>
        {
            if (sb.Length != 0) sb.Append('+');
            sb.Append(part);
            counter++;
            return counter < stopAfterN;
        }, CreateOSInformation(isUnix), isReverse);

        var actualOutput = sb.ToString();
        actualOutput.Should().Be(expectedOutput);
    }

    [Theory]
    [MemberData(nameof(TestData_GetParts))]
    public void Test_GetParts(bool isUnix, bool isReverse, string path, List<string> expectedOutput)
    {
        var actualOutput = PathHelpers.GetParts(path, CreateOSInformation(isUnix), isReverse);
        actualOutput.Should().Equal(expectedOutput);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static IEnumerable<object[]> TestData_GetParts => new[]
    {
        new object[] { true, false, "/foo/bar/baz", new List<string> { "/", "foo", "bar", "baz" } },
        new object[] { true, true, "/foo/bar/baz", new List<string> { "baz", "bar", "foo", "/" } },
        new object[] { false, false, "C:/foo/bar/baz", new List<string>{ "C:/", "foo", "bar", "baz" }},
        new object[] { false, true, "C:/foo/bar/baz", new List<string>{ "baz", "bar", "foo", "C:/" }},
    };
}
