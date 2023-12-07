using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class GridUtils
{
    /// <summary>
    /// Checks whether the given state is a perfect grid.
    /// </summary>
    /// <remarks>
    /// A perfect has no gaps, and no panel is out-of-bounds.
    /// </remarks>
    /// <exception cref="Exception">Thrown when the grid is not perfect.</exception>
    internal static bool IsPerfectGrid(WorkspaceGridState gridState)
    {
        var totalArea = 0.0;

        foreach (var panelState in gridState)
        {
            var (id, rect) = panelState;
            if (rect.Left < 0.0 || rect.Right > 1.0 || rect.Top < 0.0 || rect.Bottom > 1.0)
            {
                throw new Exception($"Panel {panelState} is out of bounds");
            }

            totalArea += rect.Height * rect.Width;

            foreach (var other in gridState)
            {
                if (id == other.Id) continue;

                if (rect.Intersects(other.Rect))
                {
                    throw new Exception($"{panelState.ToString()} intersects with {other.ToString()}");
                }
            }
        }

        if (!totalArea.IsCloseTo(1.0))
        {
            throw new Exception($"Area of {totalArea} doesn't match 1.0");
        }

        return true;
    }

    /// <summary>
    /// Returns all possible new states.
    /// </summary>
    internal static IEnumerable<WorkspaceGridState> GetPossibleStates(
        WorkspaceGridState workspaceState,
        int columns,
        int rows)
    {
        if (workspaceState.Count == columns * rows) yield break;

        foreach (var panelState in workspaceState)
        {
            if (CanAddColumn(workspaceState, panelState, columns))
            {
                var res = CreateResult(workspaceState, panelState, vertical: true, inverse: false);
                yield return res;

                if (res[0].Id != res[^1].Id)
                    yield return CreateResult(workspaceState, panelState, vertical: true, inverse: true);
            }

            if (CanAddRow(workspaceState, panelState, rows))
            {
                var res = CreateResult(workspaceState, panelState, vertical: false, inverse: false);
                yield return res;

                if (res[0].Id != res[^1].Id)
                    yield return CreateResult(workspaceState, panelState, vertical: false, inverse: true);
            }
        }
    }

    private static WorkspaceGridState CreateResult(
        WorkspaceGridState workspaceState,
        PanelGridState panelState,
        bool vertical,
        bool inverse)
    {
        var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(panelState.Rect, vertical);

        Span<PanelGridState> updatedValues = stackalloc PanelGridState[2];
        if (inverse)
        {
            updatedValues[0] = new PanelGridState(PanelId.DefaultValue, updatedLogicalBounds);
            updatedValues[1] = panelState with { Rect = newPanelLogicalBounds };
        }
        else
        {
            updatedValues[0] = panelState with { Rect = updatedLogicalBounds };
            updatedValues[1] = new PanelGridState(PanelId.DefaultValue, newPanelLogicalBounds);
        }

        var res = workspaceState.UnionById(updatedValues);
        return res;
    }

    private static bool CanAddColumn(
        WorkspaceGridState workspaceState,
        PanelGridState panelState,
        int maxColumns)
    {
        var numColumns = 0;
        var current = panelState.Rect;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var otherPanel in workspaceState)
        {
            var other = otherPanel.Rect;

            // NOTE(erri120): +1 column if another panel (self included) has the same Y position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.Y.IsCloseTo(current.Y))
            {
                numColumns++;
                continue;
            }

            // NOTE(erri120): See the example tables below. If the current panel is "3"
            // we need to count 2 columns. With the Y check above, this is not possible,
            // since the panel "2" in the first table and the panel "1" in the second table
            // start above but go down and end next to panel "3".
            // | 1 | 2 |  | 1 | 2 |
            // | 3 | 2 |  | 1 | 3 |

            // 1) check if the panel is next to us
            if (!other.Left.IsCloseTo(current.Right) && !other.Right.IsCloseTo(current.Left)) continue;

            // 2) check if the panel is in the current row
            if (other.Bottom.IsGreaterThanOrCloseTo(current.Y) || other.Top.IsLessThanOrCloseTo(current.Y))
                numColumns++;
        }

        return numColumns < maxColumns;
    }

    private static bool CanAddRow(
        WorkspaceGridState workspaceState,
        PanelGridState panelState,
        int maxRows)
    {
        var numRows = 0;
        var current = panelState.Rect;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var otherPanel in workspaceState)
        {
            var other = otherPanel.Rect;

            // NOTE(erri120): +1 column if another panel (self included) has the same X position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.X.IsCloseTo(current.X))
            {
                numRows++;
                continue;
            }

            // NOTE(erri120): See the example tables below. If the current panel is "3"
            // we need to count 2 rows. With the X check above, this is not possible,
            // since the panel "2" in the first table and the panel "1" in the second table
            // extend above and below to panel "3".
            // | 1 | 3 |  | 1 | 1 |
            // | 2 | 2 |  | 2 | 3 |

            // 1) check if the panel is above or below us
            if (!other.Top.IsCloseTo(current.Bottom) && !other.Bottom.IsCloseTo(current.Top)) continue;

            // 2) check if the panel is in the current column
            if (other.Right.IsGreaterThanOrCloseTo(current.X) || other.Left.IsLessThanOrCloseTo(current.X))
                numRows++;
        }

        return numRows < maxRows;
    }

    internal static WorkspaceGridState GetStateWithoutPanel(
        WorkspaceGridState gridState,
        PanelId panelToRemove)
    {
        if (gridState.Count == 1) return WorkspaceGridState.Empty(gridState.IsHorizontal);

        var res = gridState.Remove(gridState[panelToRemove]);
        if (res.Count == 1) return WorkspaceGridState.Empty(gridState.IsHorizontal).Add(new PanelGridState(res[0].Id, MathUtils.One));

        var panelState = gridState[panelToRemove];
        var currentRect = panelState.Rect;

        Span<PanelId> sameColumn = stackalloc PanelId[gridState.Count];
        var sameColumnCount = 0;

        Span<PanelId> sameRow = stackalloc PanelId[gridState.Count];
        var sameRowCount = 0;

        foreach (var adjacent in res.EnumerateAdjacentPanels(panelState, includeAnchor: true))
        {
            if ((adjacent.Kind & WorkspaceGridState.AdjacencyKind.SameColumn) == WorkspaceGridState.AdjacencyKind.SameColumn)
                sameColumn[sameColumnCount++] = adjacent.Panel.Id;
            if ((adjacent.Kind & WorkspaceGridState.AdjacencyKind.SameRow) == WorkspaceGridState.AdjacencyKind.SameRow)
                sameRow[sameRowCount++] = adjacent.Panel.Id;
        }

        Debug.Assert(sameColumnCount > 0 || sameRowCount > 0);

        if (gridState.IsHorizontal)
        {
            // prefer columns over rows when horizontal
            if (sameColumnCount > 0)
            {
                res = JoinSameColumn(res, currentRect, sameColumn, sameColumnCount);
            } else if (sameRowCount > 0)
            {
                res = JoinSameRow(res, currentRect, sameRow, sameRowCount);
            }
        }
        else
        {
            // prefer rows over columns when vertical
            if (sameRowCount > 0)
            {
                res = JoinSameRow(res, currentRect, sameRow, sameRowCount);
            } else if (sameColumnCount > 0)
            {
                res = JoinSameColumn(res, currentRect, sameColumn, sameColumnCount);
            }
        }

        return res;
    }

    private static WorkspaceGridState JoinSameColumn(
        WorkspaceGridState res,
        Rect currentRect,
        Span<PanelId> sameColumn,
        int sameColumnCount)
    {
        var updates = GC.AllocateUninitializedArray<PanelGridState>(sameColumnCount);

        for (var i = 0; i < sameColumnCount; i++)
        {
            var id = sameColumn[i];
            var panel = res[id];
            var rect = panel.Rect;

            var x = rect.X;
            var width = rect.Width;

            var y = Math.Min(rect.Y, currentRect.Y);
            var height = rect.Height + currentRect.Height;

            updates[i] = new PanelGridState(id, new Rect(x, y, width, height));
        }

        return res.UnionById(updates);
    }

    private static WorkspaceGridState JoinSameRow(
        WorkspaceGridState res,
        Rect currentRect,
        Span<PanelId> sameRow,
        int sameRowCount)
    {
        var updates = GC.AllocateUninitializedArray<PanelGridState>(sameRowCount);

        for (var i = 0; i < sameRowCount; i++)
        {
            var id = sameRow[i];
            var panel = res[id];
            var rect = panel.Rect;

            var y = rect.Y;
            var height = rect.Height;

            var x = Math.Min(rect.X, currentRect.X);
            var width = rect.Width + currentRect.Width;

            updates[i] = new PanelGridState(id, new Rect(x, y, width, height));
        }

        return res.UnionById(updates);
    }

    internal static IReadOnlyList<ResizerInfo> GetResizers(
        ImmutableDictionary<PanelId, Rect> currentState,
        bool isWorkspaceHorizontal = true)
    {
        var tmp = new List<ResizerInfo>(capacity: currentState.Count);
        var adjacentPanels = new List<ResizerInfo>(capacity: currentState.Count);

        // Step 1: fill the tmp List with all Resizers
        foreach (var current in currentState)
        {
            var currentRect = current.Value;

            // Step 1.1: go through all panels and find their adjacent panels
            foreach (var other in currentState)
            {
                if (other.Key == current.Key) continue;

                var rect = other.Value;

                // same column
                // | a | x |  | b | x |
                // | b | x |  | a | x |
                if (rect.Left >= currentRect.Left && rect.Right <= currentRect.Right)
                {
                    if (rect.Top.IsCloseTo(currentRect.Bottom) || rect.Bottom.IsCloseTo(currentRect.Top))
                    {
                        Add(adjacentPanels, current, other, isHorizontal: true);
                    }
                }

                // same row
                // | a | b |  | b | a |  | a | b |
                // | x | x |  | x | x |  | a | c |
                if (rect.Top >= currentRect.Top && rect.Bottom <= currentRect.Bottom)
                {
                    if (rect.Left.IsCloseTo(currentRect.Right) || rect.Right.IsCloseTo(currentRect.Left))
                    {
                        Add(adjacentPanels, current, other, isHorizontal: false);
                    }
                }
            }

            // Step 1.2: combine the resizers for the current panel
            foreach (var info in adjacentPanels)
            {
                var pos = info.LogicalPosition;

                var acc = new List<PanelId>();
                acc.AddRange(info.ConnectedPanels);

                foreach (var other in adjacentPanels)
                {
                    if (other.IsHorizontal != info.IsHorizontal) continue;

                    var otherPos = other.LogicalPosition;
                    if (pos == otherPos) continue;
                    if (!pos.X.IsCloseTo(otherPos.X) && !pos.Y.IsCloseTo(otherPos.Y)) continue;

                    acc.AddRange(other.ConnectedPanels.Where(id => !acc.Contains(id)));
                }

                tmp.Add(info with { ConnectedPanels = acc.ToArray() });
            }

            adjacentPanels.Clear();
        }

        // Step 2: combine all resizers of all panels
        var res = tmp
            .DistinctBy(info => info.LogicalPosition)
            .GroupBy(info => isWorkspaceHorizontal ? info.LogicalPosition.X : info.LogicalPosition.Y)
            .SelectMany(group =>
            {
                var connectedPanels = group.SelectMany(info => info.ConnectedPanels).Distinct().ToArray();
                return group.Select(info => info with { ConnectedPanels = connectedPanels });
            }).ToArray();

        return res;

        static void Add(ICollection<ResizerInfo> list, KeyValuePair<PanelId, Rect> current, KeyValuePair<PanelId, Rect> other, bool isHorizontal)
        {
            var (currentId, currentRect) = current;
            var (otherId, rect) = other;

            var pos = MathUtils.GetMidPoint(currentRect, rect, isHorizontal);
            list.Add(new ResizerInfo(IsHorizontal: isHorizontal, pos, new[] { currentId, otherId }));
        }
    }

    internal record struct ResizerInfo(bool IsHorizontal, Point LogicalPosition, PanelId[] ConnectedPanels);
}
