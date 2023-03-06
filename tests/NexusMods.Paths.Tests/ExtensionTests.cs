using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class ExtensionTests
{
    // ReSharper disable InconsistentNaming
    public static Extension DDS = new(".DDS");
    public static Extension Dds = new(".Dds");
    public static Extension DDS2 = new(".DDS");
    public static Extension EMPTY = new("");
    // ReSharper restore InconsistentNaming

    [Fact]
    public void ExtensionsAreEqual()
    {
        Assert.Equal(DDS, DDS);
        Assert.Equal(DDS, DDS2);
        Assert.Equal(DDS, Dds);

        Assert.True(DDS == Dds);
        Assert.True(DDS != EMPTY);

        Assert.NotEqual(EMPTY, DDS);

        Assert.NotEqual(DDS, (object)42);
    }

    [Fact]
    public void CanGetExtensionOfPath()
    {
        Assert.Equal(DDS, (@"c:\foo\bar.dds".ToAbsolutePath()).Extension);
    }

    [Fact]
    public void ExtensionsHaveConversionOperators()
    {
        Assert.True(".DDS" == (string)DDS);
        Assert.True(DDS == (Extension)".DDs");
    }

    [Fact]
    public void ExtensionsRequireDots()
    {
        Assert.Throws<PathException>(() => new Extension("foo"));
    }

    [Fact]
    public void ExtensionsOverrideObjectMethods()
    {
        Assert.Equal(".DDS", DDS.ToString());
        Assert.Equal(".DDS".GetHashCode(StringComparison.InvariantCultureIgnoreCase), DDS.GetHashCode());
    }

    [Fact]
    public void CanGetExtensionFromPath()
    {
        Assert.Equal(DDS, Extension.FromPath("myfoo.DDS"));
        Assert.Equal(DDS, Extension.FromPath("myfoo.bar.DDS"));
        Assert.Equal(EMPTY, Extension.FromPath("baz"));
    }
}
