using System.Collections.ObjectModel;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

public interface ISelectLocationViewModel : IViewModel
{
    public ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel> SuggestedEntries { get; }
}
