using System.Diagnostics.CodeAnalysis;
using NexusMods.Games.AdvancedInstaller.UI.Content;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class SelectableTreeEntryCreatingDirectoryDesignViewModel : SelectableTreeEntryViewModel
{
    public SelectableTreeEntryCreatingDirectoryDesignViewModel() : base(new DummyCoordinator())
    {
        Status = SelectableDirectoryNodeStatus.Editing;
    }
}
