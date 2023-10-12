using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

internal class BodyViewModel : AViewModel<IBodyViewModel>,
    IBodyViewModel
{
    public IModContentViewModel ModContentViewModel { get; } = new ModContentViewModel();
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();

    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } =
        new EmptyPreviewViewModel();

    public ISelectLocationViewModel SelectLocationViewModel { get; } =
        new SelectLocationViewModel();

    public IViewModel CurrentPreviewViewModel => PreviewViewModel;
}
