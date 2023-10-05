namespace NexusMods.App.UI.WorkspaceSystem;

internal static class GridUtils
{
    /// <summary>
    /// Returns information about how to create new panels.
    /// </summary>
    internal static IEnumerable<PanelCreationInfo> GetAvailableCreationInfos(IReadOnlyList<IPanelViewModel> panels, int columns, int rows)
    {
        if (panels.Count == columns * rows) yield break;
        foreach (var panel in panels)
        {
            if (CanSplitVertically(panel, panels, columns))
            {
                var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(panel.LogicalBounds, vertical: true);
                yield return new PanelCreationInfo(panel, updatedLogicalBounds, newPanelLogicalBounds);

                if (updatedLogicalBounds != newPanelLogicalBounds)
                {
                    yield return new PanelCreationInfo(panel, newPanelLogicalBounds, updatedLogicalBounds);
                }
            }

            if (CanSplitHorizontally(panel, panels, rows))
            {
                var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(panel.LogicalBounds, vertical: false);
                yield return new PanelCreationInfo(panel, updatedLogicalBounds, newPanelLogicalBounds);

                if (updatedLogicalBounds != newPanelLogicalBounds)
                {
                    yield return new PanelCreationInfo(panel, newPanelLogicalBounds, updatedLogicalBounds);
                }
            }
        }
    }

    private static bool CanSplitVertically(IPanelViewModel panel, IReadOnlyList<IPanelViewModel> panels, int columns)
    {
        var count = 0;

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < panels.Count; i++)
        {
            var other = panels[i];
            if (Math.Abs(other.LogicalBounds.Y - panel.LogicalBounds.Y) < double.Epsilon) count++;
        }

        return count < columns;
    }

    private static bool CanSplitHorizontally(IPanelViewModel panel, IReadOnlyList<IPanelViewModel> panels, int rows)
    {
        var count = 0;

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < panels.Count; i++)
        {
            var other = panels[i];
            if (Math.Abs(other.LogicalBounds.X - panel.LogicalBounds.X) < double.Epsilon) count++;
        }

        return count < rows;
    }
}
