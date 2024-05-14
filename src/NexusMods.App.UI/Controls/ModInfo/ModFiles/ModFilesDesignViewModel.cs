using System.Reactive;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using ReactiveUI;


namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

public class ModFilesDesignViewModel : AViewModel<IModFilesViewModel>, IModFilesViewModel
{
    public IFileTreeViewModel? FileTreeViewModel { get; } = new ModFileTreeDesignViewModel();

    public ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; } = ReactiveCommand.Create<NavigationInformation, Unit>(_ => Unit.Default);
}
