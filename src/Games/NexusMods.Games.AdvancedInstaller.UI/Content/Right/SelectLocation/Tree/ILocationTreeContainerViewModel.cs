using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

using SelectableTreeNode = TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>;

public interface ILocationTreeContainerViewModel : IViewModelInterface
{
    public SelectableTreeNode Root { get; }

    public HierarchicalTreeDataGridSource<SelectableTreeNode> Tree { get; }
}
