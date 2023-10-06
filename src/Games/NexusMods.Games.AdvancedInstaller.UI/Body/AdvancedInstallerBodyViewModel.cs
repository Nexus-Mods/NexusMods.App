using NexusMods.App.UI;
using NexusMods.Games.AdvancedInstaller.UI.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.SelectLocation;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerBodyViewModel : AViewModel<IAdvancedInstallerBodyViewModel>, IAdvancedInstallerBodyViewModel
{
    public IAdvancedInstallerModContentViewModel ModContentViewModel { get; } = new AdvancedInstallerModContentViewModel();
    public IAdvancedInstallerPreviewViewModel PreviewViewModel { get; } = new AdvancedInstallerPreviewViewModel();
    public IAdvancedInstallerEmptyPreviewViewModel EmptyPreviewViewModel { get; } = new AdvancedInstallerEmptyPreviewViewModel();
    public IAdvancedInstallerSelectLocationViewModel SelectLocationViewModel { get; } = new AdvancedInstallerSelectLocationViewModel();

    public IViewModel CurrentPreviewViewModel => SelectLocationViewModel;
}
