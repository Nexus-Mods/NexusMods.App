using NexusMods.Abstractions.Installers.DTO;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class SelectableTreeEntryDesignViewModel() : SelectableTreeEntryViewModel(
    new GamePath(LocationId.Game, ""),
    SelectableDirectoryNodeStatus.Regular);
