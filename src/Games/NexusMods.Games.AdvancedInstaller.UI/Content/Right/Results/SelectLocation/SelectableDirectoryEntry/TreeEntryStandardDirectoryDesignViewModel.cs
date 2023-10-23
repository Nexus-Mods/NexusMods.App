using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

public class TreeEntryStandardDirectoryDesignViewModel : TreeEntryViewModel
{
    public TreeEntryStandardDirectoryDesignViewModel()
    {
        Path = new GamePath(LocationId.Game, "Cool Folder Name");
        Status = SelectableDirectoryNodeStatus.Regular;
    }
}
