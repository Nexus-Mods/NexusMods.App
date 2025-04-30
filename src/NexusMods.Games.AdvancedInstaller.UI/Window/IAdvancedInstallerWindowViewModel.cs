using NexusMods.Abstractions.UI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerWindowViewModel : IViewModelInterface
{
    /// <summary>
    /// The page to be displayed in the window.
    /// Can be the initial Unsupported Mod page or the Advanced Installer page.
    /// </summary>
    public IViewModelInterface CurrentPageVM { get; }

    /// <summary>
    /// View model for the Unsupported Mod dialog page.
    /// </summary>
    public IUnsupportedModPageViewModel UnsupportedModVM { get; }

    /// <summary>
    /// View model for the main Advanced Installer page.
    /// </summary>
    public IAdvancedInstallerPageViewModel AdvancedInstallerVM { get; }
}
