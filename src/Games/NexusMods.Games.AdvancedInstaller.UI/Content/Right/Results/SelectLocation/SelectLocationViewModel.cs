using System.Collections.ObjectModel;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public SelectLocationViewModel() : base()
    {
        SuggestedEntries = Array.Empty<IAdvancedInstallerSuggestedEntryViewModel>().ToReadOnlyObservableCollection();
    }

    public ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel> SuggestedEntries { get; set; }
}
