using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

/// <summary>
/// I/O related tests for the pathing library.
/// </summary>
// ReSharper disable once InconsistentNaming
public class IOTests
{
    // TODO: Linux/OSX equivalent.
    
    [SkippableTheory]
    [InlineData("C:", 50000)]
    public void GetFiles_ShouldSupportFsRoot_OnWindows(string directory, int minItems)
    {
        Skip.IfNot(OperatingSystem.IsWindows());
        
        // Note: This test can be slow, so the check is implemented like this.
        var path = AbsolutePath.FromFullPath(directory);
        int currentItems = 0;
        foreach (var _ in path.EnumerateFiles())
        {
            currentItems++;
            if (currentItems > minItems)
                return; // success
        }
        
        Assert.Fail($"Minimum item count not reach, expected at least {minItems}, got {currentItems}");
    }
    
    [Fact]
    public async Task CanReadAndWriteFiles()
    {
        var testDir = KnownFolders.EntryFolder.CombineChecked("testDir");
        if (testDir.DirectoryExists())
            testDir.DeleteDirectory();
        
        testDir.CreateDirectory();

        var fileOne = testDir.CombineChecked("file1.txt");
        var fileTwo = testDir.CombineChecked("file2.txt");
        var fileThree = testDir.CombineChecked("file3.txt");
        var fileFour = testDir.CombineChecked(@"testFolder\inner\testfour.txt");
        await fileOne.WriteAllTextAsync("this is a test");
        await fileTwo.WriteAllTextAsync("test two");
        
        Assert.Equal("test two", await fileTwo.ReadAllTextAsync());

        await fileOne.CopyToAsync(fileThree);
        Assert.True(fileThree.FileExists);
        
        Assert.Equal(Size.From(14), fileOne.Length);
        Assert.True(DateTime.Now - fileOne.LastWriteTime < TimeSpan.FromSeconds(1));
        Assert.True(DateTime.UtcNow - fileOne.LastWriteTimeUtc < TimeSpan.FromSeconds(1));
        
        Assert.True(DateTime.Now - fileOne.CreationTime < TimeSpan.FromSeconds(1));
        Assert.True(DateTime.UtcNow - fileOne.CreationTimeUtc < TimeSpan.FromSeconds(1));
        
        var files = testDir.EnumerateFiles(KnownExtensions.Txt).ToHashSet();
        Assert.Contains(fileOne, files);
        Assert.Contains(fileTwo, files);
        Assert.Contains(fileThree, files);

        // Make sure we can delete read only files
        fileThree.FileInfo.IsReadOnly = true;
        fileThree.Delete();
        Assert.False(fileThree.FileExists);
        
        fileFour.Parent.CreateDirectory();
        fileFour.Create().Close();
        fileFour.FileInfo.IsReadOnly = true;
        fileTwo.FileInfo.IsReadOnly = true;
        await fileTwo.MoveToAsync(fileFour);
        Assert.False(fileTwo.FileExists);
        Assert.True(fileFour.FileExists);

        var dirs = testDir.EnumerateDirectories().ToHashSet();
        Assert.Equal(2, dirs.Count);
        
        testDir.DeleteDirectory(true);
        Assert.True(fileOne.FileExists);
        testDir.Delete();
    }
}