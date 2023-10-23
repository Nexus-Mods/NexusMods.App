using OneOf;
using ITreeEntryViewModel = NexusMods.Games.AdvancedInstaller.UI.Content.Left.ITreeEntryViewModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

internal class SelectableDirectoryEntryViewModel : AViewModel<ISelectableDirectoryEntryViewModel>,
    ISelectableDirectoryEntryViewModel
{
    public OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, PreviewView.PreviewEntry.ITreeEntryViewModel> Node
    {
        get;
    }

    public SelectableDirectoryEntryViewModel(ITreeEntryViewModel node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, PreviewView.PreviewEntry.ITreeEntryViewModel>
            .FromT0(node);
    }

    public SelectableDirectoryEntryViewModel(ISelectableDirectoryNode node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, PreviewView.PreviewEntry.ITreeEntryViewModel>
            .FromT1(node);
    }

    public SelectableDirectoryEntryViewModel(PreviewView.PreviewEntry.ITreeEntryViewModel viewModel)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, PreviewView.PreviewEntry.ITreeEntryViewModel>
            .FromT2(viewModel);
    }
}
