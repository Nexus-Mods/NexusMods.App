using System.Reactive.Disposables;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerPageViewModel : AViewModel<IAdvancedInstallerPageViewModel>,
    IAdvancedInstallerPageViewModel
{
    public AdvancedInstallerPageViewModel(string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register,
        string gameName)
    {
        BodyViewModel = new BodyViewModel(new DeploymentData(), modName, archiveFiles, register, null, gameName);
        FooterViewModel = new FooterViewModel();
        ShouldInstall = false;

        FooterViewModel.InstallCommand = ReactiveCommand.Create(() =>
        {
            ShouldInstall = true;
        }, this.WhenAnyValue(vm => vm.BodyViewModel.CanInstall));

        this.WhenActivated(disposables =>
        {
            FooterViewModel.CancelCommand.Subscribe(_ =>
            {
                ShouldInstall = false;
            }).DisposeWith(disposables);

        });
    }
    public IFooterViewModel FooterViewModel { get; }
    public IBodyViewModel BodyViewModel { get; }

    public bool ShouldInstall { get; private set; }
}
