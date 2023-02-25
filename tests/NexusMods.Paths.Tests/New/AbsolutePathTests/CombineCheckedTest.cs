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
        // Path.GetFullPath normalizes
        var expected = Path.GetFullPath(AppContext.BaseDirectory + actualRelativePath);
        var relativePath = new RelativePath(actualRelativePath.ToLower());
        var absolutePath = AbsolutePath.FromDirectoryAndFileName(AppContext.BaseDirectory, "");
        Assert.Equal(expected, absolutePath.CombineChecked(relativePath).GetFullPath());
    }
}