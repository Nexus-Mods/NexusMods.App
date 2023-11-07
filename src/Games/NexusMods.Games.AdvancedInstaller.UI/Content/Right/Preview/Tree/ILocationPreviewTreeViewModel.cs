namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

public interface ILocationPreviewTreeViewModel : IViewModelInterface
{
    public IPreviewTreeEntryViewModel Root { get; }

    public HierarchicalTreeDataGridSource<IPreviewTreeEntryViewModel> Tree { get; }
}
