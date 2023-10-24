using System.Diagnostics.CodeAnalysis;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class TreeEntryCreatingDirectoryDesignViewModel : TreeEntryViewModel
{
    public TreeEntryCreatingDirectoryDesignViewModel()
    {
        Status = SelectableDirectoryNodeStatus.Editing;
    }
}
