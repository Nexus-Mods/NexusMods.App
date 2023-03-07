namespace NexusMods.Paths.Tests.New.RelativePathTests;

public class SortingTests
{
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
