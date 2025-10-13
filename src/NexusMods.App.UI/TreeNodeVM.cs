using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Kernel;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI;

/// <summary>
/// View compatible wrapper for the DynamicData <see cref="Node{TItem,TKey}"/> class.
/// DynamicData TransformToTree() allows us to convert a flat list of items into a tree structure.
/// This class wraps the DynamicData Node class and exposes the tree structure as a collection of <see cref="TreeNodeVM{TItem,TKey}"/> objects.
/// </summary>
/// <typeparam name="TItem">The type of the items in the original flat list</typeparam>
/// <typeparam name="TKey">The type of the Key for the items</typeparam>
public class TreeNodeVM<TItem, TKey> : ReactiveObject, IActivatableViewModel
    where TItem : class, IViewModelInterface, IExpandableItem where TKey : notnull
{
    private readonly Lazy<ReadOnlyObservableCollection<TreeNodeVM<TItem, TKey>>> _children;

    /// <summary>
    /// Collection of child nodes.
    /// This is an observable collection so that the UI can be notified of changes to the tree structure.
    /// </summary>
    public ReadOnlyObservableCollection<TreeNodeVM<TItem, TKey>> Children => _children.Value;

    /// <summary>
    /// Reference to the original item of type <see cref="TItem"/> in the flat list.
    /// </summary>
    public TItem Item { get; }

    /// <summary>
    /// The Id used in the <see cref="SourceCache{TObject,TKey}"/> for the original item.
    /// This is convenience property to avoid having to access the <see cref="TItem"/> object.
    /// </summary>
    public TKey Id { get; }

    /// <summary>
    /// The parent node.
    /// </summary>
    public Optional<TreeNodeVM<TItem, TKey>> Parent { get; }

    /// <summary>
    ///     A wrapper around the inner reactive <see cref="Item"/>'s IsExpanded property.
    /// </summary>
    /// <remarks>
    ///     This exists due to binding shenanigans with TreeDataGrid, it uses the
    ///     Avalonia binding system, and we can't bind to the inner item's IsExpanded property directly.
    /// </remarks>
    public bool IsExpanded
    {
        get => Item.IsExpanded;
        set => Item.IsExpanded = value;
    }

    /// <summary>
    /// Creates a new <see cref="TreeNodeVM{TItem,TKey}"/> from a <see cref="Node{TItem,TKey}"/>.
    /// <see cref="Node{TItem,TKey}"/> is the output of DynamicData TransformToTree().
    /// </summary>
    /// <param name="node"></param>
    /// <param name="parent"></param>
    public TreeNodeVM(Node<TItem, TKey> node, TreeNodeVM<TItem, TKey>? parent = null)
    {
        Item = node.Item;
        Id = node.Key;
        Parent = parent;
        
        this.WhenActivated(d =>
        {
            // re-broadcast 'PropertyChanged' event from the inner item.
            Item.WhenAnyValue(x => x.IsExpanded)
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsExpanded)))
                .DisposeWith(d);
        });

        _children = new Lazy<ReadOnlyObservableCollection<TreeNodeVM<TItem, TKey>>>(() =>
        {
            node.Children
                .Connect()
                .Transform(child => new TreeNodeVM<TItem, TKey>(child, this))
                .Bind(out var children)
                .Subscribe();
            return children;
        });
    }

    /// <summary>
    /// Returns a collection of all the descendent Id of this node (excluding this node).
    /// </summary>
    /// <returns></returns>
    public List<TKey> GetAllDescendentIds()
    {
        var results = new List<TKey>();
        if (Children.Count == 0) return results;

        GetAllDescendentIdsRecursive(this, results);
        return results;
    }


    /// <summary>
    /// Returns a collection of all the descendent nodes of this node (excluding this node).
    /// </summary>
    /// <returns></returns>
    public List<TreeNodeVM<TItem, TKey>> GetAllDescendentNodes()
    {
        var results = new List<TreeNodeVM<TItem, TKey>>();
        if (Children.Count == 0) return results;

        GetAllDescendentNodesRecursive(this, results);
        return results;
    }

    #region private

    private void GetAllDescendentIdsRecursive(TreeNodeVM<TItem, TKey> node, List<TKey> results)
    {
        foreach (var child in node.Children)
        {
            results.Add(child.Id);

            GetAllDescendentIdsRecursive(child, results);
        }
    }

    private void GetAllDescendentNodesRecursive(TreeNodeVM<TItem, TKey> node, List<TreeNodeVM<TItem, TKey>> results)
    {
        foreach (var child in node.Children)
        {
            results.Add(child);

            GetAllDescendentNodesRecursive(child, results);
        }
    }

    #endregion private

    public ViewModelActivator Activator { get; } = new();
}
