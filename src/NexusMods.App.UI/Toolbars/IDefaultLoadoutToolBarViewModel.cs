using NexusMods.Paths;

namespace NexusMods.App.UI.Toolbars;

public interface IDefaultLoadoutToolBarViewModel : ILoadoutToolbarViewModel
{
    /// <summary>
    /// Starts the installation of a mod from the given path.
    /// </summary>
    /// <param name="path"></param>
    public void StartManualModInstall(AbsolutePath path);
}
