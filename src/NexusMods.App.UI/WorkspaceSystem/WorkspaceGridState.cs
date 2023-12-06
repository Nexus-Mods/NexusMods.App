using System.Collections;
using System.Collections.Immutable;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly struct WorkspaceGridState :
    IImmutableSet<PanelGridState>,
    ISet<PanelGridState>,
    IReadOnlyList<PanelGridState>,
    IList<PanelGridState>
{
    public readonly ImmutableSortedSet<PanelGridState> Inner;
    public readonly bool IsHorizontal;

    public WorkspaceGridState(ImmutableSortedSet<PanelGridState> inner, bool isHorizontal)
    {
        Inner = inner.WithComparer(PanelGridStateComparer.Instance);
        IsHorizontal = isHorizontal;
    }

    public static WorkspaceGridState From(IEnumerable<KeyValuePair<PanelId, Rect>> values, bool isHorizontal)
    {
        return new WorkspaceGridState(
            inner: values.Select(kv => new PanelGridState(kv.Key, kv.Value)).ToImmutableSortedSet(PanelGridStateComparer.Instance),
            isHorizontal
        );
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

    public static WorkspaceGridState Empty(bool isHorizontal) => new(ImmutableSortedSet<PanelGridState>.Empty, isHorizontal);

    private WorkspaceGridState WithInner(ImmutableSortedSet<PanelGridState> inner)
    {
        return new WorkspaceGridState(inner, IsHorizontal);
    }

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

    public bool TryGetValue(PanelId id, out PanelGridState panel)
    {
        foreach (var item in Inner)
        {
            if (item.Id != id) continue;
            panel = item;
            return true;
        }

        panel = default;
        return false;
    }

    public WorkspaceGridState UnionById(PanelGridState[] other)
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

    #region Interface Implementations

    public ImmutableSortedSet<PanelGridState>.Enumerator GetEnumerator() => Inner.GetEnumerator();
    IEnumerator<PanelGridState> IEnumerable<PanelGridState>.GetEnumerator() => Inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();

    bool ICollection<PanelGridState>.Remove(PanelGridState item) => throw new NotSupportedException();
    public int Count => Inner.Count;
    public bool IsReadOnly => true;
    public bool Contains(PanelGridState value) => Inner.Contains(value);

    void ISet<PanelGridState>.ExceptWith(IEnumerable<PanelGridState> other) => throw new NotSupportedException();
    void ISet<PanelGridState>.IntersectWith(IEnumerable<PanelGridState> other) => throw new NotSupportedException();
    public bool IsProperSubsetOf(IEnumerable<PanelGridState> other) => Inner.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<PanelGridState> other) => Inner.IsProperSubsetOf(other);
    public bool IsSubsetOf(IEnumerable<PanelGridState> other) => Inner.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<PanelGridState> other) => Inner.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<PanelGridState> other) => Inner.Overlaps(other);
    public bool SetEquals(IEnumerable<PanelGridState> other) => Inner.SetEquals(other);
    void ISet<PanelGridState>.SymmetricExceptWith(IEnumerable<PanelGridState> other) => throw new NotSupportedException();
    void ISet<PanelGridState>.UnionWith(IEnumerable<PanelGridState> other) => throw new NotSupportedException();
    bool ISet<PanelGridState>.Add(PanelGridState item) => throw new NotSupportedException();
    void ICollection<PanelGridState>.Clear() => throw new NotSupportedException();
    void ICollection<PanelGridState>.CopyTo(PanelGridState[] array, int arrayIndex) => ((ICollection<PanelGridState>)Inner).CopyTo(array, arrayIndex);
    void ICollection<PanelGridState>.Add(PanelGridState item) => throw new NotSupportedException();

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

    void IList<PanelGridState>.Insert(int index, PanelGridState item) => throw new NotSupportedException();

    void IList<PanelGridState>.RemoveAt(int index) => throw new NotSupportedException();

    public PanelGridState this[int index]
    {
        get => Inner[index];
        set => throw new NotSupportedException();
    }

    #endregion
}
