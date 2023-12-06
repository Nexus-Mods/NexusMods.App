using System.Collections;
using System.Collections.Immutable;
using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public readonly struct WorkspaceGridState : IImmutableDictionary<PanelId, Rect>
{
    public ImmutableDictionary<PanelId, Rect> Inner { get; }
    public bool IsHorizontal { get; }

    public WorkspaceGridState(ImmutableDictionary<PanelId, Rect> inner, bool isHorizontal)
    {
        Inner = inner;
        IsHorizontal = isHorizontal;
    }

    public static WorkspaceGridState From(IEnumerable<IPanelViewModel> enumerable, bool isHorizontal)
    {
        return new WorkspaceGridState(enumerable.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds), isHorizontal);
    }

    public static readonly WorkspaceGridState Empty = new(ImmutableDictionary<PanelId, Rect>.Empty, isHorizontal: true);
    public static WorkspaceGridState Single(PanelId key) => Empty.Add(key, MathUtils.One);

    private WorkspaceGridState WithInner(ImmutableDictionary<PanelId, Rect> inner)
    {
        return new WorkspaceGridState(inner, IsHorizontal);
    }

    #region IImmutableDictionary implementations

    public ImmutableDictionary<PanelId, Rect>.Enumerator GetEnumerator() => Inner.GetEnumerator();
    IEnumerator<KeyValuePair<PanelId, Rect>> IEnumerable<KeyValuePair<PanelId, Rect>>.GetEnumerator() => Inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Inner.GetEnumerator();

    public int Count => Inner.Count;
    public bool ContainsKey(PanelId key) => Inner.ContainsKey(key);
    public bool TryGetValue(PanelId key, out Rect value) => Inner.TryGetValue(key, out value);
    public Rect this[PanelId key] => Inner[key];
    public IEnumerable<PanelId> Keys => Inner.Keys;
    public IEnumerable<Rect> Values => Inner.Values;

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.Add(PanelId key, Rect value) => Inner.Add(key, value);
    public WorkspaceGridState Add(PanelId key, Rect value) => WithInner(Inner.Add(key, value));

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.AddRange(IEnumerable<KeyValuePair<PanelId, Rect>> pairs) => Inner.AddRange(pairs);
    public WorkspaceGridState AddRange(IEnumerable<KeyValuePair<PanelId, Rect>> pairs) => WithInner(Inner.AddRange(pairs));

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.Clear() => Inner.Clear();
    public WorkspaceGridState Clear() => WithInner(Inner.Clear());

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.Remove(PanelId key) => Inner.Remove(key);
    public WorkspaceGridState Remove(PanelId key) => WithInner(Inner.Remove(key));

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.RemoveRange(IEnumerable<PanelId> keys) => Inner.RemoveRange(keys);
    public WorkspaceGridState RemoveRange(IEnumerable<PanelId> keys) => WithInner(Inner.RemoveRange(keys));

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.SetItem(PanelId key, Rect value) => Inner.SetItem(key, value);
    public WorkspaceGridState SetItem(PanelId key, Rect value) => WithInner(Inner.SetItem(key, value));

    IImmutableDictionary<PanelId, Rect> IImmutableDictionary<PanelId, Rect>.SetItems(IEnumerable<KeyValuePair<PanelId, Rect>> items) => Inner.SetItems(items);
    public WorkspaceGridState SetItems(IEnumerable<KeyValuePair<PanelId, Rect>> items) => WithInner(Inner.SetItems(items));

    public bool TryGetKey(PanelId equalKey, out PanelId actualKey) => Inner.TryGetKey(equalKey, out actualKey);
    public bool Contains(KeyValuePair<PanelId, Rect> pair) => Inner.Contains(pair);

    #endregion
}
