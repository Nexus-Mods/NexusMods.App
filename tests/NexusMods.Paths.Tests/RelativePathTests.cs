namespace NexusMods.Paths.Tests;

public class RelativePathTests
{
    [Fact]
    public void CanReplaceExtensions()
    {
        Assert.Equal(new Extension(".dds"), ((RelativePath)@"foo\bar.dds").Extension);
        Assert.Equal((RelativePath)@"foo\bar.zip",
            ((RelativePath)@"foo\bar.dds").ReplaceExtension(new Extension(".zip")));
        Assert.NotEqual((RelativePath)@"foo\bar\z.zip",
            ((RelativePath)@"foo\bar.dds").ReplaceExtension(new Extension(".zip")));
        Assert.Equal((RelativePath)@"foo\bar.zip",
            ((RelativePath)@"foo\bar").ReplaceExtension(new Extension(".zip")));
    }

    [Fact]
    public void PathsAreValidated()
    {
        Assert.Throws<PathException>(() => @"c:\foo".ToRelativePath());
    }

    [Fact]
    public void CanCreatePathsRelativeTo()
    {
        Assert.Equal((AbsolutePath)@"c:\foo\bar\baz.zip",
            ((RelativePath)@"baz.zip").RelativeTo((AbsolutePath)@"c:\foo\bar"));
    }

    [Fact]
    public void ObjectMethods()
    {
        Assert.Equal(@"foo\bar", ((RelativePath)@"foo\bar").ToString());

        Assert.Equal((RelativePath)@"foo\bar", (RelativePath)@"foo/bar");
        Assert.NotEqual((RelativePath)@"foo\bar", (object)42);
        Assert.True((RelativePath)@"foo\bar" == (RelativePath)@"foo/bar");
        Assert.True((RelativePath)@"foo\bar" != (RelativePath)@"foo/baz");

        Assert.Equal(((RelativePath)@"foo\bar").GetHashCode(), ((RelativePath)@"Foo\bar").GetHashCode());
    }


    [Fact]
    public void CanGetPathHashCodes()
    {
        var path = @"foo\bar.baz".ToRelativePath();
        Assert.Equal(path.GetHashCode(), path.GetHashCode());
        Assert.Equal(path.GetHashCode(), @"Foo\Bar.bAz".ToRelativePath().GetHashCode());
        Assert.Equal(-1, new RelativePath().GetHashCode());
    }


    [Fact]
    public void CaseInsensitiveEquality()
    {
        Assert.Equal(@"foo\bar.baz".ToRelativePath(), @"Foo\Bar.bAz".ToRelativePath());
        Assert.NotEqual(@"foo\bar.baz".ToRelativePath(), (object)42);
    }

    [Fact]
    public void CanGetFilenameFromRelativePath()
    {
        Assert.Equal((RelativePath)"bar.dds", @"foo\bar.dds".ToRelativePath().FileName);
    }

    [Fact]
    public void CanGetDepth()
    {
        var path = @"\foo\bar\baz".ToRelativePath();
        Assert.Equal(3, path.Depth);
    }

    [Fact]
    public void CanGetParent()
    {
        var path = @"\foo\bar\baz".ToRelativePath();
        Assert.Equal(@"\foo\bar".ToRelativePath(), path.Parent);
        Assert.Throws<PathException>(() => @"\foo".ToRelativePath().Parent);
    }

    [Fact]
    public void CanGetTopParent()
    {
        var path = @"\foo\bar\baz\qux".ToRelativePath();
        Assert.Equal(@"\foo".ToRelativePath(), path.TopParent);
        Assert.Equal(@"\foo".ToRelativePath(), @"\foo".ToRelativePath().TopParent);
    }

    [Fact]
    public void CanGetFileNameWithoutExtension()
    {
        var pathA = @"foo\bar.zip".ToRelativePath();
        var pathB = @"foo\bar".ToRelativePath();
        Assert.Equal(pathB.FileName, pathA.FileNameWithoutExtension);
        Assert.Equal(pathB.FileName, pathB.FileNameWithoutExtension);
    }

