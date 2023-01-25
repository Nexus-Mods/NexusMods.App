using FluentAssertions;

namespace NexusMods.Paths.Tests;

public class SizeTests
{
    [Fact]
    public void MathAndSize()
    {
        var a = (Size)10L;
        var b = Size.From(20L);

        a.Should().BeLessThan(b);
        b.Should().BeGreaterThan(a);

        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();

        (b / a).Should().Be(2L);
        (b * 20).Should().Be(Size.From(400));

        (b > a).Should().BeTrue();
        (a < b).Should().BeTrue();
        (b <= a).Should().BeFalse();
        (a >= b).Should().BeFalse();
        (b - a).Should().Be(a);

        Size.Zero.Should().Be(Size.Zero);
        Size.MultiplicativeIdentity.Should().Be(Size.One);

        a.ToString().Should().Be("10 B");

        ((Size)1L).Readable().Should().Be("1 B");
        ((Size)1024L).Readable().Should().Be("1 KB");
        ((Size)1024L * 1024L).Readable().Should().Be("1 MB");
        ((Size)1024L * 1024L * 1024L).Readable().Should().Be("1 GB");
        ((Size)1024L * 1024L * 1024L * 1024L).Readable().Should().Be("1 TB");
        ((Size)1024L * 1024L * 1024L * 1024L * 1024L).Readable().Should().Be("1 PB");
        ((Size)1024L * 1024L * 1024L * 1024L * 1024L * 1024L).Readable().Should().Be("1 EB");

    }
}