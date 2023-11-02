using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using DynamicData;
using NexusMods.App.UI.Extensions;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

[ExcludeFromCodeCoverage]
internal class PreviewDesignViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    // Design filler data
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> Locations { get; } =
        GetTestData().ToReadOnlyObservableCollection();

    public SourceCache<ILocationPreviewTreeViewModel, LocationId> LocationsCache { get; } =
        new(x => x.Root.FullPath.LocationId);

    private static ILocationPreviewTreeViewModel[] GetTestData()
    {
        return new ILocationPreviewTreeViewModel[]
        {
            new LocationPreviewTreeDesignViewModel(),
            new LocationPreviewTreeDesignViewModel(),
        };
    }
}
