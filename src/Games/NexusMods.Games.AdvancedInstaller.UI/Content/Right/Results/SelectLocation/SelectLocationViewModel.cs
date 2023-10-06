using System.Collections.ObjectModel;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SelectLocationViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public SelectLocationViewModel() : base()
    {
        SuggestedEntries = Array.Empty<ISuggestedEntryViewModel>().ToReadOnlyObservableCollection();
    }

    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; set; }
}
