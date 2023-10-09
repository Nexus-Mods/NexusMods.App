using System.Collections.Immutable;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class GridUtils
{
    /// <summary>
    /// Returns all possible new states.
    /// </summary>
    internal static IEnumerable<IReadOnlyDictionary<PanelId, Rect>> GetPossibleStates(IReadOnlyList<IPanelViewModel> panels, int columns, int rows)
    {
        if (panels.Count == columns * rows) yield break;
        var currentPanels = panels.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        foreach (var panel in panels)
        {
            if (CanSplitVertically(panel, panels, columns))
            {
                var res = CreateResult(currentPanels, panel, vertical: true, inverse: false);
                yield return res;

                if (res.First().Value != res.Last().Value)
                    yield return CreateResult(currentPanels, panel, vertical: true, inverse: true);
            }

            if (CanSplitHorizontally(panel, panels, columns))
            {
                var res = CreateResult(currentPanels, panel, vertical: false, inverse: false);
                yield return res;

                if (res.First().Value != res.Last().Value)
                    yield return CreateResult(currentPanels, panel, vertical: false, inverse: true);
            }
        }
    }

    private static IReadOnlyDictionary<PanelId, Rect> CreateResult(ImmutableDictionary<PanelId, Rect> currentPanels, IPanelViewModel panel, bool vertical, bool inverse)
    {
        var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(panel.LogicalBounds, vertical);

        if (inverse)
        {
            var res = currentPanels.SetItems(new []
            {
                new KeyValuePair<PanelId, Rect>(panel.Id, newPanelLogicalBounds),
                new KeyValuePair<PanelId, Rect>(PanelId.Empty, updatedLogicalBounds)
            });

            return res;
        }
        else
        {
            var res = currentPanels.SetItems(new []
            {
                new KeyValuePair<PanelId, Rect>(panel.Id, updatedLogicalBounds),
                new KeyValuePair<PanelId, Rect>(PanelId.Empty, newPanelLogicalBounds)
            });

            return res;
        }
    }

    private static bool CanSplitVertically(IPanelViewModel panel, IReadOnlyList<IPanelViewModel> panels, int maxColumns)
    {
        var currentColumns = 0;

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < panels.Count; i++)
        {
            var other = panels[i];

            // NOTE(erri120): +1 column if another panel (self included) has the same Y position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.LogicalBounds.Y.TolerantEquals(panel.LogicalBounds.Y))
            {
                currentColumns++;
                continue;
            }

            // NOTE(erri120): See the example tables below. If the current panel is "3"
            // we need to count 2 columns. With the Y check above, this is not possible,
            // since the panel "2" in the first table and the panel "1" in the second table
            // start above but go down and end next to panel "3".
            // | 1 | 2 |  | 1 | 2 |
            // | 3 | 2 |  | 1 | 3 |

            // 1) check if the panel is next to us
            if (!other.LogicalBounds.Left.TolerantEquals(panel.LogicalBounds.Right) &&
                !other.LogicalBounds.Right.TolerantEquals(panel.LogicalBounds.Left)) continue;

            // 2) check if the panel is in the current row
            if (other.LogicalBounds.Bottom >= panel.LogicalBounds.Y ||
                other.LogicalBounds.Top <= panel.LogicalBounds.Y)
                currentColumns++;
        }

        return currentColumns < maxColumns;
    }

    private static bool CanSplitHorizontally(IPanelViewModel panel, IReadOnlyList<IPanelViewModel> panels, int maxRows)
    {
        var currentRows = 0;

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < panels.Count; i++)
        {
            var other = panels[i];

            // NOTE(erri120): +1 column if another panel (self included) has the same X position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.LogicalBounds.X.TolerantEquals(panel.LogicalBounds.X))
            {
                currentRows++;
                continue;
            }

            // NOTE(erri120): See the example tables below. If the current panel is "3"
            // we need to count 2 rows. With the X check above, this is not possible,
            // since the panel "2" in the first table and the panel "1" in the second table
            // extend above and below to panel "3".
            // | 1 | 3 |  | 1 | 1 |
            // | 2 | 2 |  | 2 | 3 |

            // 1) check if the panel is above or below us
            if (!other.LogicalBounds.Top.TolerantEquals(panel.LogicalBounds.Bottom) &&
                !other.LogicalBounds.Bottom.TolerantEquals(panel.LogicalBounds.Top)) continue;

            // 2) check if the panel is in the current column
            if (other.LogicalBounds.Right >= panel.LogicalBounds.X ||
                other.LogicalBounds.Left <= panel.LogicalBounds.X)
                currentRows++;
        }

        return currentRows < maxRows;
    }

    internal static IReadOnlyDictionary<PanelId, Rect> GetStateWithoutPanel(
        IImmutableDictionary<PanelId, Rect> currentState,
        PanelId panelToRemove)
    {
        return currentState;
    }
}
