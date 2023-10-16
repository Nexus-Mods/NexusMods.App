using Avalonia.Controls;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

public interface ILocationPreviewTreeViewModel : IViewModel
{
    public HierarchicalTreeDataGridSource<ITreeEntryViewModel> Tree { get; }
}
