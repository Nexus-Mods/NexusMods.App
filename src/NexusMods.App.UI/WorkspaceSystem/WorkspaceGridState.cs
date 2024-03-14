using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly partial struct WorkspaceGridState :
    IImmutableSet<PanelGridState>,
    IReadOnlyList<PanelGridState>
{
    internal const int MaxColumns = 8;
    internal const int MaxRows = 8;

    public readonly ImmutableSortedSet<PanelGridState> Inner;
    public readonly bool IsHorizontal;

    public WorkspaceGridState(ImmutableSortedSet<PanelGridState> inner, bool isHorizontal)
    {
        Inner = inner.WithComparer(PanelGridStateComparer.Instance);
        IsHorizontal = isHorizontal;
    }

    public static WorkspaceGridState From(IEnumerable<IPanelViewModel> panels, bool isHorizontal)
    {
        return new WorkspaceGridState(
            inner: panels.Select(panel => new PanelGridState(panel.Id, panel.LogicalBounds)).ToImmutableSortedSet(PanelGridStateComparer.Instance),
            isHorizontal
        );
    }

    public static WorkspaceGridState From(IEnumerable<PanelGridState> panels, bool isHorizontal)
    {
        return new WorkspaceGridState(
            inner: panels.ToImmutableSortedSet(PanelGridStateComparer.Instance),
            isHorizontal
        );
    }

    public static WorkspaceGridState From(bool isHorizontal, params PanelGridState[] panels) => From(panels, isHorizontal);

    public static WorkspaceGridState From(IReadOnlyDictionary<PanelId, Rect> panels, bool isHorizontal)
    {
        return new WorkspaceGridState(
            inner: panels.Select(kv => new PanelGridState(kv.Key, kv.Value)).ToImmutableSortedSet(PanelGridStateComparer.Instance),
            isHorizontal
        );
    }

    public static WorkspaceGridState Empty(bool isHorizontal) => new(ImmutableSortedSet<PanelGridState>.Empty, isHorizontal);

    private WorkspaceGridState WithInner(ImmutableSortedSet<PanelGridState> inner)
    {
        return new WorkspaceGridState(inner, IsHorizontal);
    }

    [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
    public PanelGridState this[PanelId id]
    {
        get
        {
            foreach (var panel in Inner)
            {
                if (panel.Id == id) return panel;
            }

            throw new KeyNotFoundException();
        }
    }

    [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
    public bool TryGetValue(PanelId id, out PanelGridState panel)
    {
        foreach (var item in Inner)
        {
            if (item.Id != id) continue;
            panel = item;
            return true;
        }

        panel = default(PanelGridState);
        return false;
    }

    public WorkspaceGridState UnionById(PanelGridState[] other) => UnionById(other.AsSpan());

    public WorkspaceGridState UnionById(ReadOnlySpan<PanelGridState> other)
    {
        var builder = Inner.ToBuilder();
        foreach (var panelToAdd in other)
        {
            if (TryGetValue(panelToAdd.Id, out var existingPanel))
            {
                builder.Remove(existingPanel);
            }

            builder.Add(panelToAdd);
        }

        return WithInner(builder.ToImmutable());
    }

    public AdjacentPanelEnumerator EnumerateAdjacentPanels(PanelGridState anchor, bool includeAnchor) => new(this, anchor, includeAnchor);

    public (int columnCount, int maxRowCount) CountColumns()
    {
        var columnCount = 0;
        var maxRowCount = 0;

        Span<ColumnInfo> seenColumns = stackalloc ColumnInfo[MaxColumns];
        using var enumerator = new ColumnEnumerator(this, seenColumns);

        Span<PanelGridState> rowBuffer = stackalloc PanelGridState[MaxRows];
        while (enumerator.MoveNext(rowBuffer))
        {
            var current = enumerator.Current;
            if (current.Info.IsInfinity()) continue;

            columnCount += 1;
            maxRowCount = Math.Max(maxRowCount, enumerator.Current.Rows.Length);
        }

        return (columnCount, maxRowCount);
    }

    public (int rowCount, int maxColumnCount) CountRows()
    {
        var rowCount = 0;
        var maxColumnCount = 0;

        Span<RowInfo> seenRows = stackalloc RowInfo[MaxRows];
        using var enumerator = new RowEnumerator(this, seenRows);

        Span<PanelGridState> columnBuffer = stackalloc PanelGridState[MaxColumns];
        while (enumerator.MoveNext(columnBuffer))
        {
            var current = enumerator.Current;
            if (current.Info.IsInfinity()) continue;

            rowCount += 1;
            maxColumnCount = Math.Max(maxColumnCount, enumerator.Current.Columns.Length);
        }

        return (rowCount, maxColumnCount);
    }

    [Conditional("DEBUG")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void DebugPrint()
    {
        foreach (var panel in Inner)
        {
            Console.WriteLine(panel.ToString());
        }

        Console.WriteLine();
    }

    #region Interface Implementations

    public ImmutableSortedSet<PanelGridState>.Enumerator GetEnumerator() => Inner.GetEnumerator();
    IEnumerator<PanelGridState> IEnumerable<PanelGridState>.GetEnumerator() => Inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();

    public int Count => Inner.Count;
    public bool Contains(PanelGridState value) => Inner.Contains(value);

    public bool IsProperSubsetOf(IEnumerable<PanelGridState> other) => Inner.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<PanelGridState> other) => Inner.IsProperSubsetOf(other);
    public bool IsSubsetOf(IEnumerable<PanelGridState> other) => Inner.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<PanelGridState> other) => Inner.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<PanelGridState> other) => Inner.Overlaps(other);
    public bool SetEquals(IEnumerable<PanelGridState> other) => Inner.SetEquals(other);

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.Add(PanelGridState value) => Inner.Add(value);
    public WorkspaceGridState Add(PanelGridState value) => WithInner(Inner.Add(value));

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.Clear() => Inner.Clear();
    public WorkspaceGridState Clear() => WithInner(Inner.Clear());

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.Except(IEnumerable<PanelGridState> other) => Inner.Except(other);
    public WorkspaceGridState Except(IEnumerable<PanelGridState> other) => WithInner(Inner.Except(other));

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.Intersect(IEnumerable<PanelGridState> other) => Inner.Intersect(other);
    public WorkspaceGridState Intersect(IEnumerable<PanelGridState> other) => WithInner(Inner.Intersect(other));

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.Remove(PanelGridState value) => Inner.Remove(value);
    public WorkspaceGridState Remove(PanelGridState value) => WithInner(Inner.Remove(value));

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.SymmetricExcept(IEnumerable<PanelGridState> other) => Inner.SymmetricExcept(other);
    public WorkspaceGridState SymmetricExcept(IEnumerable<PanelGridState> other) => WithInner(Inner.SymmetricExcept(other));

    bool IImmutableSet<PanelGridState>.TryGetValue(PanelGridState equalValue, out PanelGridState actualValue) => Inner.TryGetValue(equalValue, out actualValue);

    IImmutableSet<PanelGridState> IImmutableSet<PanelGridState>.Union(IEnumerable<PanelGridState> other) => Inner.Union(other);
    public WorkspaceGridState Union(IEnumerable<PanelGridState> other) => WithInner(Inner.Union(other));

    public int IndexOf(PanelGridState item) => Inner.IndexOf(item);

    public PanelGridState this[int index] => Inner[index];

    #endregion
}
