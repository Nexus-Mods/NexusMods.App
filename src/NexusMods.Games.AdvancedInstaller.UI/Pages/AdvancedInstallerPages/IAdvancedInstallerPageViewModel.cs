using NexusMods.UI.Sdk;

namespace NexusMods.Games.AdvancedInstaller.UI;

/// <summary>
/// View model for the Advanced Installer page containing the main body and the footer.
/// </summary>
public interface IAdvancedInstallerPageViewModel : IViewModelInterface
{
    /// <summary>
    /// Footer element view model.
    /// </summary>
    public IFooterViewModel FooterViewModel { get; }

    /// <summary>
    /// Body view model.
    /// </summary>
    public IBodyViewModel BodyViewModel { get; }

    /// <summary>
    /// Indicates whether the user has chosen to install the mod or not.
    /// If false, the DeploymentData should be ignored.
    /// </summary>
    public bool ShouldInstall { get; set;}
}
