using System.Collections.ObjectModel;

namespace NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

public interface ISelectLocationViewModel : IViewModelInterface
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }
    public ReadOnlyObservableCollection<ISelectLocationTreeViewModel> AllFoldersTrees { get; }
}
