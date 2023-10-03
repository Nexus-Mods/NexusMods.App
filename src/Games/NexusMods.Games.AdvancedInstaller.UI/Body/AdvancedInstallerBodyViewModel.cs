using NexusMods.App.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerBodyViewModel : AViewModel<IAdvancedInstallerBodyViewModel>, IAdvancedInstallerBodyViewModel
{
    public IAdvancedInstallerModContentViewModel ModContentViewModel { get; } = new AdvancedInstallerModContentViewModel();
    public IAdvancedInstallerPreviewViewModel PreviewViewModel { get; } = new AdvancedInstallerPreviewViewModel();
    public IAdvancedInstallerEmptyPreviewViewModel EmptyPreviewViewModel { get; } = new AdvancedInstallerEmptyPreviewViewModel();

    public IViewModel CurrentPreviewViewModel => PreviewViewModel;
}
