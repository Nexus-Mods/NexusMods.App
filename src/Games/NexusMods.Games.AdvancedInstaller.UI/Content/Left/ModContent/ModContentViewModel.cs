using System.Collections.ObjectModel;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;
using DynamicData;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
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

    public ModContentViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles)
    {
        // Crete mod content tree entries
        PopulateModContentEntriesCache(ModContentEntriesCache, archiveFiles);

        // Generate the mod content tree structure
        ModContentEntriesCache.Connect()
            .TransformToTree(item => item.Parent)
            .Transform(node => new ModContentNode(node))
            .Bind(out _modContentTreeRoots)
            .DisposeMany()
            .Subscribe();

        // Populate the TreeDataGridSource
        Tree = CreateTreeDataGridSource(Root);
    }


    public void SelectChildrenRecursive(ModContentNode node)
    {
        foreach (var child in node.Children)
        {
            if (node.Item.Status != ModContentTreeEntryStatus.Default) continue;

            child.Item.Status = ModContentTreeEntryStatus.SelectingViaParent;
            SelectChildrenRecursive(child);
        }
    }

    #region private

    private readonly ReadOnlyObservableCollection<ModContentNode> _modContentTreeRoots;

    private void PopulateModContentEntriesCache(
        ISourceCache<IModContentTreeEntryViewModel, RelativePath> cache,
        FileTreeNode<RelativePath, ModSourceFileEntry> node)
    {
        var allNodes = node.GetAllNodes();

        // Populate the cache
        cache.Edit(updater =>
        {
            foreach (var (relativePath, fileTreeNode) in allNodes)
            {
                // All the tree leaf nodes are files, so it's a directory if it has children
                var entry = new ModContentTreeEntryViewModel(relativePath, fileTreeNode.Children.Count > 0);
                updater.AddOrUpdate(entry);
            }
        });
    }

    private static TreeDataGridSource CreateTreeDataGridSource(ModContentNode root)
    {
        return new(root)
        {
            Columns =
            {
                new HierarchicalExpanderColumn<ModContentNode>(
                    new TemplateColumn<ModContentNode>(null,
                        new FuncDataTemplate<ModContentNode>((node, _) =>
                            new ModContentTreeEntryView
                            {
                                DataContext = node?.Item,
                            }),
                        width: new GridLength(1, GridUnitType.Star)
                    ),
                    x => x.Children)
            }
        };
    }


    #endregion private
}
