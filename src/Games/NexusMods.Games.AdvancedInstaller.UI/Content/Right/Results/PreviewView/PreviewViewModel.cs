using System.Collections.ObjectModel;
using DynamicData;
using NexusMods.Paths;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

internal class PreviewViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    public SourceCache<ILocationPreviewTreeViewModel, LocationId> LocationsCache { get; } =
        new(x => x.Root.FullPath.LocationId);

    private readonly ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> _locations;
    public ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> Locations => _locations;

    public PreviewViewModel()
    {
        LocationsCache.Connect()
            .Bind(out _locations)
            .DisposeMany()
            .Subscribe();
    }
}
