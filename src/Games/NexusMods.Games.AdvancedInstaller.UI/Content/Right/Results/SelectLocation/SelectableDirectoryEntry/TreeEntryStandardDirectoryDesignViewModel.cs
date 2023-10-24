using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class TreeEntryStandardDirectoryDesignViewModel : TreeEntryViewModel
{
    public TreeEntryStandardDirectoryDesignViewModel()
    {
        Path = new GamePath(LocationId.Game, "Cool Folder Name");
        Status = SelectableDirectoryNodeStatus.Regular;
    }
}
