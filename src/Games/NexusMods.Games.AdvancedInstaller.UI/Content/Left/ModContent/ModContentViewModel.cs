﻿using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using DynamicData;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.Trees;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

using TreeDataGridSource = HierarchicalTreeDataGridSource<TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>>;
using ModContentNode = TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>;

internal class ModContentViewModel : AViewModel<IModContentViewModel>, IModContentViewModel
{
    [Reactive] public bool IsDisabled { get; set; }
    public ModContentNode Root => _modContentTreeRoots.First();

    public TreeDataGridSource Tree { get; }

    public SourceCache<IModContentTreeEntryViewModel, RelativePath> SelectedEntriesCache { get; } =
        new(entry => entry.RelativePath);

    public SourceCache<IModContentTreeEntryViewModel, RelativePath> ModContentEntriesCache { get; } =
        new(x => x.RelativePath);

    public ModContentViewModel(KeyedBox<RelativePath, ModFileTree> archiveFiles)
    {
        PopulateModContentEntriesCache(ModContentEntriesCache, archiveFiles);

        // Generate the mod content tree structure
        ModContentEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new ModContentNode(node))
            .Bind(out _modContentTreeRoots)
            .Subscribe();

        Tree = CreateTreeDataGridSource(Root);

        Root.IsExpanded = true;
    }

    #region utility

    public void SelectChildrenRecursive(ModContentNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.Item.Status != ModContentTreeEntryStatus.Default) continue;

            child.Item.Status = ModContentTreeEntryStatus.SelectingViaParent;
            SelectChildrenRecursive(child);
        }
    }

    public void DeselectChildrenRecursive(ModContentNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.Item.Status != ModContentTreeEntryStatus.SelectingViaParent) continue;

            child.Item.Status = ModContentTreeEntryStatus.Default;
            DeselectChildrenRecursive(child);
        }
    }

    #endregion utility

    #region private

    private readonly ReadOnlyObservableCollection<ModContentNode> _modContentTreeRoots;

    /// <summary>
    /// Populates the tree cache from a FileTreeNode structure representing the archive contents.
    /// </summary>
    /// <param name="cache">The tree entries cache, where new entries will be added.</param>
    /// <param name="node">The root node of the filetree containing the archive contents that need to be added.</param>
    private void PopulateModContentEntriesCache(
        ISourceCache<IModContentTreeEntryViewModel, RelativePath> cache,
        KeyedBox<RelativePath, ModFileTree> node)
    {
        var allNodes = node.GetChildrenRecursive();

        // Populate the cache
        cache.Edit(updater =>
        {
            // Create the root node
            var root = new ModContentTreeEntryViewModel(RelativePath.Empty, true);
            updater.AddOrUpdate(root);

            foreach (var curNode in allNodes)
            {
                // All the tree leaf nodes are files, so it's a directory if it has children
                var entry = new ModContentTreeEntryViewModel(curNode.Path(), curNode.IsDirectory());
                updater.AddOrUpdate(entry);
            }
        });
    }

    /// <summary>
    /// Required to display the tree in the TreeDataGrid.
    /// </summary>
    /// <param name="root">The root node of the view</param>
    /// <returns></returns>
    private static TreeDataGridSource CreateTreeDataGridSource(ModContentNode root)
    {
        return new TreeDataGridSource(root)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ModContentNode>(
                    new TemplateColumn<ModContentNode>(null,
                        new FuncDataTemplate<ModContentNode>((node, _) =>
                            new ModContentTreeEntryView
                            {
                                // node can apparently be null even if it isn't nullable, likely for virtualization
                                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                                DataContext = node?.Item,
                            }),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    node => node.Children,
                    null,
                    node => node.IsExpanded)
            }
        };
    }

    #endregion private
}
