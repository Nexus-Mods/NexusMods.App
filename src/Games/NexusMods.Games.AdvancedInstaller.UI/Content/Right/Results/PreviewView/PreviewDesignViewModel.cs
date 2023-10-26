using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

[ExcludeFromCodeCoverage]
internal class PreviewDesignViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    // Design filler data
    public ObservableCollection<ILocationPreviewTreeViewModel> Locations { get; } =
        GetTestData().ToObservableCollection();

    private static ILocationPreviewTreeViewModel[] GetTestData()
    {
        return new[]
        {
            new LocationPreviewTreeDesignViewModel(),
            new LocationPreviewTreeDesignViewModel(),
        };
    }
}
