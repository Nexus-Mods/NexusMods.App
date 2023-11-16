
namespace NexusMods.Games.AdvancedInstaller.UI;

public interface IAdvancedInstallerWindowViewModel : IViewModelInterface
{
    public IViewModelInterface CurrentPageVM { get; }

    public IUnsupportedModPageViewModel UnsupportedModVM { get; }
    public IAdvancedInstallerPageViewModel AdvancedInstallerVM { get; }
}
