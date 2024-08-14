using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using JetBrains.Annotations;
using R3;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Controls;

public class Node : ReactiveObject, IDisposable
{
    private readonly Subject<bool> _activation = new();
    protected Observable<bool> Activation => _activation;

    public void Activate() => _activation.OnNext(true);
    public void Deactivate() => _activation.OnNext(false);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private bool _isDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _activation.Dispose();
        }

        _isDisposed = true;
    }
}

/// <summary>
/// Base class for models displayed in a <see cref="Avalonia.Controls.TreeDataGrid"/>.
/// </summary>
[PublicAPI]
public class Node<TNode> : Node
    where TNode : Node<TNode>
{
    [Reactive] public bool IsExpanded { get; [UsedImplicitly] set; }

    public ObservableCollection<TNode> Children { get; private set; } = [];
    public bool HasChildren => Children.Count > 0;

    protected static IDisposable WhenNodeActivated(TNode node, Action<TNode, CompositeDisposable> block)
    {
        var d = Disposable.CreateBuilder();

        var serialDisposable = new SerialDisposable();
        serialDisposable.AddTo(ref d);

        node.Activation.DistinctUntilChanged().Subscribe((node, serialDisposable, block), static (isActivated, state) =>
        {
            var (node, serialDisposable, block) = state;

            serialDisposable.Disposable = null;
            if (isActivated)
            {
                var compositeDisposable = new CompositeDisposable();
                serialDisposable.Disposable = compositeDisposable;

                block(node, compositeDisposable);
            }
        }).AddTo(ref d);

        return d.Build();
    }

    private bool _isDisposed;
    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            Children = null!;

            _isDisposed = true;
        }

        base.Dispose(disposing);
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
