using NexusMods.Paths;

namespace NexusMods.App.UI.Toolbars;

public interface IDefaultLoadoutToolbarViewModel : ILoadoutToolbarViewModel
{
    /// <summary>
    /// Starts the installation of a mod from the given path.
    /// </summary>
    /// <param name="path"></param>
    public Task StartManualModInstall(string path);
}
