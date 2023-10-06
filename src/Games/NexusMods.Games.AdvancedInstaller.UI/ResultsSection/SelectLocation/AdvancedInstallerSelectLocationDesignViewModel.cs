using System.Collections.ObjectModel;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class AdvancedInstallerSelectLocationDesignViewModel : AdvancedInstallerSelectLocationViewModel
{
    public AdvancedInstallerSelectLocationDesignViewModel() : base()
    {
        var entries = Enumerable.Range(0, 4)
            .Select(_ => new AdvancedInstallerSuggestedEntryDesignViewModel());

        SuggestedEntries =
            new ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel>(
                new ObservableCollection<IAdvancedInstallerSuggestedEntryViewModel>(entries));
    }
}
