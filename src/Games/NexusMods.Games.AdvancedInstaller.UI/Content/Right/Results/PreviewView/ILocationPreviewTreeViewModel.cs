using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public interface ILocationPreviewTreeViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<IPreviewTreeEntryViewModel> Tree { get; }
}
