using NexusMods.DataModel.Games;
using NexusMods.Paths;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SuggestedEntryViewModel : AViewModel<ISuggestedEntryViewModel>,
    ISuggestedEntryViewModel
{
    public SuggestedEntryViewModel(GameLocationsRegister register, LocationId locationId, string? subtitle,
        IAdvancedInstallerCoordinator coordinator)
    {
        Title = register[locationId].FileName;
        Subtitle = subtitle is not null ? subtitle! : locationId.Value;
    }
    public string Title { get; }
    public string Subtitle { get; }
}
