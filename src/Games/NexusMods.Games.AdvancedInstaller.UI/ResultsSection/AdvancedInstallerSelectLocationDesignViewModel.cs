using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;

namespace NexusMods.Games.AdvancedInstaller.UI;

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
