using System.Reactive;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Mods;
using NexusMods.App.UI.Controls.Navigation;
using NexusMods.App.UI.Controls.Trees;
using NexusMods.App.UI.Pages.LoadoutGroupFiles;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.ModInfo.ModFiles;

[Obsolete($"To be replaced by {nameof(ILoadoutGroupFilesViewModel)}")]
public interface IModFilesViewModel : IViewModelInterface
{
    IFileTreeViewModel? FileTreeViewModel { get; }

    ReactiveCommand<NavigationInformation, Unit> OpenEditorCommand { get; }

    void Initialize(LoadoutId loadoutId, ModId modId, PageIdBundle pageIdBundle) { }
}
