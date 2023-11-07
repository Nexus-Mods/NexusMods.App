namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public interface ISelectLocationTreeViewModel : IViewModelInterface
{
    public ISelectableTreeEntryViewModel Root { get; }

    public HierarchicalTreeDataGridSource<ISelectableTreeEntryViewModel> Tree { get; }

    public IAdvancedInstallerCoordinator Coordinator { get; }
}
