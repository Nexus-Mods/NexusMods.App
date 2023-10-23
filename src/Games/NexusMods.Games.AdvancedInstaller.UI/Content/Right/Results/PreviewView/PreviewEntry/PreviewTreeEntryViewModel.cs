using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;

internal class PreviewTreeEntryViewModel : AViewModel<IPreviewTreeEntryViewModel>,
    IPreviewTreeEntryViewModel
{
    public OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode> Node { get; }

    public PreviewTreeEntryViewModel(ITreeEntryViewModel node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode>.FromT0(node);
    }

    public PreviewTreeEntryViewModel(ISelectableDirectoryNode node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode>.FromT1(node);
    }

    public PreviewTreeEntryViewModel(IPreviewEntryNode node)
    {
        Node = OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode>.FromT2(node);
    }
}
