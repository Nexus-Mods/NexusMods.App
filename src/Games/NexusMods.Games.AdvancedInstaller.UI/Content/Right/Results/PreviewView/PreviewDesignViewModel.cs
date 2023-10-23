using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

internal class PreviewDesignViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    // Design filler data
    public virtual ILocationPreviewTreeViewModel[] Locations { get; } = GetTestData();

    private static ILocationPreviewTreeViewModel[] GetTestData()
    {
        return new[]
        {
            new LocationPreviewTreeDesignViewModel(),
            new LocationPreviewTreeDesignViewModel(),
        };
    }
}
