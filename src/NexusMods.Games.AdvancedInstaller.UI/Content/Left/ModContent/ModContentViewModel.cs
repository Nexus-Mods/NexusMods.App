using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.App.UI.Helpers.TreeDataGrid;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using NexusMods.Paths.Trees.Traits;
using NexusMods.Sdk.Library;
using NexusMods.UI.Sdk;
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

    public ModContentViewModel(KeyedBox<RelativePath, LibraryArchiveTree> archiveFiles)
    {
        PopulateModContentEntriesCache(ModContentEntriesCache, archiveFiles);

        // Generate the mod content tree structure
        ModContentEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new ModContentNode(node))
            .Bind(out _modContentTreeRoots)
            .Subscribe();

        Tree = TreeDataGridHelpers.CreateTreeSourceWithSingleCustomColumn<ModContentNode, IModContentTreeEntryViewModel, RelativePath>(Root);
        Root.Item.IsExpanded = true;
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
        KeyedBox<RelativePath, LibraryArchiveTree> node)
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
                var entry = new ModContentTreeEntryViewModel(curNode.Item.Path, curNode.IsDirectory());
                updater.AddOrUpdate(entry);
            }
        });
    }
#endregion private
}
