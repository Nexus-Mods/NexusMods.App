using NexusMods.App.UI.Controls.Trees;


namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

public class ModFilesDesignViewModel : AViewModel<IModFilesViewModel>,
    IModFilesViewModel
{
    public IFileTreeViewModel? FileTreeViewModel { get; } = new ModFileTreeDesignViewModel();
}
