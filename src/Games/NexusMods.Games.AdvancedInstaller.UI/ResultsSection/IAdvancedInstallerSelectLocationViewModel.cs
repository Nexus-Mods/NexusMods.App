using System.Collections.ObjectModel;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerSelectLocationViewModel : IViewModel
{
    public ReadOnlyObservableCollection<IAdvancedInstallerSuggestedEntryViewModel> SuggestedEntries { get; }
}
