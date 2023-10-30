using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SuggestedEntryDesignViewModel : AViewModel<ISuggestedEntryViewModel>,
    ISuggestedEntryViewModel
{

    // Needed for design time
    public SuggestedEntryDesignViewModel()
    {
        Title = "Title";
        Subtitle = "Subtitle";
    }

    public string Title { get; }
    public string Subtitle { get; }
}
