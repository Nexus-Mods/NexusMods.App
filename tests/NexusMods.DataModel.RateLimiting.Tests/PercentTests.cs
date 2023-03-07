using FluentAssertions;

namespace NexusMods.DataModel.RateLimiting.Tests;

public class PercentTests
{
    [Fact]
    public void CanCreateAndComparePercents()
    {
        var p1 = Percent.FactoryPutInRange(0.50);
        p1.Should().BeEquivalentTo(Percent.FactoryPutInRange(0.50));
        p1.ToString().Should().Be("50%");
        p1.Value.Should().Be(0.5);
        ((double)p1).Should().Be(0.5);

        Percent.FactoryPutInRange(1.1).Should().Be(Percent.FactoryPutInRange(1.0));
        Percent.FactoryPutInRange(-1).Should().Be(Percent.FactoryPutInRange(0.0));

        var p0 = Percent.Zero;
        var p2 = Percent.One;

        (p1 > p0).Should().BeTrue();
        (p1 > p2).Should().BeFalse();

        (p1 < p0).Should().BeFalse();
        (p1 < p2).Should().BeTrue();


        (p0 == p2).Should().BeFalse();
        (p0 != p2).Should().BeTrue();

        (p0 <= p1).Should().BeTrue();

        (p1 >= p0).Should().BeTrue();

        new[] { p1, p0, p2 }.Order().Should().BeEquivalentTo(new[] { p0, p1, p2 });
    }

    [Fact]
    public void CanHashPercents()
    {
        var p0 = Percent.FactoryPutInRange(0.5);
        var p1 = Percent.FactoryPutInRange(0.3);

        p0.GetHashCode().Should().Be(p0.GetHashCode());
        p1.GetHashCode().Should().Be(p1.GetHashCode());
        p0.GetHashCode().Should().NotBe(p1.GetHashCode());
    }

    [Fact]
    public void CanPerformMathOnPercents()
    {
        var p0 = Percent.FactoryPutInRange(0.5);
        var p2 = Percent.FactoryPutInRange(0.25);
        (p0 + p2).Should().Be(Percent.FactoryPutInRange(0.75));
        (p0 - p2).Should().Be(Percent.FactoryPutInRange(0.25));
    }

    [Fact]
    public void SupportsOtherConstructors()
    {
        Percent.FactoryPutInRange(1, 2).Value.Should().Be(0.5);
        Percent.FactoryPutInRange(1L, 2L).Value.Should().Be(0.5);
    }

    [Fact]
    public void CanRoundTripThroughString()
    {
        Percent.FactoryPutInRange(0.33333).ToString(3).Should().Be("33.333%");
        Percent.TryParse("3.33", out var parsed).Should().BeTrue();
        parsed.Value.Should().Be(0.0333);

    }

    [Fact]
    public void CanGetInverse()
    {
        Percent.FactoryPutInRange(0.75).Inverse.Value.Should().Be(0.25);
        Percent.FactoryPutInRange(0.25).Inverse.Value.Should().Be(0.75);
    }

    [Fact]
    public void CantCreatePercentOutOfRange()
    {
        this.Invoking(_ => new Percent(2.0))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("Element out of range: 2");
    }
}
