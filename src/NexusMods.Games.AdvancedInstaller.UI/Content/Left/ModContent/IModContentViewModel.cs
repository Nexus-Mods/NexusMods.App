using DynamicData;
using NexusMods.Paths;
using NexusMods.UI.Sdk;

namespace NexusMods.Games.AdvancedInstaller.UI.ModContent;

public interface IModContentViewModel : IViewModelInterface
{
    /// <summary>
    /// Used to disable the UI when the user is creating a folder.
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// The root node of the visual tree.
    /// </summary>
    public TreeNodeVM<IModContentTreeEntryViewModel, RelativePath> Root { get; }

    /// <summary>
    /// Flat collection of <see cref="ModContentTreeEntryViewModel"/>s.
    /// Used to subscribe to commands on the entire tree structure.
    /// </summary>
    public SourceCache<IModContentTreeEntryViewModel, RelativePath> ModContentEntriesCache { get; }

    /// <summary>
    /// TreeDataGridSource for the visual tree.
    /// </summary>
    public HierarchicalTreeDataGridSource<TreeNodeVM<IModContentTreeEntryViewModel, RelativePath>> Tree { get; }

    /// <summary>
    /// Editable flat collection of tree entries that the user has started selecting for mapping.
    /// This only contains the explicitly selected entries, not SelectingViaParent entries.
    /// </summary>
    public SourceCache<IModContentTreeEntryViewModel, RelativePath> SelectedEntriesCache { get; }


    /// <summary>
    /// Recursively sets the status of all the children to SelectingViaParent.
    /// Skips nodes and subtree that aren't in the default state.
    /// </summary>
    /// <param name="node">Parent node, not included in operation</param>
    public void SelectChildrenRecursive(TreeNodeVM<IModContentTreeEntryViewModel, RelativePath> node);

    /// <summary>
    /// Recursively resets the status of all the SelectingViaParent children to Default.
    /// If a child folder isn't in the SelectingViaParent state, it's subtree is skipped.
    /// </summary>
    /// <param name="node">Parent node, not included in operation</param>
    public void DeselectChildrenRecursive(TreeNodeVM<IModContentTreeEntryViewModel, RelativePath> node);
}
