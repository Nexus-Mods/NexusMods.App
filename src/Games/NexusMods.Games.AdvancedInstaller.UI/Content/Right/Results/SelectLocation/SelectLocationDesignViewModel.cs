using System.Collections.ObjectModel;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

internal class SelectLocationDesignViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }

    public ReadOnlyObservableCollection<ISelectLocationTreeViewModel> AllFoldersTrees { get; }
    public SelectLocationDesignViewModel() : base()
    {
        var entries = Enumerable.Range(0, 2)
            .Select(_ => new SuggestedEntryDesignViewModel());

        AllFoldersTrees = new ISelectLocationTreeViewModel[]
        {
            new SelectLocationTreeDesignViewModel(),
            new SelectLocationTreeDesignViewModel(),
        }.ToReadOnlyObservableCollection();

        SuggestedEntries =
            new ReadOnlyObservableCollection<ISuggestedEntryViewModel>(
                new ObservableCollection<ISuggestedEntryViewModel>(entries));
    }
}
