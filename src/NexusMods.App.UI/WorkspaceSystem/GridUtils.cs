using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class GridUtils
{
    internal static bool IsPerfectGrid(ImmutableDictionary<PanelId, Rect> state)
    {
        var totalArea = 0.0;

        foreach (var kv in state)
        {
            var rect = kv.Value;
            if (rect.Left < 0.0 || rect.Right > 1.0 || rect.Top < 0.0 || rect.Bottom > 1.0)
            {
                throw new Exception($"Panel {kv.Key} is out of bounds: {rect}");
            }

            totalArea += rect.Height * rect.Width;

            foreach (var other in state)
            {
                if (kv.Key == other.Key) continue;

                if (rect.Intersects(other.Value))
                {
                    throw new Exception($"{kv.ToString()} intersects with {other.ToString()}");
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
    internal static IEnumerable<ImmutableDictionary<PanelId, Rect>> GetPossibleStates(
        ImmutableDictionary<PanelId, Rect> panels,
        int columns,
        int rows)
    {
        if (panels.Count == columns * rows) yield break;

        foreach (var kv in panels)
        {
            if (CanAddColumn(kv, panels, columns))
            {
                var res = CreateResult(panels, kv, vertical: true, inverse: false);
                yield return res;

                if (res.First().Value != res.Last().Value)
                    yield return CreateResult(panels, kv, vertical: true, inverse: true);
            }

            if (CanAddRow(kv, panels, rows))
            {
                var res = CreateResult(panels, kv, vertical: false, inverse: false);
                yield return res;

                if (res.First().Value != res.Last().Value)
                    yield return CreateResult(panels, kv, vertical: false, inverse: true);
            }
        }
    }

    private static ImmutableDictionary<PanelId, Rect> CreateResult(
        ImmutableDictionary<PanelId, Rect> currentPanels,
        KeyValuePair<PanelId, Rect> kv,
        bool vertical,
        bool inverse)
    {
        var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(kv.Value, vertical);

        if (inverse)
        {
            var res = currentPanels.SetItems(new []
            {
                new KeyValuePair<PanelId, Rect>(kv.Key, newPanelLogicalBounds),
                new KeyValuePair<PanelId, Rect>(PanelId.DefaultValue, updatedLogicalBounds)
            });

            return res;
        }
        else
        {
            var res = currentPanels.SetItems(new []
            {
                new KeyValuePair<PanelId, Rect>(kv.Key, updatedLogicalBounds),
                new KeyValuePair<PanelId, Rect>(PanelId.DefaultValue, newPanelLogicalBounds)
            });

            return res;
        }
    }

    private static bool CanAddColumn(
        KeyValuePair<PanelId, Rect> kv,
        ImmutableDictionary<PanelId, Rect> panels,
        int maxColumns)
    {
        var currentColumns = 0;
        var current = kv.Value;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var otherPair in panels)
        {
            var other = otherPair.Value;

            // NOTE(erri120): +1 column if another panel (self included) has the same Y position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.Y.IsCloseTo(current.Y))
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
            if (!other.Left.IsCloseTo(current.Right) && !other.Right.IsCloseTo(current.Left)) continue;

            // 2) check if the panel is in the current row
            if (other.Bottom.IsGreaterThanOrCloseTo(current.Y) || other.Top.IsLessThanOrCloseTo(current.Y))
                currentColumns++;
        }

        return currentColumns < maxColumns;
    }

    private static bool CanAddRow(
        KeyValuePair<PanelId, Rect> kv,
        ImmutableDictionary<PanelId, Rect> panels,
        int maxRows)
    {
        var currentRows = 0;
        var current = kv.Value;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var otherPair in panels)
        {
            var other = otherPair.Value;

            // NOTE(erri120): +1 column if another panel (self included) has the same X position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.X.IsCloseTo(current.X))
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
            if (!other.Top.IsCloseTo(current.Bottom) && !other.Bottom.IsCloseTo(current.Top)) continue;

            // 2) check if the panel is in the current column
            if (other.Right.IsGreaterThanOrCloseTo(current.X) || other.Left.IsLessThanOrCloseTo(current.X))
                currentRows++;
        }

        return currentRows < maxRows;
    }

    internal static ImmutableDictionary<PanelId, Rect> GetStateWithoutPanel(
        ImmutableDictionary<PanelId, Rect> currentState,
        PanelId panelToRemove,
        bool isHorizontal = true)
    {
        if (currentState.Count == 1) return ImmutableDictionary<PanelId, Rect>.Empty;

        var res = currentState.Remove(panelToRemove);
        if (res.Count == 1)
        {
            return new Dictionary<PanelId, Rect>
            {
                { res.First().Key, MathUtils.One }
            }.ToImmutableDictionary();
        }

        var currentRect = currentState[panelToRemove];

        Span<PanelId> sameColumn = stackalloc PanelId[currentState.Count];
        var sameColumnCount = 0;

        Span<PanelId> sameRow = stackalloc PanelId[currentState.Count];
        var sameRowCount = 0;

        foreach (var kv in res)
        {
            var (id, rect) = kv;

            // same column
            // | a | x |  | b | x |
            // | b | x |  | a | x |
            if (rect.Left.IsGreaterThanOrCloseTo(currentRect.Left) && rect.Right.IsLessThanOrCloseTo(currentRect.Right))
            {
                if (rect.Top.IsCloseTo(currentRect.Bottom) || rect.Bottom.IsCloseTo(currentRect.Top))
                {
                    sameColumn[sameColumnCount++] = id;
                }
            }

            // same row
            // | a | b |  | b | a |  | a | b |
            // | x | x |  | x | x |  | a | c |
            if (rect.Top.IsGreaterThanOrCloseTo(currentRect.Top) && rect.Bottom.IsLessThanOrCloseTo(currentRect.Bottom))
            {
                if (rect.Left.IsCloseTo(currentRect.Right) || rect.Right.IsCloseTo(currentRect.Left))
                {
                    sameRow[sameRowCount++] = id;
                }
            }
        }

        Debug.Assert(sameColumnCount > 0 || sameRowCount > 0);

        if (isHorizontal)
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

    private static ImmutableDictionary<PanelId, Rect> JoinSameColumn(
        ImmutableDictionary<PanelId, Rect> res,
        Rect currentRect,
        Span<PanelId> sameColumn,
        int sameColumnCount)
    {
        var updates = GC.AllocateUninitializedArray<KeyValuePair<PanelId, Rect>>(sameColumnCount);

        for (var i = 0; i < sameColumnCount; i++)
        {
            var id = sameColumn[i];
            var rect = res[id];

            var x = rect.X;
            var width = rect.Width;

            var y = Math.Min(rect.Y, currentRect.Y);
            var height = rect.Height + currentRect.Height;

            updates[i] = new KeyValuePair<PanelId, Rect>(id, new Rect(x, y, width, height));
        }

        return res.SetItems(updates);
    }

    private static ImmutableDictionary<PanelId, Rect> JoinSameRow(
        ImmutableDictionary<PanelId, Rect> res,
        Rect currentRect,
        Span<PanelId> sameRow,
        int sameRowCount)
    {
        var updates = GC.AllocateUninitializedArray<KeyValuePair<PanelId, Rect>>(sameRowCount);

        for (var i = 0; i < sameRowCount; i++)
        {
            var id = sameRow[i];
            var rect = res[id];

            var y = rect.Y;
            var height = rect.Height;

            var x = Math.Min(rect.X, currentRect.X);
            var width = rect.Width + currentRect.Width;

            updates[i] = new KeyValuePair<PanelId, Rect>(id, new Rect(x, y, width, height));
        }

        return res.SetItems(updates);
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
