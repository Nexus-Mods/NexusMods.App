using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class AbsolutePathTests
{
    [Fact]
    public void CanParsePaths()
    {
        Assert.Equal(((AbsolutePath)@"c:\foo\bar").ToString(), ((AbsolutePath)@"c:\foo\bar").ToString());
    }

    [Fact]
    public void CanGetParentPath()
    {
        Assert.Equal(((AbsolutePath)@"c:\foo").ToString(), ((AbsolutePath)@"c:\foo\bar").Parent.ToString());
    }

    [Fact]
    public void ParentOfTopLevelPathThrows()
    {
        Assert.Throws<PathException>(() => ((AbsolutePath)@"c:\").Parent.ToString());
    }

    [Fact]
    public void CanCreateRelativePathsFromAbolutePaths()
    {
        Assert.Equal((RelativePath)@"baz\qux.zip",
            ((AbsolutePath)@"\\foo\bar\baz\qux.zip").RelativeTo((AbsolutePath)@"\\foo\bar"));
        Assert.Throws<PathException>(() =>
            ((AbsolutePath)@"\\foo\bar\baz\qux.zip").RelativeTo((AbsolutePath)@"\\z\bar"));
        Assert.Throws<PathException>(() =>
            ((AbsolutePath)@"\\foo\bar\baz\qux.zip").RelativeTo((AbsolutePath)@"\\z\bar\buz"));
    }

    [Fact]
    public void PathsAreEquatable()
    {
        Assert.Equal((AbsolutePath)@"c:\foo", (AbsolutePath)@"c:\foo");

        Assert.True((AbsolutePath)@"c:\foo" == (AbsolutePath)@"c:\Foo");
        Assert.False((AbsolutePath)@"c:\foo" != (AbsolutePath)@"c:\Foo");
        Assert.NotEqual((AbsolutePath)@"c:\foo", (AbsolutePath)@"c:\bar");
        Assert.NotEqual((AbsolutePath)@"c:\foo\bar", (AbsolutePath)@"c:\foo");
    }

    [Fact]
    public void CanGetPathHashCodes()
    {
        var pathA = @"c:\foo\bar.zip".ToAbsolutePath();
        Assert.Equal(pathA.GetHashCode(), pathA.GetHashCode());
        Assert.Equal(@"c:\foo\bar.baz".ToAbsolutePath().GetHashCode(),
            @"C:\Foo\Bar.bAz".ToAbsolutePath().GetHashCode());
        Assert.Equal(-1, new AbsolutePath().GetHashCode());
    }


    [Fact]
    public void CaseInsensitiveEquality()
    {
        Assert.Equal(@"c:\foo\bar.baz".ToAbsolutePath(), @"C:\Foo\Bar.bAz".ToAbsolutePath());
        Assert.NotEqual(@"c:\foo\bar.baz".ToAbsolutePath(), (object)42);
    }

    [Fact]
    public void CanReplaceExtensions()
    {
        Assert.Equal(new Extension(".dds"), ((AbsolutePath)@"/foo/bar.dds").Extension);
        Assert.Equal((RelativePath)"bar.dds", ((AbsolutePath)@"/foo/bar.dds").FileName);
        Assert.Equal((AbsolutePath)@"/foo/bar.zip",
            ((AbsolutePath)@"/foo/bar.dds").ReplaceExtension(new Extension(".zip")));
        Assert.Equal((AbsolutePath)@"/foo\bar.zip",
            ((AbsolutePath)@"/foo\bar").ReplaceExtension(new Extension(".zip")));
        Assert.Equal(
            (AbsolutePath)@"E:\\foo\\bar\\more-than-three-letters.foo",
            ((AbsolutePath)@"E:\\foo\\bar\\more-than-three-letters.bar").ReplaceExtension(new Extension(".foo")));
    }

    [Fact]
    public void CanAddExtension()
    {
        var pathA = @"c:\foo\bar.zip".ToAbsolutePath();
        var pathB = @"c:\foo\bar".ToAbsolutePath();
        Assert.Equal(pathA, pathB.WithExtension(KnownExtensions.Zip));
    }

    [Fact]
    public void CanAppendToName()
    {
        var pathA = @"c:\foo\bar.zip".ToAbsolutePath();
        var pathB = @"c:\foo\barBaz.zip".ToAbsolutePath();
        Assert.Equal(pathB, pathA.AppendToName("Baz"));
    }

    [Fact]
    public void CanGetPathFormats()
    {
        Assert.Equal(PathFormat.Windows, ((AbsolutePath)@"c:\foo\bar").PathFormat);
        Assert.Equal(PathFormat.Windows, ((AbsolutePath)@"\\foo\bar").PathFormat);
        Assert.Equal(PathFormat.Unix, ((AbsolutePath)@"/foo/bar").PathFormat);
        Assert.Throws<PathException>(() => ((AbsolutePath)@"c!\foo/bar").PathFormat);
    }

    [Fact]
    public void CanJoinPaths()
    {
        Assert.Equal("/foo/bar/baz/qux",
            ((AbsolutePath)"/").Join("foo", (RelativePath)"bar", "baz/qux").ToString());
        Assert.Throws<PathException>(() => ((AbsolutePath)"/").Join(42));
    }

    [Fact]
    public void CanConvertPathsToStrings()
    {
        Assert.Equal("/foo/bar", ((AbsolutePath)"/foo/bar").ToString());
        Assert.Equal("", new AbsolutePath().ToString());
    }

    [Fact]
    public void CanCheckInFolder()
    {
        var pathA = @"c:\foo\bar\baz.zip".ToAbsolutePath();
        var pathB = @"c:\foo\bar".ToAbsolutePath();
        var pathC = @"c:\fOo\Bar".ToAbsolutePath();
        Assert.True(pathA.InFolder(pathB));
        Assert.True(pathA.InFolder(pathC));
        Assert.False(pathB.InFolder(pathA));
    }

    [Fact]
    public void PathsAreComparable()
    {
        var data = new[]
        {
            (AbsolutePath)@"c:\a",
            (AbsolutePath)@"c:\b\c",
            (AbsolutePath)@"c:\d\e\f",
            (AbsolutePath)@"c:\b"
        };
        var data2 = data.OrderBy(a => a).ToArray();

        var data3 = new[]
        {
            (AbsolutePath)@"c:\a",
            (AbsolutePath)@"c:\b",
            (AbsolutePath)@"c:\b\c",
            (AbsolutePath)@"c:\d\e\f"
        };
        Assert.Equal(data3, data2);
    }

    [Fact]
    public void CanGetThisAndAllParents()
    {
        var path = @"c:\foo\bar\baz.zip".ToAbsolutePath();
        var subPaths = new[]
        {
            @"c:\",
            @"C:\foo",
            @"c:\foo\Bar",
            @"c:\foo\bar\baz.zip"
        }.Select(f => f.ToAbsolutePath());

        Assert.Equal(subPaths.OrderBy(f => f), path.ThisAndAllParents().OrderBy(f => f).ToArray());
    }

    #region IO Tests

    [Fact]
    public async Task CanReadAndWriteFiles()
    {
        var testDir = KnownFolders.EntryFolder.Join("testDir");
        if (testDir.DirectoryExists())
            testDir.DeleteDirectory();
        testDir.CreateDirectory();

        var fileOne = testDir.Join("file1.txt");
        var fileTwo = testDir.Join("file2.txt");
        var fileThree = testDir.Join("file3.txt");
        var fileFour = testDir.Join(@"testFolder\inner\testfour.txt");
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

    #endregion
}