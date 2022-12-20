using FluentAssertions;

namespace NexusMods.Paths.Tests;

public class SizeTests
{
    [Fact]
    public void MathAndSize()
    {
        var a = (Size)10L;
        Size b = 20L;

        a.Should().BeLessThan(b);
        b.Should().BeGreaterThan(a);

        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();

        (b / a).Should().Be(2L);
        (b * a).Should().Be(200L);

        (b > a).Should().BeTrue();
        (a < b).Should().BeTrue();
        (b <= a).Should().BeFalse();
        (a >= b).Should().BeFalse();
        (b - a).Should().Be(a);

        Size.Zero.Should().Be(0L);
        Size.MultiplicativeIdentity.Should().Be(1L);

        a.ToString().Should().Be("10 B");

        ((Size)1L).Readable().Should().Be("1 B");
        ((Size)1024L).Readable().Should().Be("1 KB");
        ((Size)1024L * 1024L).Readable().Should().Be("1 MB");
        ((Size)1024L * 1024L * 1024L).Readable().Should().Be("1 GB");
        ((Size)1024L * 1024L * 1024L * 1024L).Readable().Should().Be("1 TB");
        ((Size)1024L * 1024L * 1024L * 1024L * 1024L).Readable().Should().Be("1 PB");
        ((Size)1024L * 1024L * 1024L * 1024L * 1024L * 1024L).Readable().Should().Be("1 EB");

        long lsize = 10;
        Size ssize = lsize;
        lsize.Should().Be(ssize);

    }
}