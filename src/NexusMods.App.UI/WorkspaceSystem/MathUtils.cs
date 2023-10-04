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

    private static Vector AsVector(this Size size) => new(x: size.Width, y: size.Height);
}
