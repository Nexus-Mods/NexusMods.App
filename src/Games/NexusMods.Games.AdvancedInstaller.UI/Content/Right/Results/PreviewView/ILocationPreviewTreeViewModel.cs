using ITreeEntryViewModel =
    NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry.ITreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public interface ILocationPreviewTreeViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
