using FluentAssertions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths.Tests;

public class ExtensionTests
{
    private readonly InMemoryFileSystem _fileSystem;

    public ExtensionTests()
    {
        _fileSystem = new InMemoryFileSystem();
    }

    // ReSharper disable InconsistentNaming
    private static readonly Extension DDS = new(".DDS");
    private static readonly Extension Dds = new(".Dds");
    private static readonly Extension DDS2 = new(".DDS");
    private static readonly Extension EMPTY = new("");
    // ReSharper restore InconsistentNaming

    [Fact]
    public void Test_Equality()
    {
        DDS.Should().Be(DDS);
        DDS.Should().Be(DDS2);
        DDS.Should().Be(Dds);

        EMPTY.Should().NotBe(DDS);
        DDS.Should().NotBe(42);
    }

    [Theory]
    [InlineData("foo.dds", ".dds")]
    [InlineData(".dds", ".dds")]
    [InlineData(".", "")]
    [InlineData("foo.bar.baz.dds", ".dds")]
    public void Test_FromPath(string input, string expectedExtension)
    {
        var actualExtension = Extension.FromPath(input);
        actualExtension.Should().Be(expectedExtension);
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
