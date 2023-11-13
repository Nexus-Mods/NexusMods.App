using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

using PreviewTreeNode = TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>;

public interface ILocationPreviewTreeViewModel : IViewModelInterface
{
    public PreviewTreeNode Root { get; }

    public HierarchicalTreeDataGridSource<PreviewTreeNode> Tree { get; }
}
