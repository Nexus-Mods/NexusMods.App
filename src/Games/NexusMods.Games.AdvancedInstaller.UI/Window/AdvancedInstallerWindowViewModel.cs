using System.Reactive.Disposables;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
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
    /// Constructor.
    /// </summary>
    public AdvancedInstallerWindowViewModel(
        string title,
        KeyedBox<RelativePath, LibraryArchiveTree> archiveFiles,
        Loadout.ReadOnly loadout,
        bool showUnsupportedStep)
    {
        AdvancedInstallerVM = new AdvancedInstallerPageViewModel(title, archiveFiles, loadout);
        UnsupportedModVM = new UnsupportedModPageViewModel(title);
        CurrentPageVM = showUnsupportedStep ? UnsupportedModVM : AdvancedInstallerVM;

        this.WhenActivated(disposables =>
        {
            UnsupportedModVM.AcceptCommand
                .Subscribe(_ => CurrentPageVM = AdvancedInstallerVM)
                .DisposeWith(disposables);
        });
    }
}