    [Fact]
    public void CanCheckStartAndEndOfFilePaths()
    {
        var pathA = @"foo\bar\baz.zip".ToRelativePath();
        Assert.True(pathA.FileNameEndsWith("iP"));
        Assert.True(pathA.FileNameStartsWith("Ba"));
    }

    [Fact]
    public void CanCheckStartOfPath()
    {
        var pathA = @"foo\bar\baz".ToRelativePath();
        Assert.True(pathA.StartsWith("fo"));
        Assert.True(pathA.StartsWith("Fo"));
        Assert.False(pathA.StartsWith("fooo"));
    }

    [Fact]
    public void CanConvertWithExplicitConversions()
    {
        Assert.Equal("foo", (string)"foo".ToRelativePath());
        Assert.Equal((RelativePath)"foo", "foo".ToRelativePath());
    }

    [Fact]
    public void PathsAreEquaitable()
    {
        var pathA = @"foo\bar".ToRelativePath();
        var pathAA = @"foo\baR".ToRelativePath();
        var pathB = @"foo\baz".ToRelativePath();

        Assert.Equal(pathA, pathA);
        Assert.Equal(pathA, pathAA);
        Assert.True(pathA == pathAA);
        Assert.False(pathA == pathB);
    }

    [Fact]
    public void CanCreateFilenameFromParts()
    {
        var path = @"foo\bar.zip".ToRelativePath();
        Assert.Equal(path, RelativePath.FromParts(new[] { "foo", "bar.zip" }));
        Assert.Equal(path, RelativePath.FromParts(path.Parts));
    }

    [Fact]
    public void CanGetPathsAsStrings()
    {
        var path = @"\foo\bar\baz".ToRelativePath();
        Assert.Equal(@"foo\bar\baz", path.ToString());
        Assert.Equal("", new RelativePath().ToString());
    }

    [Fact]
    public void CanAddExtension()
    {
        var pathA = @"foo\bar.zip".ToRelativePath();
        var pathB = @"foo\bar".ToRelativePath();
        Assert.Equal(pathA, pathB.WithExtension(Ext.Zip));
    }

    [Fact]
    public void CanReplaceExtension()
    {
        var pathA = @"foo\dont_always_use_three_letter_names.zip".ToRelativePath();
        var pathB = @"foo\dont_always_use_three_letter_names.json".ToRelativePath();
        Assert.Equal(pathB, pathA.ReplaceExtension(Ext.Json));
    }

    [Fact]
    public void CanJoinPaths()
    {
        var pathA = @"foo\bar\baz\quz".ToRelativePath();
        var pathB = @"foo".ToRelativePath();
        var pathC = @"bar\baz\quz".ToRelativePath();
        var pathD = @"quz".ToRelativePath();
        Assert.Equal(pathA, pathB.Join(pathC));
        Assert.Equal(pathA, pathB.Join(@"bar\baz", pathD));
        Assert.Throws<PathException>(() => pathB.Join(new object(), pathA));
    }

    [Fact]
    public void CanCheckInFolder()
    {
        var pathA = @"foo\bar\baz.zip".ToRelativePath();
        var pathB = @"foo\bar".ToRelativePath();
        var pathC = @"fOo\Bar".ToRelativePath();
        Assert.True(pathA.InFolder(pathB));
        Assert.True(pathA.InFolder(pathC));
        Assert.False(pathB.InFolder(pathA));
    }

    [Fact]
    public void PathsAreComparable()
    {
        var data = new[]
        {
            (RelativePath)@"a",
            (RelativePath)@"b\c",
            (RelativePath)@"d\e\f",
            (RelativePath)@"b"
        };
        var data2 = data.OrderBy(a => a).ToArray();

        var data3 = new[]
        {
            (RelativePath)@"a",
            (RelativePath)@"b",
            (RelativePath)@"b\c",
            (RelativePath)@"d\e\f"
        };
        Assert.Equal(data3, data2);
    }
}