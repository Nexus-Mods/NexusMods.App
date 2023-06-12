using FluentAssertions;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class CombineCheckedTest
{
    private readonly InMemoryFileSystem _fileSystem;

    public CombineCheckedTest()
    {
        _fileSystem = new InMemoryFileSystem();
    }

    [Fact]
    public void CasingMatchesFilesystem_Lower() => AssertCasingMatchesFileSystem("Assets/AbsolutePath/lower_dummy.txt");

    [Fact]
    public void CasingMatchesFilesystem_Upper() => AssertCasingMatchesFileSystem("Assets/AbsolutePath/UPPER_DUMMY.TXT");

    [Theory]
    [InlineData("\\Assets\\AbsolutePath\\UPPER_DUMMY.TXT")] // Including starting slash here too!
    [InlineData("/Assets/AbsolutePath/UPPER_DUMMY.TXT")]
    public void CasingMatchesFilesystem_Upper_WithDifferentSlashes(string relativePath) => AssertCasingMatchesFileSystem(relativePath);

    private void AssertCasingMatchesFileSystem(string actualRelativePath)
    {
        var absolutePath = AbsolutePath.FromDirectoryAndFileName(AppContext.BaseDirectory, "", _fileSystem);
        var result = absolutePath.Combine(actualRelativePath);

        result.ToString().Where(x => x is '\\' or '/').Distinct().Count()
            .Should()
            .Be(1, "Paths are normallized to a single separator format");
        result.ToString().Should().NotContain(@"/\", "trailing separators are recognized and removed");
        result.ToString().Should().NotContain(@"\/", "trailing separators are recognized and removed");
    }

    [SkippableTheory]
    [InlineData("/", "foo", "/foo", "/", "foo", true)]
    [InlineData("/foo", "bar", "/foo/bar", "/foo", "bar", true)]
    [InlineData("/foo/bar", "baz", "/foo/bar/baz", "/foo/bar", "baz", true)]
    [InlineData("C:\\", "foo", "C:\\foo", "C:\\", "foo", false)]
    [InlineData("C:\\foo", "bar", "C:\\foo\\bar", "C:\\foo", "bar", false)]
    [InlineData("C:\\foo\\bar", "baz", "C:\\foo\\bar\\baz", "C:\\foo\\bar", "baz", false)]
    public void Test_CombinedChecked(string left, string right,
        string expectedFullPath, string expectedDirectory,
        string expectedFileName, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());
        var absolutePath = AbsolutePath.FromFullPath(left, _fileSystem);
        var relativePath = new RelativePath(right);

        var result = absolutePath.Combine(relativePath);
        result.GetFullPath().Should().Be(expectedFullPath);
        result.Directory.Should().Be(expectedDirectory);
        result.FileName.Should().Be(expectedFileName);
    }

    [SkippableTheory]
    [InlineData("/", "foo", "/foo", "/", "foo", true)]
    [InlineData("/foo", "bar", "/foo/bar", "/foo", "bar", true)]
    [InlineData("/foo/bar", "baz", "/foo/bar/baz", "/foo/bar", "baz", true)]
    [InlineData("C:\\", "foo", "C:\\foo", "C:\\", "foo", false)]
    [InlineData("C:\\foo", "bar", "C:\\foo\\bar", "C:\\foo", "bar", false)]
    [InlineData("C:\\foo\\bar", "baz", "C:\\foo\\bar\\baz", "C:\\foo\\bar", "baz", false)]
    public void Test_CombineUnchecked(string left, string right,
        string expectedFullPath, string expectedDirectory,
        string expectedFileName, bool linux)
    {
        Skip.If(linux != OperatingSystem.IsLinux());
        var absolutePath = AbsolutePath.FromFullPath(left, _fileSystem);
        var relativePath = new RelativePath(right);

        var result = absolutePath.Combine(relativePath);
        result.GetFullPath().Should().Be(expectedFullPath);
        result.Directory.Should().Be(expectedDirectory);
        result.FileName.Should().Be(expectedFileName);
    }
}
