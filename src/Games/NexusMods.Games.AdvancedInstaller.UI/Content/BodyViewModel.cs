using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content.Left;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.EmptyPreview;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.PreviewView;
using NexusMods.Games.AdvancedInstaller.UI.Content.Right.Results.SelectLocation;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;

namespace NexusMods.Games.AdvancedInstaller.UI.Content;

internal class BodyViewModel : AViewModel<IBodyViewModel>,
    IBodyViewModel
{
    public BodyViewModel(FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles, GameLocationsRegister register, string gameName = "")
    {
        ModContentViewModel = new ModContentViewModel(archiveFiles);
        SelectLocationViewModel = new SelectLocationViewModel(register, gameName);
    }

    public IModContentViewModel ModContentViewModel { get; }
    public IPreviewViewModel PreviewViewModel { get; } = new PreviewViewModel();

    public IEmptyPreviewViewModel EmptyPreviewViewModel { get; } =
        new EmptyPreviewViewModel();

    public ISelectLocationViewModel SelectLocationViewModel { get; }

    public IViewModel CurrentPreviewViewModel => SelectLocationViewModel;
    public DeploymentData Data { get; set; }
}
