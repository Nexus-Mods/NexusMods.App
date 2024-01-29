using System.Reactive.Disposables;
using NexusMods.Abstractions.FileStore.Trees;
using NexusMods.Abstractions.Installers.Info;
using NexusMods.Paths;
using NexusMods.Paths.Trees;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerWindowViewModel : AViewModel<IAdvancedInstallerWindowViewModel>,
    IAdvancedInstallerWindowViewModel
{

    /// <InheritDoc/>
    [Reactive] public IViewModelInterface CurrentPageVM { get; protected set; }

    /// <InheritDoc/>
    public IUnsupportedModPageViewModel UnsupportedModVM { get; }

    /// <InheritDoc/>
    public IAdvancedInstallerPageViewModel AdvancedInstallerVM { get; }

    /// <summary>
    /// Construct the VM for the Advanced Installer container window.
    /// </summary>
    /// <param name="modName">The name of the mod to install.</param>
    /// <param name="archiveFiles">The tree of files contained in the archive.</param>
    /// <param name="register">The register of the game locations.</param>
    /// <param name="gameName">The name of the game managed.</param>
    public AdvancedInstallerWindowViewModel(string modName,
        KeyedBox<RelativePath, ModFileTree> archiveFiles,
        IGameLocationsRegister register,
        string gameName)
    {
        AdvancedInstallerVM = new AdvancedInstallerPageViewModel(modName, archiveFiles, register, gameName);
        UnsupportedModVM = new UnsupportedModPageViewModel(modName);
        CurrentPageVM = UnsupportedModVM;

        this.WhenActivated(disposables =>
        {
            UnsupportedModVM.AcceptCommand
                .Subscribe(_ => CurrentPageVM = AdvancedInstallerVM)
                .DisposeWith(disposables);
        });
    }
}
