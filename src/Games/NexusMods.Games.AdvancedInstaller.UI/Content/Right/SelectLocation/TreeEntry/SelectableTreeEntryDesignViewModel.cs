using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SelectableTreeEntryDesignViewModel : SelectableTreeEntryViewModel
{
    public SelectableTreeEntryDesignViewModel() : base(
        new GamePath(LocationId.Game, ""),
        SelectableDirectoryNodeStatus.Regular) { }
}
