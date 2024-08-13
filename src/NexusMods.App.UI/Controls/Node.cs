using Avalonia.Controls.Models.TreeDataGrid;
using JetBrains.Annotations;
using ObservableCollections;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls;

/// <summary>
/// Base class for models displayed in a <see cref="Avalonia.Controls.TreeDataGrid"/>.
/// </summary>
[PublicAPI]
public class Node<TNode> : IDisposable
    where TNode : Node<TNode>
{
    [Reactive] public bool IsExpanded { get; [UsedImplicitly] set; }

    public ObservableList<TNode> Children { get; private set; } = [];
    public bool HasChildren => Children.Count > 0;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private bool _isDisposed;
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        Children = null!;
        _isDisposed = true;
    }

    public static HierarchicalExpanderColumn<TNode> CreateExpanderColumn(IColumn<TNode> innerColumn)
    {
        return new HierarchicalExpanderColumn<TNode>(
            inner: innerColumn,
            childSelector: static model => model.Children,
            hasChildrenSelector: static model => model.HasChildren,
            isExpandedSelector: static model => model.IsExpanded
        )
        {
            Tag = "expander",
        };
    }
}
