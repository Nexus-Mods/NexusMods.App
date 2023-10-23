using System.Collections.ObjectModel;
using NexusMods.App.UI;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

internal class PreviewDesignViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    // Design filler data
    public virtual ReadOnlyObservableCollection<ILocationPreviewTreeViewModel> Locations { get; } = GetTestData().ToReadOnlyObservableCollection();

    private static ILocationPreviewTreeViewModel[] GetTestData()
    {
        return new[]
        {
            new LocationPreviewTreeDesignViewModel(),
            new LocationPreviewTreeDesignViewModel(),
        };
    }
}
