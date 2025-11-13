
using NexusMods.Sdk.Games;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SelectableTreeEntryDesignViewModel() : SelectableTreeEntryViewModel(
    new GamePath(LocationId.Game, ""),
    SelectableDirectoryNodeStatus.Regular);
