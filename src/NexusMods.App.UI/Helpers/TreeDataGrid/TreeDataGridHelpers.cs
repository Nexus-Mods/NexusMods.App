using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using NexusMods.UI.Sdk;
using ReactiveUI;
using Splat;

namespace NexusMods.App.UI.Helpers.TreeDataGrid;

/// <summary>
/// Helper functions for constructing <see cref="TreeDataGrid"/>(s)
/// </summary>
public static class TreeDataGridHelpers
{
    /// <summary>
    /// Generates a HierarchicalTreeDataGridSource with a custom column for a given tree structure.
    /// </summary>
    /// <param name="treeRoot">The single root.</param>
    /// <returns>A configured HierarchicalTreeDataGridSource.</returns>
    /// <typeparam name="TNode">Type of node which derives from <see cref="TreeNodeVM{TItem,TKey}"/></typeparam>
    /// <typeparam name="TItem">Type of item which is stored inside <typeparamref name="TNode"/>'s value</typeparam>
    /// <typeparam name="TKey">Type of item which is stored inside <typeparamref name="TNode"/>'s key</typeparam>
    public static HierarchicalTreeDataGridSource<TNode> CreateTreeSourceWithSingleCustomColumn<TNode, TItem, TKey>(
        TNode treeRoot)
        where TNode : TreeNodeVM<TItem, TKey>
        where TItem : class, IViewModelInterface, IExpandableItem
        where TKey : notnull
    {
        return CreateTreeSourceWithSingleCustomColumn<TNode, TItem, TKey>(Enumerable.Repeat(treeRoot, 1));
    }

    /// <summary>
    /// Generates a HierarchicalTreeDataGridSource with a custom column for a given tree structure.
    /// </summary>
    /// <param name="treeRoots">An observable collection of the tree roots.</param>
    /// <returns>A configured HierarchicalTreeDataGridSource.</returns>
    /// <typeparam name="TNode">Type of node which derives from <see cref="TreeNodeVM{TItem,TKey}"/></typeparam>
    /// <typeparam name="TItem">Type of item which is stored inside <typeparamref name="TNode"/>'s value</typeparam>
    /// <typeparam name="TKey">Type of item which is stored inside <typeparamref name="TNode"/>'s key</typeparam>
    public static HierarchicalTreeDataGridSource<TNode> CreateTreeSourceWithSingleCustomColumn<TNode, TItem, TKey>(
        IEnumerable<TNode> treeRoots) 
        where TNode : TreeNodeVM<TItem, TKey>
        where TItem : class, IViewModelInterface, IExpandableItem
        where TKey : notnull
    {
        var locator = Locator.Current.GetService<IViewLocator>();
        return new HierarchicalTreeDataGridSource<TNode>(treeRoots)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<TNode>(
                    new TemplateColumn<TNode>(null,
                        new FuncDataTemplate<TNode>((node, _) =>
                            {
                                // This should never be null but can be during rapid resize, due to
                                // virtualization shenanigans. Think this is a control bug, but well, gotta work with what we have.
                                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                                if (node == null)
                                    return new Control();
                                    
                                // Very sus but it works, t r u s t.
                                var view = locator!.ResolveView(node.Item);
                                var ctrl = view as Control;
                                Debug.Assert(ctrl != null, $"You need to add a view for {typeof(TItem)} into DI.");
                                ctrl!.DataContext = node.Item;
                                return ctrl;
                            }
                        ),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    node => (IEnumerable<TNode>?)node.Children,
                    null,  
                    node => node.IsExpanded),
            }
        };
    }
}

