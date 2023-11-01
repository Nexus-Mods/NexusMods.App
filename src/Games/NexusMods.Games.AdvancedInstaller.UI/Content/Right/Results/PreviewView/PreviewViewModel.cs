using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using NexusMods.App.UI.Extensions;
using NexusMods.Paths;
using ReactiveUI;

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
            .Subscribe();
    }
}
