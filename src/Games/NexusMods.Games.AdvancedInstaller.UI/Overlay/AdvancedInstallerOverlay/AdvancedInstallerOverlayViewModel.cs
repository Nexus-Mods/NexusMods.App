using System.Reactive.Disposables;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Games.AdvancedInstaller.UI.Content;
using NexusMods.Games.AdvancedInstaller.UI.Content.Bottom;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerOverlayViewModel : AViewModel<IAdvancedInstallerOverlayViewModel>,
    IAdvancedInstallerOverlayViewModel
{
    public AdvancedInstallerOverlayViewModel(string modName, FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register, string gameName = "")
    {
        BodyViewModel = new BodyViewModel(modName, archiveFiles, register, gameName);
        FooterViewModel = new FooterViewModel();
        WasCancelled = false;

        this.WhenActivated(disposables =>
        {
            FooterViewModel.CancelCommand = ReactiveCommand.Create(() =>
            {
                WasCancelled = true;
                IsActive = false;
            }).DisposeWith(disposables);

            FooterViewModel.InstallCommand = ReactiveCommand.Create(() =>
            {
                WasCancelled = false;
                IsActive = false;
            }, this.WhenAnyValue(vm => vm.BodyViewModel.CanInstall))
                .DisposeWith(disposables);
        });
    }

    [Reactive] public bool IsActive { get; set; }
    public IFooterViewModel FooterViewModel { get; }
    public IBodyViewModel BodyViewModel { get; }

    public bool WasCancelled { get; private set; }
}
