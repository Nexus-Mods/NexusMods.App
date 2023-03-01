using NexusMods.Paths.Extensions;
using NexusMods.Paths.Tests.New.Helpers;

namespace NexusMods.Paths.Tests.New.AbsolutePathTests;

/// <summary>
/// Tests for utility methods.
/// </summary>
public class UtilityTests
{
    [Theory]
    [InlineData("nya/neko/nyan", "nya/neko/nyan/nyanya")]
    public void InFolder(string parent, string child)
    {
        var parentPath = AbsolutePath.FromFullPath(parent.NormalizeSeparator());
        var childPath = AbsolutePath.FromFullPath(child.NormalizeSeparator());
        Assert.True(childPath.InFolder(parentPath));
    }
    
    [Theory]
    [InlineData("Desktop/Cat.png", "/home/sewer", "/home/sewer/Desktop/Cat.png")]
    [InlineData("/home/sewer/Desktop/Cat.png", "", "/home/sewer/Desktop/Cat.png")]
    public void RelativeTo(string expected, string parent, string child)
    {
        child = child.NormalizeSeparator();
        parent = parent.NormalizeSeparator();
        expected = expected.NormalizeSeparator();
        Assert.Equal(expected, child.ToAbsolutePath().RelativeTo(parent.ToAbsolutePath()).ToString());
    }
    
    [Theory]
    [InlineData("nya", "nya/neko/nyan/nyanya")]
    public void TopParent(string expected, string item)
    {
        Assert.Equal(expected, AbsolutePath.FromFullPath(item.NormalizeSeparator()).TopParent.GetFullPath());
    }
}