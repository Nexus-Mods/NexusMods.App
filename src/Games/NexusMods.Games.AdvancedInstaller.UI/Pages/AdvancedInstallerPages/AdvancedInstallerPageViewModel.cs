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
    /// <summary>
    /// Constructor for the main Manual installer page, this currently contains the footer and body view models.
    /// TODO: This isn't an actual Page yet, still needs conversion.
    /// </summary>
    /// <param name="modName">The display name for the mod to install.</param>
    /// <param name="archiveFiles">A <see cref="FileTreeNode{RelativePath,ModSourceFileEntry}"/> of the files contained in the mod archive.</param>
    /// <param name="register">The register containing the game locations.</param>
    /// <param name="gameName">The display name of the game being managed.</param>
    public AdvancedInstallerPageViewModel(string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register,
        string gameName)
    {
        BodyViewModel = new BodyViewModel(new DeploymentData(), modName, archiveFiles, register, null, gameName);
        FooterViewModel = new FooterViewModel();
        ShouldInstall = false;

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.BodyViewModel.CanInstall)
                .BindToVM(this, vm => vm.FooterViewModel.CanInstall)
                .DisposeWith(disposables);

            FooterViewModel.CancelCommand.Subscribe(_ => { ShouldInstall = false; }).DisposeWith(disposables);
            FooterViewModel.InstallCommand.Subscribe(_ => { ShouldInstall = true; }).DisposeWith(disposables);
        });
    }

    /// <inheritdoc />
    public IFooterViewModel FooterViewModel { get; }

    /// <inheritdoc />
    public IBodyViewModel BodyViewModel { get; }

    /// <inheritdoc />
    public bool ShouldInstall { get; set; }
}
