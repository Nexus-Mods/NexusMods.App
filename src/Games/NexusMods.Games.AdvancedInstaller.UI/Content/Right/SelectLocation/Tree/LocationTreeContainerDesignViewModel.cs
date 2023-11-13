using System.Diagnostics.CodeAnalysis;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

[ExcludeFromCodeCoverage]
public class LocationTreeContainerDesignViewModel : LocationTreeContainerViewModel
{
    public LocationTreeContainerDesignViewModel() : base(new TreeNodeVM<ISelectableTreeEntryViewModel, GamePath>(
        new SelectableTreeEntryDesignViewModel(), new GamePath(LocationId.Game, ""))) { }
}
