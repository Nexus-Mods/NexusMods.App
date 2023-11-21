using System.Diagnostics.CodeAnalysis;
using Avalonia;
using FluentAssertions;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.UI.Tests.WorkspaceSystem;

[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
public class MathUtilsTests
{
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 1.0)]
    public void Test_AsVector(double width, double height)
    {
        var size = new Size(width, height);
        var vec = size.AsVector();
        vec.X.Should().Be(width);
        vec.Y.Should().Be(height);
    }

    [Theory]
    [InlineData(0.0, 0.0, true)]
    [InlineData(0.25, 0.25, true)]
    [InlineData(0.5, 0.5, true)]
    [InlineData(1.0, 1.0, true)]
    [InlineData(0.0, 0.1, false)]
    [InlineData(0.25, 0.3, false)]
    [InlineData(0.5, 0.6, false)]
    [InlineData(0.9, 1.0, false)]
    public void Test_IsCloseTo(double left, double right, bool expected)
    {
        var res = left.IsCloseTo(right);
        res.Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(TestData_CalculateActualBounds))]
    public void Test_CalculateActualBounds(Size workspaceSize, Rect logicalBounds, Rect expected)
    {
        var actual = MathUtils.CalculateActualBounds(workspaceSize, logicalBounds);
        actual.Should().Be(expected);
    }

    public static IEnumerable<object[]> TestData_CalculateActualBounds() => new[]
    {
        new object[]{ new Size(100, 100), new Rect(0.0, 0.0, 1.0, 1.0), new Rect(0.0, 0.0, 100, 100) },
        new object[]{ new Size(100, 100), new Rect(0.0, 0.0, 0.5, 1.0), new Rect(0.0, 0.0, 50, 100) },
        new object[]{ new Size(100, 100), new Rect(0.0, 0.0, 1.0, 0.5), new Rect(0.0, 0.0, 100, 50) },
        new object[]{ new Size(100, 100), new Rect(0.0, 0.0, 0.5, 0.5), new Rect(0.0, 0.0, 50, 50) },

        new object[]{ new Size(100, 100), new Rect(0.5, 0.5, 0.5, 0.5), new Rect(50, 50, 50, 50) },
    };

    [Theory]
    [MemberData(nameof(TestData_Split))]
    public void Test_Split(Rect currentLogicalBounds, bool vertical, Rect updatedLogicalBounds, Rect newPanelLogicalBounds)
    {
        var tuple = MathUtils.Split(currentLogicalBounds, vertical);
        tuple.UpdatedLogicalBounds.Should().Be(updatedLogicalBounds);
        tuple.NewPanelLogicalBounds.Should().Be(newPanelLogicalBounds);
    }

    public static IEnumerable<object[]> TestData_Split() => new[]
    {
        new object[] { new Rect(0.0, 0.0, 100, 100), true, new Rect(0.0, 0.0, 50, 100), new Rect(50, 0.0, 50, 100) },
        new object[] { new Rect(0.0, 0.0, 100, 100), false, new Rect(0.0, 0.0, 100, 50), new Rect(0.0, 50, 100, 50) },
    };

    [Theory]
    [MemberData(nameof(TestData_GetMidPoint))]
    public void Test_GetMidPoint(Rect a, Rect b, bool isHorizontal, Vector expected)
    {
        var actual = MathUtils.GetMidPoint(a, b, isHorizontal);
        actual.Should().Be(expected);
    }

    public static IEnumerable<object[]> TestData_GetMidPoint() => new[]
    {
        new object[] { new Rect(0, 0, 0.5, 1.0), new Rect(0.5, 0, 0.5, 1.0), false, new Vector(0.5, 0.5) },
        new object[] { new Rect(0, 0, 1.0, 0.5), new Rect(0, 0.5, 1.0, 0.5), true, new Vector(0.5, 0.5) },

        new object[] { new Rect(0, 0, 0.5, 1.0), new Rect(0.5, 0, 0.5, 0.5), false, new Vector(0.5, 0.25) },
        new object[] { new Rect(0, 0, 0.5, 1.0), new Rect(0.5, 0.5, 0.5, 0.5), false, new Vector(0.5, 0.75) },

        new object[] { new Rect(0, 0, 1.0, 0.5), new Rect(0, 0.5, 0.5, 0.5), true, new Vector(0.25, 0.5) },
        new object[] { new Rect(0, 0, 1.0, 0.5), new Rect(0.5, 0.5, 0.5, 0.5), true, new Vector(0.75, 0.5) },
    };
}
