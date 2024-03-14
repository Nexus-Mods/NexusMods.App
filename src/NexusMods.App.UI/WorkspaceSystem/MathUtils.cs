using System.Runtime.CompilerServices;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class MathUtils
{
    /// <summary>
    /// <see cref="Size"/> with a width and height of zero.
    /// </summary>
    internal static readonly Size Zero = new(width: 0.0, height: 0.0);

    /// <summary>
    /// <see cref="Rect"/> with a width and height of <c>1.0</c> and x
    /// and y of zero.
    /// </summary>
    internal static readonly Rect One = new(
        x: 0.0,
        y: 0.0,
        width: 1.0,
        height: 1.0
    );

    /// <summary>
    /// Calculates the actual bounds of a panel inside a workspace using it's logical bounds.
    /// </summary>
    /// <seealso cref="IPanelViewModel.LogicalBounds"/>
    /// <seealso cref="IPanelViewModel.ActualBounds"/>
    internal static Rect CalculateActualBounds(Size workspaceSize, Rect logicalBounds)
    {
        return logicalBounds * workspaceSize.AsVector();
    }

    /// <summary>
    /// Calculates the logical bounds for the existing panel and the new panel that is going
    /// to take up half of the existing panels' space.
    /// </summary>
    internal static (Rect UpdatedLogicalBounds, Rect NewPanelLogicalBounds) Split(Rect currentLogicalBounds, bool vertical)
    {
        Rect newPanelLogicalBounds;
        Rect updatedLogicalBounds;

        if (vertical)
        {
            var newWidth = currentLogicalBounds.Width / 2;

            updatedLogicalBounds = currentLogicalBounds.WithWidth(newWidth);
            newPanelLogicalBounds = updatedLogicalBounds.WithX(currentLogicalBounds.X + newWidth);
        }
        else
        {
            var newHeight = currentLogicalBounds.Height / 2;

            updatedLogicalBounds = currentLogicalBounds.WithHeight(newHeight);
            newPanelLogicalBounds = updatedLogicalBounds.WithY(currentLogicalBounds.Y + newHeight);
        }

        return (updatedLogicalBounds, newPanelLogicalBounds);
    }

    internal static (Point Start, Point End) GetResizerPoints(Rect a, Rect b, WorkspaceGridState.AdjacencyKind adjacencyKind)
    {
        var isSameRow = (adjacencyKind & WorkspaceGridState.AdjacencyKind.SameRow) == WorkspaceGridState.AdjacencyKind.SameRow;

        if (isSameRow)
        {
            var x = a.Right.IsCloseTo(b.Left) ? a.Right : b.Right;
            var yStart = a.Top.IsLessThanOrCloseTo(b.Top) ? a.Top : b.Top;
            var yEnd = a.Bottom.IsGreaterThanOrCloseTo(b.Bottom) ? a.Bottom : b.Bottom;

            x = Math.Min(x, 1.0);
            yStart = Math.Max(yStart, 0.0);
            yEnd = Math.Min(yEnd, 1.0);

            return (new Point(x, yStart), new Point(x, yEnd));
        }

        var y = a.Bottom.IsCloseTo(b.Top) ? a.Bottom : b.Bottom;
        var xStart = a.Left.IsLessThanOrCloseTo(b.Left) ? a.Left : b.Left;
        var xEnd = a.Right.IsGreaterThanOrCloseTo(b.Right) ? a.Right : b.Right;

        y = Math.Min(y, 1.0);
        xStart = Math.Max(xStart, 0.0);
        xEnd = Math.Min(xEnd, 1.0);

        return (new Point(xStart, y), new Point(xEnd, y));
    }

    /// <summary>
    /// Converts a <see cref="Size"/> into a <see cref="Vector"/>.
    /// </summary>
    internal static Vector AsVector(this Size size) => new(x: size.Width, y: size.Height);

    private const double DefaultTolerance = 0.001;

    /// <summary>
    /// Tolerant equality check.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsCloseTo(this double left, double right, double tolerance = DefaultTolerance)
    {
        if (double.IsInfinity(left) || double.IsInfinity(right))
            return double.IsInfinity(left) && double.IsInfinity(right);

        var delta = left - right;
        return -tolerance < delta && tolerance > delta;
    }

    /// <summary>
    /// Tolerant equality check.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsCloseTo(this Point left, Point right, double tolerance = DefaultTolerance)
    {
        return left.X.IsCloseTo(right.X, tolerance: tolerance) && left.Y.IsCloseTo(right.Y, tolerance: tolerance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsGreaterThanOrCloseTo(this double left, double right, double tolerance = DefaultTolerance)
    {
        return left > right || IsCloseTo(left, right, tolerance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsLessThanOrCloseTo(this double left, double right, double tolerance = DefaultTolerance)
    {
        return left < right || IsCloseTo(left, right, tolerance);
    }
}
