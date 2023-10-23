using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView.PreviewEntry;
using OneOf;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

public interface ISelectableDirectoryEntryViewModel : IViewModel
{
    public OneOf<ITreeEntryViewModel, ISelectableDirectoryNode, IPreviewEntryNode> Node { get; }
}
