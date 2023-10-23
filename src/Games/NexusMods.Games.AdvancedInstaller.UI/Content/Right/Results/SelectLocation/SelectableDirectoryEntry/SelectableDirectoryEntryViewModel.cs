using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

internal class SelectableDirectoryEntryViewModel : AViewModel<ISelectableDirectoryEntryViewModel>,
    ISelectableDirectoryEntryViewModel
{
    public OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode> Node { get; }

    public SelectableDirectoryEntryViewModel(ITreeEntryViewModel node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode>.FromT0(node);
    }

    public SelectableDirectoryEntryViewModel(ISelectableDirectoryNode node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode>.FromT1(node);
    }

    public SelectableDirectoryEntryViewModel(IPreviewEntryNode node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode>.FromT2(node);
    }
}
