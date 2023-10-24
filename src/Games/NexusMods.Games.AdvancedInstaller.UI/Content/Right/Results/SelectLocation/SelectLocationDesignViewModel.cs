using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

[ExcludeFromCodeCoverage]
internal class SelectLocationDesignViewModel : AViewModel<ISelectLocationViewModel>,
    ISelectLocationViewModel
{
    public ReadOnlyObservableCollection<ISuggestedEntryViewModel> SuggestedEntries { get; }

    public ReadOnlyObservableCollection<ISelectLocationTreeViewModel> AllFoldersTrees { get; }

    public SelectLocationDesignViewModel()
    {
        var entries = Enumerable.Range(0, 2)
            .Select(_ => new SuggestedEntryDesignViewModel());

        AllFoldersTrees = new ISelectLocationTreeViewModel[]
        {
            new SelectLocationTreeDesignViewModel(),
            new SelectLocationTreeDesignViewModel(),
        }.ToReadOnlyObservableCollection();

        SuggestedEntries = new(new(entries));
    }
}
