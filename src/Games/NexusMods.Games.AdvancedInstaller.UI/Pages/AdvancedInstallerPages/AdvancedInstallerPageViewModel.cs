using System.Reactive.Disposables;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using NexusMods.Paths.Trees;
using ReactiveUI;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerPageViewModel : AViewModel<IAdvancedInstallerPageViewModel>,
    IAdvancedInstallerPageViewModel
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public AdvancedInstallerPageViewModel(
        string title,
        KeyedBox<RelativePath, LibraryArchiveTree> archiveFiles,
        Loadout.ReadOnly loadout)
    {
        // TODO: convert to page?

        BodyViewModel = new BodyViewModel(new DeploymentData(), title, archiveFiles, loadout);
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
