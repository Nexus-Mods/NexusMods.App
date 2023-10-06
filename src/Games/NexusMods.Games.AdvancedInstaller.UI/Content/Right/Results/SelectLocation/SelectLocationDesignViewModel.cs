using System.Collections.ObjectModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SelectLocationDesignViewModel : SelectLocationViewModel
{
    public SelectLocationDesignViewModel() : base()
    {
        var entries = Enumerable.Range(0, 4)
            .Select(_ => new SuggestedEntryDesignViewModel());

        SuggestedEntries =
            new ReadOnlyObservableCollection<ISuggestedEntryViewModel>(
                new ObservableCollection<ISuggestedEntryViewModel>(entries));
    }
}
