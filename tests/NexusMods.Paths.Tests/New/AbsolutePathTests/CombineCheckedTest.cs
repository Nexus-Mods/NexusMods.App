using FluentAssertions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

public class CombineCheckedTest
{
    [Fact]
    public void CasingMatchesFilesystem_Lower() => AssertCasingMatchesFileSystem("Assets/AbsolutePath/lower_dummy.txt");
    
    [Fact]
    public void CasingMatchesFilesystem_Upper() => AssertCasingMatchesFileSystem("Assets/AbsolutePath/UPPER_DUMMY.TXT");
    
    [Theory]
    [InlineData("\\Assets\\AbsolutePath\\UPPER_DUMMY.TXT")] // Including starting slash here too!
    [InlineData("/Assets/AbsolutePath/UPPER_DUMMY.TXT")] 
    public void CasingMatchesFilesystem_Upper_WithDifferentSlashes(string relativePath) => AssertCasingMatchesFileSystem(relativePath);
    
    private static void AssertCasingMatchesFileSystem(string actualRelativePath)
    {
        var absolutePath = AbsolutePath.FromDirectoryAndFileName(AppContext.BaseDirectory, "");
        var result = absolutePath.CombineChecked(actualRelativePath);

        result.ToString().Where(x => x is '\\' or '/').Distinct().Count()
            .Should()
            .Be(1, "Paths are normallized to a single separator format");
        result.ToString().Should().NotContain(@"/\", "trailing separators are recognized and removed");
        result.ToString().Should().NotContain(@"\/", "trailing separators are recognized and removed");
    }
}