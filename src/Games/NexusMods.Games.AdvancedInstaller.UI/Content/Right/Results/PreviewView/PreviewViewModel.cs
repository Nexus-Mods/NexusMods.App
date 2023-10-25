using System.Collections.ObjectModel;
using NexusMods.App.UI.Extensions;

namespace NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;

internal class PreviewViewModel : AViewModel<IPreviewViewModel>, IPreviewViewModel
{
    // TODO: Update PreviewViewModel at runtime.
    public ObservableCollection<ILocationPreviewTreeViewModel> Locations { get; set; } =
        Array.Empty<ILocationPreviewTreeViewModel>().ToObservableCollection();
}
