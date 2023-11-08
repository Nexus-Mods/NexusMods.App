using System.Collections.ObjectModel;
using DynamicData;
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
    where TItem : class, IViewModelInterface where TKey : notnull
{
    private readonly ReadOnlyObservableCollection<TreeNodeVM<TItem, TKey>> _children;

    /// <summary>
    /// Collection of child nodes.
    /// This is an observable collection so that the UI can be notified of changes to the tree structure.
    /// </summary>
    public ReadOnlyObservableCollection<TreeNodeVM<TItem, TKey>> Children => _children;

    /// <summary>
    /// Reference to the original item of type <see cref="TItem"/> in the flat list.
    /// </summary>
    public TItem Item { get; }

    /// <summary>
    /// The key used in the <see cref="SourceCache{TObject,TKey}"/> for the original item.
    /// This is convenience property to avoid having to access the <see cref="TItem"/> object.
    /// </summary>
    public TKey Key { get; }

    /// <summary>
    /// Creates a new <see cref="TreeNodeVM{TItem,TKey}"/> from a <see cref="Node{TItem,TKey}"/>.
    /// <see cref="Node{TItem,TKey}"/> is the output of DynamicData TransformToTree().
    /// </summary>
    /// <param name="node"></param>
    public TreeNodeVM(Node<TItem, TKey> node)
    {
        Item = node.Item;
        Key = node.Key;

        node.Children
            .Connect()
            .Transform(child => new TreeNodeVM<TItem, TKey>(child))
            .Bind(out _children)
            .DisposeMany()
            .Subscribe();
    }

    public ViewModelActivator Activator { get; } = new();
}
