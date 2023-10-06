using System.Collections.ObjectModel;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public class SelectLocationDesignViewModel : SelectLocationViewModel
{
    public SelectLocationDesignViewModel() : base()
    {
        var entries = Enumerable.Range(0, 4)
            .Select(_ => new AdvancedInstallerSuggestedEntryDesignViewModel());

        SuggestedEntries =
            new ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel>(
                new ObservableCollection<IAdvancedInstallerSuggestedEntryViewModel>(entries));
    }
}
