using System.Diagnostics.CodeAnalysis;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Preview;

[ExcludeFromCodeCoverage]
// ReSharper disable once UnusedType.Global
public class LocationPreviewTreeDesignViewModel : LocationPreviewTreeViewModel
{
    public LocationPreviewTreeDesignViewModel() : base(
        new TreeNodeVM<IPreviewTreeEntryViewModel, GamePath>(
            new PreviewTreeEntryViewModel(
                new GamePath(LocationId.Game, ""),
                true,
                false),
            new GamePath(LocationId.Game, ""))) { }
}
