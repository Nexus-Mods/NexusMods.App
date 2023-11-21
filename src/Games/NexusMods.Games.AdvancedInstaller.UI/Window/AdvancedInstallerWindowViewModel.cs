using System.Reactive.Disposables;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.ModInstallers;
using NexusMods.Paths;
using NexusMods.Paths.FileTree;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace NexusMods.Games.AdvancedInstaller.UI;

public class AdvancedInstallerWindowViewModel: AViewModel<IAdvancedInstallerWindowViewModel>, IAdvancedInstallerWindowViewModel
{
    [Reactive] public IViewModelInterface CurrentPageVM { get; protected set; }
    public IUnsupportedModPageViewModel UnsupportedModVM { get; }
    public IAdvancedInstallerPageViewModel AdvancedInstallerVM { get; }

    public AdvancedInstallerWindowViewModel(string modName,
        FileTreeNode<RelativePath, ModSourceFileEntry> archiveFiles,
        GameLocationsRegister register,
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
