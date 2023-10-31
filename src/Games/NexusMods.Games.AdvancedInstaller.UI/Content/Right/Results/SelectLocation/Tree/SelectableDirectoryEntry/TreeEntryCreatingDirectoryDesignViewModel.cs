using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation.SelectableDirectoryEntry;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class TreeEntryCreatingDirectoryDesignViewModel : TreeEntryViewModel
{
    public TreeEntryCreatingDirectoryDesignViewModel() : base(new DummyCoordinator())
    {
        Status = SelectableDirectoryNodeStatus.Editing;
    }
}
