namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerPageViewModel : IViewModelInterface
{
    public IFooterViewModel FooterViewModel { get; }

    public IBodyViewModel BodyViewModel { get; }

    public bool ShouldInstall { get; }
}
