using System.Collections.Immutable;
using System.Diagnostics;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

internal static class GridUtils
{
    /// <summary>
    /// Returns all possible new states.
    /// </summary>
    internal static IEnumerable<ImmutableDictionary<PanelId, Rect>> GetPossibleStates(IReadOnlyList<IPanelViewModel> panels, int columns, int rows)
    {
        if (panels.Count == columns * rows) yield break;
        var currentPanels = panels.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);

        foreach (var panel in panels)
        {
            if (CanAddColumn(panel, panels, columns))
            {
                var res = CreateResult(currentPanels, panel, vertical: true, inverse: false);
                yield return res;

                if (res.First().Value != res.Last().Value)
                    yield return CreateResult(currentPanels, panel, vertical: true, inverse: true);
            }

            if (CanAddRow(panel, panels, rows))
            {
                var res = CreateResult(currentPanels, panel, vertical: false, inverse: false);
                yield return res;

                if (res.First().Value != res.Last().Value)
                    yield return CreateResult(currentPanels, panel, vertical: false, inverse: true);
            }
        }
    }

    private static ImmutableDictionary<PanelId, Rect> CreateResult(ImmutableDictionary<PanelId, Rect> currentPanels, IPanelViewModel panel, bool vertical, bool inverse)
    {
        var (updatedLogicalBounds, newPanelLogicalBounds) = MathUtils.Split(panel.LogicalBounds, vertical);

        if (inverse)
        {
            var res = currentPanels.SetItems(new []
            {
                new KeyValuePair<PanelId, Rect>(panel.Id, newPanelLogicalBounds),
                new KeyValuePair<PanelId, Rect>(PanelId.DefaultValue, updatedLogicalBounds)
            });

            return res;
        }
        else
        {
            var res = currentPanels.SetItems(new []
            {
                new KeyValuePair<PanelId, Rect>(panel.Id, updatedLogicalBounds),
                new KeyValuePair<PanelId, Rect>(PanelId.DefaultValue, newPanelLogicalBounds)
            });

            return res;
        }
    }

    private static bool CanAddColumn(IPanelViewModel panel, IReadOnlyList<IPanelViewModel> panels, int maxColumns)
    {
        var currentColumns = 0;

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < panels.Count; i++)
        {
            var other = panels[i];

            // NOTE(erri120): +1 column if another panel (self included) has the same Y position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.LogicalBounds.Y.IsCloseTo(panel.LogicalBounds.Y))
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
            if (!other.LogicalBounds.Left.IsCloseTo(panel.LogicalBounds.Right) &&
                !other.LogicalBounds.Right.IsCloseTo(panel.LogicalBounds.Left)) continue;

            // 2) check if the panel is in the current row
            if (other.LogicalBounds.Bottom >= panel.LogicalBounds.Y ||
                other.LogicalBounds.Top <= panel.LogicalBounds.Y)
                currentColumns++;
        }

        return currentColumns < maxColumns;
    }

    private static bool CanAddRow(IPanelViewModel panel, IReadOnlyList<IPanelViewModel> panels, int maxRows)
    {
        var currentRows = 0;

        // ReSharper disable once ForCanBeConvertedToForeach
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (var i = 0; i < panels.Count; i++)
        {
            var other = panels[i];

            // NOTE(erri120): +1 column if another panel (self included) has the same X position.
            // Since self is included, the number of columns is guaranteed to be at least 1.
            if (other.LogicalBounds.X.IsCloseTo(panel.LogicalBounds.X))
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
            if (!other.LogicalBounds.Top.IsCloseTo(panel.LogicalBounds.Bottom) &&
                !other.LogicalBounds.Bottom.IsCloseTo(panel.LogicalBounds.Top)) continue;

            // 2) check if the panel is in the current column
            if (other.LogicalBounds.Right >= panel.LogicalBounds.X ||
                other.LogicalBounds.Left <= panel.LogicalBounds.X)
                currentRows++;
        }

        return currentRows < maxRows;
    }

    internal static IReadOnlyDictionary<PanelId, Rect> GetStateWithoutPanel(
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
            };
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
            if (rect.Left >= currentRect.Left && rect.Right <= currentRect.Right)
            {
                if (rect.Top.IsCloseTo(currentRect.Bottom) || rect.Bottom.IsCloseTo(currentRect.Top))
                {
                    sameColumn[sameColumnCount++] = id;
                }
            }

            // same row
            // | a | b |  | b | a |  | a | b |
            // | x | x |  | x | x |  | a | c |
            if (rect.Top >= currentRect.Top && rect.Bottom <= currentRect.Bottom)
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

    internal static IReadOnlyList<ResizerInfo> GetResizers(ImmutableDictionary<PanelId, Rect> currentState)
    {
        // TODO: make this less messy

        var res = new List<ResizerInfo>(capacity: currentState.Count);
        var tmp = new List<ResizerInfo>(capacity: currentState.Count);

        foreach (var current in currentState)
        {
            var currentRect = current.Value;

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
                        Add(current, other, isHorizontal: true);
                    }
                }

                // same row
                // | a | b |  | b | a |  | a | b |
                // | x | x |  | x | x |  | a | c |
                if (rect.Top >= currentRect.Top && rect.Bottom <= currentRect.Bottom)
                {
                    if (rect.Left.IsCloseTo(currentRect.Right) || rect.Right.IsCloseTo(currentRect.Left))
                    {
                        Add(current, other, isHorizontal: false);
                    }
                }
            }

            foreach (var info in tmp)
            {
                var pos = info.LogicalPosition;
                if (res.Any(x => x.LogicalPosition == pos)) continue;

                var acc = new List<PanelId>();
                acc.AddRange(info.ConnectedPanels);

                foreach (var other in tmp)
                {
                    if (other.IsHorizontal != info.IsHorizontal) continue;

                    var otherPos = other.LogicalPosition;
                    if (pos == otherPos) continue;
                    if (!pos.X.IsCloseTo(otherPos.X) && !pos.Y.IsCloseTo(otherPos.Y)) continue;

                    acc.AddRange(other.ConnectedPanels.Where(id => !acc.Contains(id)));
                }

                res.Add(info with { ConnectedPanels = acc.ToArray() });
            }

            tmp.Clear();
        }

        return res;

        void Add(KeyValuePair<PanelId, Rect> current, KeyValuePair<PanelId, Rect> other, bool isHorizontal)
        {
            var (currentId, currentRect) = current;
            var (otherId, rect) = other;

            var pos = MathUtils.GetMidPoint(currentRect, rect, isHorizontal);
            tmp.Add(new ResizerInfo(IsHorizontal: isHorizontal, pos, new[] { currentId, otherId }));
        }
    }

    internal record struct ResizerInfo(bool IsHorizontal, Point LogicalPosition, PanelId[] ConnectedPanels);
}
