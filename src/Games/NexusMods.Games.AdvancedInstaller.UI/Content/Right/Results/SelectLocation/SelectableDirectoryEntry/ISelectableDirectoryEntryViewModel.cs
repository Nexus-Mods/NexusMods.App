using OneOf;
using ITreeEntryViewModel = NexusMods.Games.AdvancedInstaller.UI.Content.Left.ITreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

public interface ISelectableDirectoryEntryViewModel : IViewModel
{
    public OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, PreviewView.PreviewEntry.ITreeEntryViewModel> Node
    {
        get;
    }
}
