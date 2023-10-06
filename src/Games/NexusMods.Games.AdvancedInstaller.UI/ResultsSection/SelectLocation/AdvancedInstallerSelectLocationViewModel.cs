using System.Collections.ObjectModel;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public class AdvancedInstallerSelectLocationViewModel : AViewModel<IAdvancedInstallerSelectLocationViewModel>,
    IAdvancedInstallerSelectLocationViewModel
{

    public AdvancedInstallerSelectLocationViewModel() : base()
    {
        SuggestedEntries = Array.Empty<IAdvancedInstallerSuggestedEntryViewModel>().ToReadOnlyObservableCollection();
    }
    public ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel> SuggestedEntries { get; set; }
}
