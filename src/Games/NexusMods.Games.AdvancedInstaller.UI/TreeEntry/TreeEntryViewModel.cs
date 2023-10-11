using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using OneOf;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class TreeEntryViewModel : AViewModel<ITreeEntryViewModel>,
    ITreeEntryViewModel
{

    public OneOf<IModContentNode, ISelectableDirectoryNode, IPreviewEntryNode> Node { get; }
    public TreeEntryViewModel(OneOf<IModContentNode, ISelectableDirectoryNode, IPreviewEntryNode> node)
    {
        Node = node;
    }

}

