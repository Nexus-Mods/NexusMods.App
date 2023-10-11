using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI;

internal class TreeEntryDesignViewModel : TreeEntryViewModel
{
    public TreeEntryDesignViewModel(OneOf<IModContentNode, ISelectableDirectoryNode, IPreviewEntryNode> node) : base(node) { }
}
