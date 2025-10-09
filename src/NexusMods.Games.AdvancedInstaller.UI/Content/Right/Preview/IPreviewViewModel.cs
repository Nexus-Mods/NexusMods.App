using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Abstractions.GameLocators;
using NexusMods.UI.Sdk;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;

/// <summary>
/// Shows a preview of the file structure the installed mod files will be placed in after installation.
/// </summary>
public interface IPreviewViewModel : IViewModelInterface
{
    /// <summary>
    /// A flat collection of all the preview tree entries.
    /// DynamicData TransformToTree is used to generate a bound observable tree structure.
    /// This should be used to add/remove entries from the preview tree.
    /// </summary>
    public SourceCache<IPreviewTreeEntryViewModel, GamePath> TreeEntriesCache { get; }

    /// <summary>
    /// Observable collection of the root nodes of the preview tree.
    /// </summary>
    public ReadOnlyObservableCollection<PreviewTreeNode> TreeRoots { get; }

    /// <summary>
    /// The source data used for the TreeDataGrid.
    /// </summary>
    public HierarchicalTreeDataGridSource<PreviewTreeNode> Tree { get; }
}
