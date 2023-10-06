using System.Collections.ObjectModel;
using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal interface ISelectLocationViewModel : IViewModel
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }
}
