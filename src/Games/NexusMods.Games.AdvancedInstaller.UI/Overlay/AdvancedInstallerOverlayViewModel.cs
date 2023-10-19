using NexusMods.App.UI;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerOverlayViewModel : AViewModel<IAdvancedInstallerOverlayViewModel>,
    IAdvancedInstallerOverlayViewModel
{
    public AdvancedInstallerOverlayViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register, string gameName = "")
    {
        BodyViewModel = new BodyViewModel(archiveFiles, register, gameName);
    }

    public bool IsActive { get; set; }
    public virtual IFooterViewModel FooterViewModel { get; } = new FooterViewModel();
    public virtual IBodyViewModel BodyViewModel { get; }
}
