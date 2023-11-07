using System.Diagnostics.CodeAnalysis;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class SelectableTreeEntryStandardDirectoryDesignViewModel : SelectableTreeEntryViewModel
{
    public SelectableTreeEntryStandardDirectoryDesignViewModel() : base(new DummyCoordinator())
    {
        Path = new GamePath(LocationId.Game, "Cool Folder Name");
        Status = SelectableDirectoryNodeStatus.Regular;
    }
}
