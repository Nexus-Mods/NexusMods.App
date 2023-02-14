using NexusMods.Paths.Extensions;

namespace NexusMods.Paths.Tests;

public class ArrayExtensionsTests
{
    [Fact]
    public void AreEqualTests()
    {
        Assert.True(ArrayExtensions.AreEqual(new[] { 1, 2, 3 }, 0, new[] { 1, 2 }, 0, 2));
        Assert.False(ArrayExtensions.AreEqual(new[] { 1, 2, 3 }, 0, new[] { 1, 2, 2 }, 0, 3));
        Assert.False(ArrayExtensions.AreEqual(new[] { 1, 2, 3 }, 0, new[] { 1, 2 }, 0, 3));
        Assert.False(ArrayExtensions.AreEqual(new[] { 1, 2 }, 1, new[] { 1, 2, 3 }, 0, 2));
    }

    [Fact]
    public void AreEqualIgnoreCaseTests()
    {
        Assert.True(ArrayExtensions.AreEqualIgnoreCase(new[] { "a", "b" }, 0, new[] { "A", "B" }, 0, 2));
        Assert.False(ArrayExtensions.AreEqualIgnoreCase(new[] { "a", "b", "c" }, 0, new[] { "A", "B", "Z" }, 0, 3));
        Assert.False(ArrayExtensions.AreEqualIgnoreCase(new[] { "a", "b", "C" }, 0, new[] { "A", "B" }, 0, 3));
        Assert.False(ArrayExtensions.AreEqualIgnoreCase(new[] { "a", "b" }, 0, new[] { "A", "B", "C" }, 0, 3));
    }


    [Fact]
    public void CompareTo()
    {
        Assert.Equal(0, ArrayExtensions.Compare(new[] { 1, 1 }, new[] { 1, 1 }));
        Assert.Equal(1, ArrayExtensions.Compare(new[] { 1, 1, 1 }, new[] { 1, 1 }));
        Assert.Equal(-1, ArrayExtensions.Compare(new[] { 1, 1 }, new[] { 1, 1, 1 }));
        Assert.Equal(1, ArrayExtensions.Compare(new[] { 1, 2 }, new[] { 1, 1, 1 }));

        Assert.Equal(0, ArrayExtensions.CompareString(new[] { "1", "1" }, new[] { "1", "1" }));
        Assert.Equal(1, ArrayExtensions.CompareString(new[] { "1", "1", "1" }, new[] { "1", "1" }));
        Assert.Equal(-1, ArrayExtensions.CompareString(new[] { "1", "1" }, new[] { "1", "1", "1" }));
        Assert.Equal(1, ArrayExtensions.CompareString(new[] { "1", "2" }, new[] { "1", "1", "1" }));
    }
}